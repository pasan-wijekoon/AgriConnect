using AgriConnect.Api.Config;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AgriConnectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));


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
    // ---- Component D — Seed price history & reference fixtures ----
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var result = await DataSeeder.SeedAsync(db, logger);
        Console.WriteLine($"[Component D] Seeded: {result.SnapshotsAdded} snapshots, {result.AnomalyFlagsAdded} anomalies, {result.SupplyEventsAdded} supply events, {result.ReportExportsAdded} report exports.");
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

app.MapControllers();      
app.Run();