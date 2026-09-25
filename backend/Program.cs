using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums serialize/deserialize as their string names (e.g. "Pickup", not 0), matching
// the enum-as-string convention already used for the DB layer (plan §0.2).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AgriConnectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---- Component B — Dev auth seam (plan §6) ----
// No shared User/Auth/JWT implementation exists yet anywhere in the repo, so this
// scheme reads X-Dev-Role/X-Dev-UserId headers into a ClaimsPrincipal. Switch
// Auth:Mode to "Jwt" and register real bearer validation here once shared auth
// lands — controllers never change, since they only read User.FindFirst(...).
var authMode = builder.Configuration["Auth:Mode"] ?? "Dev";
switch (authMode)
{
    case "Dev":
        builder.Services
            .AddAuthentication(DevAuthDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthDefaults.Scheme, options => { });
        break;
    default:
        throw new NotSupportedException(
            $"Auth:Mode '{authMode}' is not supported yet. Only 'Dev' is implemented until shared JWT auth lands (plan §6).");
}
builder.Services.AddAuthorization();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(WebClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || args.Contains("--seed") || args.Contains("--seed-only"))
{
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
