using System.Text;
using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using AgriConnect.Api.Services.Analytics;
using AgriConnect.Api.Services.Reports;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums serialize/deserialize as their string names (e.g. "Pickup", not 0), matching
// the enum-as-string convention already used for the DB layer (plan §0.2). Also
// registers Component D's validation-error-key convention (dateRangeEnd, not
// DateRangeEnd) on the same AddControllers() call.
builder.Services.AddControllers(options =>
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Every error response is RFC 7807 ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AgriConnectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---- Authentication (integration decision, 2026-09-27) ----
// Component B and Component D each independently built a dev-mode auth bypass
// (DevAuthenticationHandler vs. FakeClaimsPrincipalMiddleware) plus, on Component D's
// side, a real (but not yet wired to any login endpoint) JWT bearer scheme. Only one
// auth mechanism can be active app-wide, so — per team decision — this integration
// branch keeps Component D's JWT bearer + FakeClaimsPrincipalMiddleware as the single
// auth path. FakeClaimsPrincipalMiddleware was extended (see FakeClaimsPrincipal.cs) to
// also honour an optional X-Dev-UserId header, matching Component B's
// DevAuthenticationHandler contract exactly, so Component B's existing IDOR tests
// (which need two distinct Buyer/Farmer ids, not one fixed id per role) keep passing
// unchanged. DevAuthenticationHandler.cs itself is left in the repo, unregistered —
// superseded, not deleted, since it's not this branch's call to remove another
// component's file outright (see PROGRESS.md).
var jwt = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwt["SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

// ---- Component D — Market Price Analytics & Reporting ----
builder.Services.AddScoped<TrendAggregationService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<ShortageDetectionService>();
builder.Services.AddScoped<AnomalyInvestigationService>();
builder.Services.AddScoped<ReportExportService>();

// ---- Shared / Cross-Cutting — Audit & Notifications (FR20/FR22, plan §12) ----
// Not owned by any single component; Component B is the first to need them.
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<NotificationService>();

// ---- Component B — Listing availability seam (plan §3) ----
// Swap for a real Component-A-backed implementation once the Listing table lands.
builder.Services.AddScoped<IListingAvailabilityPort, FixtureListingAvailabilityPort>();

// ---- Component B — Order services (FR8/FR9/FR11) ----
builder.Services.AddScoped<IStockReservationService, StockReservationService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<ReservationExpirySweepService>();

// ---- Component B — Scheduling seam (FR10, plan §8) ----
// Swap for a real HTTP client to Student 4's Logistics Scheduling Agent once it exists.
builder.Services.AddScoped<ILogisticsSchedulingPort, StubLogisticsSchedulingPort>();

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

// ---- CORS (needed for the React web client, plan §10) ----
// No CORS policy existed at all until now — nothing else in the repo has
// claimed it. ALLOWED_ORIGINS is already provisioned in docker/.env.example;
// the local-dev default covers the Vite dev server's default port (5173) plus
// the ports docker-compose.yml maps the web/backend services to.
const string WebClientCorsPolicy = "WebClient";
var allowedOrigins = (builder.Configuration["ALLOWED_ORIGINS"] ?? "http://localhost:5173,http://localhost:3000,http://localhost:5000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// Swashbuckle (classic Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// TODO: Replace FakeClaimsPrincipal with real AuthController once Component A's User model is wired up.
if (app.Environment.IsDevelopment())
{
    app.UseFakeClaimsPrincipal();
}

app.UseAuthorization();

if (app.Environment.IsDevelopment() || args.Contains("--seed") || args.Contains("--seed-only"))
{
    // ---- Shared Reference Tables — seed crops, regions, and dev users ----
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var sharedResult = await SharedReferenceSeeder.SeedAsync(db, logger);
        Console.WriteLine($"[Shared] Seeded: {sharedResult.CropsAdded} crops, {sharedResult.RegionsAdded} regions, {sharedResult.UsersAdded} users.");
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
        Console.WriteLine($"[Component B] Seeded: {result.CollectionCentresAdded} collection centres, {result.OrdersAdded} orders, {result.ReservationsAdded} reservations, {result.SchedulesAdded} schedules.");
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
