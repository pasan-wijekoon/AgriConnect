using AgriConnect.Api.Models;

namespace AgriConnect.Api.Config;

/// <summary>
/// Synthetic demo data for Component D: 16 weeks of price history, a review queue of
/// anomaly flags, and supply events. Crops and regions are looked up by name, so
/// <see cref="SharedReferenceSeeder"/> must run first.
///
/// Each table is only seeded when it is empty.
/// </summary>
public static class AnalyticsFixtures
{
    public record SeedResult(int SnapshotsAdded, int AnomalyFlagsAdded, int SupplyEventsAdded);

    private const int Weeks = 16;

    // The avg-price band (LKR/kg) for each crop; weekly min/max are kept inside it too.
    private static readonly Dictionary<string, (decimal Low, decimal High)> PriceBands = new()
    {
        ["Carrot"] = (150m, 220m),
        ["Tomato"] = (80m, 140m),
        ["Cabbage"] = (60m, 100m),
    };

    private static readonly string[] RegionNames = ["Nuwara Eliya", "Dambulla"];

    // Each supply event also bends its price series over the last 3 weeks (Shock, as a
    // fraction of the half-band), so the chart and the events list tell the same story.
    private static readonly (string Crop, string Region, SupplyEventType Type, SupplyEventSeverity Severity, double Shock, int DaysAgo, string Notes)[] SupplyEvents =
    [
        ("Tomato", "Dambulla", SupplyEventType.Shortage, SupplyEventSeverity.High, 0.40, 2,
            "Supply 52% below the 8-week baseline for 3 consecutive weeks; heavy rain disrupted harvests."),
        ("Carrot", "Nuwara Eliya", SupplyEventType.Shortage, SupplyEventSeverity.Medium, 0.22, 5,
            "Supply 28% below baseline for 2 consecutive weeks."),
        ("Cabbage", "Nuwara Eliya", SupplyEventType.Oversupply, SupplyEventSeverity.Medium, -0.35, 4,
            "Supply 41% above baseline for 3 consecutive weeks; peak harvest overlapping across growers."),
        ("Cabbage", "Dambulla", SupplyEventType.Oversupply, SupplyEventSeverity.Low, -0.15, 9,
            "Supply 17% above baseline for 2 consecutive weeks."),
    ];

    // Seven above the AI-suggested price, one well below it.
    private static readonly (decimal Deviation, AnomalyStatus Status, int DaysAgo)[] AnomalyFlags =
    [
        (52.70m, AnomalyStatus.Open, 1),
        (-30.00m, AnomalyStatus.Open, 2),
        (38.50m, AnomalyStatus.Open, 3),
        (64.10m, AnomalyStatus.Open, 5),
        (26.40m, AnomalyStatus.Open, 6),
        (44.20m, AnomalyStatus.Reviewed, 9),
        (31.80m, AnomalyStatus.Reviewed, 12),
        (58.90m, AnomalyStatus.Dismissed, 16),
    ];

    public static SeedResult Seed(AgriConnectDbContext db)
    {
        var cropIds = LoadIdsByName(db.Crops.Select(c => new { c.Name, c.Id }).ToList()
            .ToDictionary(c => c.Name, c => c.Id), PriceBands.Keys, "Crop");
        var regionIds = LoadIdsByName(db.Regions.Select(r => new { r.Name, r.Id }).ToList()
            .ToDictionary(r => r.Name, r => r.Id), RegionNames, "Region");

        var now = DateTimeOffset.UtcNow;
        int snapshots = 0, flags = 0, events = 0;

        if (!db.PriceTrendSnapshots.Any())
        {
            var rows = BuildSnapshots(cropIds, regionIds, DateOnly.FromDateTime(now.UtcDateTime));
            db.PriceTrendSnapshots.AddRange(rows);
            snapshots = rows.Count;
        }

        if (!db.PriceAnomalyFlags.Any())
        {
            db.PriceAnomalyFlags.AddRange(AnomalyFlags.Select(a => new PriceAnomalyFlag
            {
                Id = Guid.NewGuid(),
                ListingId = Guid.NewGuid(),
                DeviationPercent = a.Deviation,
                FlaggedAt = now.AddDays(-a.DaysAgo),
                Status = a.Status,
            }));
            flags = AnomalyFlags.Length;
        }

        if (!db.ShortageOversupplyEvents.Any())
        {
            db.ShortageOversupplyEvents.AddRange(SupplyEvents.Select(e => new ShortageOversupplyEvent
            {
                Id = Guid.NewGuid(),
                CropId = cropIds[e.Crop],
                RegionId = regionIds[e.Region],
                Type = e.Type,
                Severity = e.Severity,
                DetectedAt = now.AddDays(-e.DaysAgo),
                Notes = e.Notes,
            }));
            events = SupplyEvents.Length;
        }

        db.SaveChanges();
        return new SeedResult(snapshots, flags, events);
    }

    private static List<PriceTrendSnapshot> BuildSnapshots(
        Dictionary<string, Guid> cropIds, Dictionary<string, Guid> regionIds, DateOnly today)
    {
        // Fixed seed: every developer gets the same chart, so the demo is reproducible.
        var random = new Random(42);
        var thisMonday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var rows = new List<PriceTrendSnapshot>();
        var cropIndex = 0;

        foreach (var (crop, (low, high)) in PriceBands)
        {
            var mid = (low + high) / 2;
            var half = (high - low) / 2;
            cropIndex++;

            foreach (var region in RegionNames)
            {
                // Dambulla is the wholesale hub, so it trades a little above the growing region.
                var regionOffset = region == "Dambulla" ? 0.10 : -0.10;
                var shock = SupplyEvents.FirstOrDefault(e => e.Crop == crop && e.Region == region).Shock;
                var walk = 0.0;

                for (var w = Weeks - 1; w >= 0; w--)
                {
                    var step = Weeks - 1 - w;
                    var seasonal = 0.30 * Math.Sin(2 * Math.PI * step / Weeks + cropIndex * 1.7);
                    walk = walk * 0.7 + (random.NextDouble() - 0.5) * 0.16;
                    var shockNow = w < 3 ? shock * (3 - w) / 3.0 : 0.0;

                    var avg = Clamp(mid + half * (decimal)(regionOffset + seasonal + walk + shockNow), low, high);
                    var spread = avg * (decimal)(0.06 + random.NextDouble() * 0.04);

                    // Fewer listings reach market in a shortage, more in a glut.
                    var samples = shockNow switch
                    {
                        > 0 => random.Next(4, 10),
                        < 0 => random.Next(30, 46),
                        _ => random.Next(10, 31),
                    };

                    rows.Add(new PriceTrendSnapshot
                    {
                        Id = Guid.NewGuid(),
                        CropId = cropIds[crop],
                        RegionId = regionIds[region],
                        Period = thisMonday.AddDays(-7 * w),
                        AvgPrice = Math.Round(avg, 2),
                        MinPrice = Math.Round(Math.Max(low, avg - spread), 2),
                        MaxPrice = Math.Round(Math.Min(high, avg + spread), 2),
                        SampleCount = samples,
                    });
                }
            }
        }

        return rows;
    }

    private static Dictionary<string, Guid> LoadIdsByName(
        Dictionary<string, Guid> existing, IEnumerable<string> required, string table)
    {
        var missing = required.Where(n => !existing.ContainsKey(n)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"{table} rows missing: {string.Join(", ", missing)}. Run SharedReferenceSeeder first.");
        }

        return existing;
    }

    private static decimal Clamp(decimal value, decimal low, decimal high) =>
        Math.Min(high, Math.Max(low, value));
}
