using System.Text;
using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using AgriConnect.Api.Services.Analytics;
using AgriConnect.Api.Services.Reports;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums serialize/deserialize as their string names (e.g. "Pickup", not 0), matching
// the enum-as-string convention already used for the DB layer (plan §0.2). Also
// registers Component D's validation-error-key convention (dateRangeEnd, not
// DateRangeEnd) on the same AddControllers() call. (Component A's AddControllers() set
// PropertyNamingPolicy = CamelCase explicitly — already ASP.NET Core's own default for
// AddControllers(), so nothing was lost by not repeating it here.)
builder.Services.AddControllers(options =>
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Every error response is RFC 7807 ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AgriConnectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---- Authentication (integration decision, 2026-09-27, updated same day when
// Component A's real auth was merged in) ----
// Three independently-built auth stories existed across the four component branches:
// Component B's DevAuthenticationHandler (dev header bypass), Component D's
// FakeClaimsPrincipalMiddleware (another dev header bypass) + an unwired JWT bearer
// scheme, and Component A's actual working login/register/JWT issuance
// (AuthController/AuthService, PBKDF2-HMAC-SHA256 password hashing). Per team decision,
// Component A's real auth is now the primary path — Component D's JWT bearer scheme
// (below) validates the exact tokens AuthService.GenerateToken issues (same signing key
// config, now read under Component A's Jwt:Key/Jwt:Issuer/Jwt:Audience/Jwt:ExpiryHours
// names since AuthService.cs already hardcodes those).
//
// FakeClaimsPrincipalMiddleware is kept, Development-only, as a secondary fallback —
// not a competing auth system: it only activates when a request carries neither an
// Authorization header nor was otherwise authenticated, so a real bearer token always
// wins. This is what keeps backend.Tests' 127 existing tests (which construct specific
// role/user-id combinations via X-Dev-Role/X-Dev-UserId, including two distinct
// Buyer/Farmer ids for IDOR coverage) exercising business logic directly rather than
// needing a full register-then-login round trip per test — a deliberate scope decision,
// not an oversight; see PROGRESS.md. DevAuthenticationHandler.cs is left in the repo,
// unregistered — superseded, not deleted.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is not configured. Set it via appsettings.Development.json, " +
        "an environment variable (Jwt__Key), or `dotnet user-secrets`.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgriConnect",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AgriConnect",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

// ---- Component A — Produce Listings & Price Discovery (auth + marketplace) ----
builder.Services.AddHttpClient();
builder.Services.AddScoped<AgenticAiService>();
builder.Services.AddScoped<TodayPriceCatalogService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ListingService>();

// ---- Component D — Market Price Analytics & Reporting ----
builder.Services.AddScoped<TrendAggregationService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<ShortageDetectionService>();
builder.Services.AddScoped<AnomalyInvestigationService>();
builder.Services.AddScoped<ReportExportService>();
// Missed during the original Component D merge (2026-09-27) — AnalyticsController
// needs it (GET /api/analytics/filters) but nothing had registered it, a gap only
// a live browser walkthrough surfaced, not `dotnet build`/`dotnet test`.
builder.Services.AddScoped<ReferenceDataService>();

// ---- Shared / Cross-Cutting — Audit & Notifications (FR20/FR22, plan §12) ----
// Not owned by any single component; Component B is the first to need them.
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<NotificationService>();

// ---- Component B — Listing availability seam (plan §3) ----
// Real Component-A-backed implementation (2026-09-27) — the fixture stayed
// wired here well after Component A's Listing table landed, meaning orders
// could only ever be placed against Component B's own hardcoded demo listing
// ids, never a real marketplace listing. Found during a full-system
// integration audit. FixtureListingAvailabilityPort is kept in the repo for
// backend.Tests (which still constructs it directly against fixture data),
// but is no longer the runtime registration.
builder.Services.AddScoped<IListingAvailabilityPort, DbListingAvailabilityPort>();

// ---- Component B — Order services (FR8/FR9/FR11) ----
builder.Services.AddScoped<IStockReservationService, StockReservationService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<ReservationExpirySweepService>();

// ---- Component B — Scheduling seam (FR10, plan §8) ----
// Real HTTP client to Student 4's Logistics Scheduling Agent (agentic-ai/,
// POST /agents/logistics/schedule). Falls back to StubLogisticsSchedulingPort's
// behaviour whenever the agent is unavailable, so scheduling never depends on it.
builder.Services.AddHttpClient<ILogisticsSchedulingPort, HttpLogisticsSchedulingPort>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["AgenticAi:BaseUrl"] ?? "http://localhost:8000/");
});

// ---- Component B — Buyer-Farmer Matching Agent (FR10, plan §8.1) ----
// Unlike ILogisticsSchedulingPort above, this agent already exists and is
// independently tested (agentic-ai/, Phase 8) — this is a real HTTP client, not
// a stub. AgenticAi:BaseUrl/ApiKey are already wired through docker-compose.yml
// for the containerized setup; the local-dev default targets the agent's own
// `uvicorn` default port (agentic-ai/src/app/main.py).
builder.Services.AddHttpClient<IBuyerFarmerMatchingPort, HttpBuyerFarmerMatchingPort>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["AgenticAi:BaseUrl"] ?? "http://localhost:8000/");
});
builder.Services.AddScoped<SchedulingService>();

// ---- Component B — Maps/Distance integration (FR21, plan §9) ----
// Server-side only — no Maps API credential ever reaches a client (CLAUDE.md
// §19). Default targets OSRM's free public demo server, which needs no API
// key at all; MapsApi:ApiKey stays wired for a self-hosted OSRM instance or a
// different provider, but is simply unused (no header added) when empty.
builder.Services.AddHttpClient<IDistanceService, DistanceService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["MapsApi:BaseUrl"] ?? "https://router.project-osrm.org/");
    // OSRM's public demo server's nginx front-end returns 403 Forbidden for
    // requests with no User-Agent header — which is HttpClient's default (curl
    // and browsers always send one, .NET does not). Found by comparing a
    // working curl request against a failing HttpClient one byte-for-byte.
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AgriConnect-Backend/1.0");
    var apiKey = config["MapsApi:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", apiKey);
    }
});
builder.Services.AddScoped<CollectionCentreService>();

// ---- Component C — Quality Grading & Inspection (FR12–FR14/FR5 publish gate) ----
// AgentClientService calls the same agentic-ai FastAPI app as the Buyer-Farmer
// Matching Agent above (agentic-ai/src/app/api/quality_routes.py, registered
// alongside matching.py in main.py) — reuses AgenticAi:BaseUrl rather than a
// separate config key, since it's one Python service with multiple routers.
builder.Services.AddHttpClient<IAgentClientService, AgentClientService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["AgenticAi:BaseUrl"] ?? "http://localhost:8000/");
});
builder.Services.AddScoped<IInspectionService, InspectionService>();

// ---- Component D — AI Scheduling preview (Logistics Scheduling Agent) ----
// Same agentic-ai service and AgenticAi:BaseUrl as the agents above; lets officers
// try the agent directly from the analytics dashboard. Stores nothing.
builder.Services.AddHttpClient<AgriConnect.Api.Services.Agents.LogisticsAgentClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["AgenticAi:BaseUrl"] ?? "http://localhost:8000/");
});

// ---- CORS (needed for the React web client, plan §10) ----
// ALLOWED_ORIGINS is already provisioned in docker/.env.example; the local-dev
// default covers the Vite dev server's default port (5173) plus the ports
// docker-compose.yml maps the web/backend services to. AllowCredentials() added
// per Component A's own reasoning (AllowAnyOrigin()+credentials is a CSRF risk) —
// harmless here since AllowAnyOrigin() was never used, just explicit defense.
const string WebClientCorsPolicy = "WebClient";
var allowedOrigins = (builder.Configuration["ALLOWED_ORIGINS"] ?? "http://localhost:5173,http://localhost:5174,http://localhost:3000,http://localhost:5000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        if (builder.Environment.IsDevelopment())
        {
            // flutter run -d chrome (web debug mode) picks a random localhost port
            // every single launch, so a fixed allow-list can never keep up with it —
            // reflect back any http(s)://localhost/127.0.0.1 origin instead,
            // regardless of port. Safe only in Development: production still uses
            // the explicit ALLOWED_ORIGINS list below.
            policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Host is "localhost" or "127.0.0.1"));
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }
    });
});

// Swashbuckle (classic Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---- Startup diagnostics: print DB connectivity + server status to the terminal ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        Console.WriteLine(canConnect
            ? "[Startup] Database: CONNECTED (PostgreSQL reachable)"
            : "[Startup] Database: NOT CONNECTED (PostgreSQL unreachable)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Database: NOT CONNECTED — {ex.Message}");
    }
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = string.Join(", ", app.Urls);
    Console.WriteLine($"[Startup] Backend: RUNNING on {urls}");
});

app.UseExceptionHandler();
// Gives empty 401/403/404 responses a ProblemDetails body too.
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(WebClientCorsPolicy);

// Serves generated reports from wwwroot/reports.
app.UseStaticFiles();

app.UseAuthentication();

// A signed JWT must not keep an account alive after an administrator deactivates it.
// Claims remain valid cryptographically, so check the current account state before
// every protected request. This also prevents stale tokens from accessing data.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var active = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive);
        if (!active)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication required",
                Detail = "This account is inactive or no longer exists."
            });
            return;
        }
    }
    await next();
});

app.UseAuthorization();

// ---- Database connectivity check (useful for Railway deployments) ----
// FIX: this endpoint used to sit above `var app = builder.Build();`, which caused
// CS0841 (using 'app' before it is declared) and the follow-on CS8031 errors.
// Endpoints must be mapped on `app`, i.e. AFTER Build(). It reuses the EF Core
// DbContext (same connection string as the rest of the app), so no direct Npgsql
// reference is needed. The real exception is logged server-side only, never
// returned to the caller, so connection details are not leaked publicly.
app.MapGet("/db-check", async (AgriConnectDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { connected = true })
            : Results.Json(new { connected = false }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connectivity check failed");
        return Results.Json(new { connected = false }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

if (app.Environment.IsDevelopment() || args.Contains("--seed") || args.Contains("--seed-only"))
{
    // ---- Shared Reference Tables — seed crops, regions, and dev users ----
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var sharedResult = await SharedReferenceSeeder.SeedAsync(db, logger);
        Console.WriteLine($"[Shared] Seeded: {sharedResult.CropsAdded} crops, {sharedResult.RegionsAdded} regions, {sharedResult.UsersAdded} users.");

        // Photos uploaded before images moved into the database are copied in once (idempotent).
        var uploadsDir = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads");
        var importedImages = await UploadedImageImporter.ImportAsync(db, uploadsDir, logger);
        if (importedImages > 0) Console.WriteLine($"[Shared] Imported {importedImages} existing photo(s) from wwwroot/uploads into the database.");
    }

    // ---- Component D — demo price history, anomaly flags and supply events ----
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var result = AnalyticsFixtures.Seed(db);
        Console.WriteLine($"[Component D] Seeded: {result.SnapshotsAdded} snapshots, {result.AnomalyFlagsAdded} anomalies, {result.SupplyEventsAdded} supply events.");
    }

    // ---- Component B — Seed collection centres & demo order fixtures ----
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var result = await DataSeeder.SeedAsync(db, logger);
        Console.WriteLine($"[Component B] Seeded: {result.CollectionCentresAdded} collection centres, {result.ListingsAdded} listings, {result.OrdersAdded} orders, {result.ReservationsAdded} reservations, {result.SchedulesAdded} schedules.");
    }
}

// Support running with --verify-seed to inspect database counts and sample values
if (args.Contains("--verify-seed"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();

        var centreCount = await db.CollectionCentres.CountAsync();
        var orderCount = await db.Orders.CountAsync();
        var reservationCount = await db.StockReservations.CountAsync();
        var scheduleCount = await db.PickupSchedules.CountAsync();
        Console.WriteLine($"[VERIFY] CollectionCentres: {centreCount}");
        Console.WriteLine($"[VERIFY] Orders: {orderCount}");
        Console.WriteLine($"[VERIFY] StockReservations: {reservationCount}");
        Console.WriteLine($"[VERIFY] PickupSchedules: {scheduleCount}");

        var sampleOrder = await db.Orders.FirstAsync();
        Console.WriteLine($"[VERIFY] Sample Order: Status={sampleOrder.Status}, Quantity={sampleOrder.Quantity}, DeliveryPreference={sampleOrder.DeliveryPreference}");

        var snapshotCount = await db.PriceTrendSnapshots.CountAsync();
        var anomalyCount = await db.PriceAnomalyFlags.CountAsync();
        var eventCount = await db.ShortageOversupplyEvents.CountAsync();
        var reportCount = await db.ReportExports.CountAsync();
        Console.WriteLine($"[VERIFY] PriceTrendSnapshots: {snapshotCount}");
        Console.WriteLine($"[VERIFY] PriceAnomalyFlags: {anomalyCount}");
        Console.WriteLine($"[VERIFY] ShortageOversupplyEvents: {eventCount}");
        Console.WriteLine($"[VERIFY] ReportExports: {reportCount}");

        var sampleSnapshot = await db.PriceTrendSnapshots.FirstAsync();
        Console.WriteLine($"[VERIFY] Sample Snapshot: Period={sampleSnapshot.Period}, AvgPrice={sampleSnapshot.AvgPrice}, MinPrice={sampleSnapshot.MinPrice}, MaxPrice={sampleSnapshot.MaxPrice}, SampleCount={sampleSnapshot.SampleCount}");
    }
    return;
}

// Support running with --seed-only to seed and terminate cleanly (for CI/scripts)
if (args.Contains("--seed-only"))
{
    Console.WriteLine("Seeding completed. Exiting (--seed-only flag specified).");
    return;
}

app.MapControllers();
app.Run();

// Exposes the top-level-statements Program class (implicitly `internal`) to
// backend.Tests' WebApplicationFactory<Program>-based integration tests
// (Phase 12, plan §13's "API/integration" row).
public partial class Program;
