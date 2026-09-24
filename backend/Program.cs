using System.Text;
using AgriConnect.Api.Config;
using AgriConnect.Api.Services.Analytics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AgriConnectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT bearer authentication.
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

// Component D — Market Price Analytics & Reporting.
builder.Services.AddScoped<TrendAggregationService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<ShortageDetectionService>();
builder.Services.AddScoped<AnomalyInvestigationService>();

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
}

// Support running with --verify-seed to inspect database counts and sample values
if (args.Contains("--verify-seed"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
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

app.UseAuthentication();

// TODO: Replace FakeClaimsPrincipal with real AuthController once Component A's User model is wired up.
if (builder.Environment.IsDevelopment())
{
    app.UseFakeClaimsPrincipal();
}

app.UseAuthorization();

app.MapControllers();
app.Run();