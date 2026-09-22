using AgriConnect.Api.Models;

namespace AgriConnect.Api.Config;

/// <summary>
/// Reference data models and deterministic fixtures for Component D (Market Price Analytics &amp; Reporting).
/// 
/// Since Crop, Region, Listing, and User tables are owned by other components and may not
/// yet exist in the shared database, these fixtures provide deterministic GUIDs, display
/// metadata, and realistic historical fixtures for local development, demoing, and automated tests.
/// </summary>
public static class AnalyticsFixtures
{
    // =========================================================================
    // 1. Reference Crops (Shared Reference Data / Component A)
    // =========================================================================

    public record CropReference(Guid Id, string Name, string Category);

    public static readonly CropReference CropCarrot = new(
        Guid.Parse("3f2a0001-0000-0000-0000-000000000001"),
        "Carrot",
        "Vegetable");

    public static readonly CropReference CropTomato = new(
        Guid.Parse("3f2a0002-0000-0000-0000-000000000002"),
        "Tomato",
        "Vegetable");

    public static readonly CropReference CropPotato = new(
        Guid.Parse("3f2a0003-0000-0000-0000-000000000003"),
        "Potato",
        "Vegetable");

    public static readonly CropReference CropGreenChilli = new(
        Guid.Parse("3f2a0004-0000-0000-0000-000000000004"),
        "Green Chilli",
        "Spice & Vegetable");

    public static readonly CropReference CropRedOnion = new(
        Guid.Parse("3f2a0005-0000-0000-0000-000000000005"),
        "Red Onion",
        "Vegetable");

    public static readonly IReadOnlyList<CropReference> Crops =
    [
        CropCarrot,
        CropTomato,
        CropPotato,
        CropGreenChilli,
        CropRedOnion
    ];

    public static readonly IReadOnlyDictionary<Guid, CropReference> CropsById =
        Crops.ToDictionary(c => c.Id);

    // =========================================================================
    // 2. Reference Regions (Shared Reference Data)
    // =========================================================================

    public record RegionReference(Guid Id, string Name, string Description);

    public static readonly RegionReference RegionNuwaraEliya = new(
        Guid.Parse("8b1c0001-0000-0000-0000-000000000001"),
        "Nuwara Eliya",
        "Highland vegetable cultivation hub");

    public static readonly RegionReference RegionDambulla = new(
        Guid.Parse("8b1c0002-0000-0000-0000-000000000002"),
        "Dambulla",
        "Central dedicated economic centre and distribution hub");

    public static readonly RegionReference RegionJaffna = new(
        Guid.Parse("8b1c0003-0000-0000-0000-000000000003"),
        "Jaffna",
        "Northern agricultural centre, specialty red onion & chilli");

    public static readonly RegionReference RegionBadulla = new(
        Guid.Parse("8b1c0004-0000-0000-0000-000000000004"),
        "Badulla",
        "Uva province vegetable and potato farming basin");

    public static readonly IReadOnlyList<RegionReference> Regions =
    [
        RegionNuwaraEliya,
        RegionDambulla,
        RegionJaffna,
        RegionBadulla
    ];

    public static readonly IReadOnlyDictionary<Guid, RegionReference> RegionsById =
        Regions.ToDictionary(r => r.Id);

    // =========================================================================
    // 3. Reference Users (Shared Auth)
    // =========================================================================

    public static readonly Guid AdminUserId = Guid.Parse("a1111111-0000-0000-0000-000000000001");
    public static readonly Guid OfficerUserId = Guid.Parse("a2222222-0000-0000-0000-000000000001");
    public static readonly Guid FarmerUserId = Guid.Parse("f1111111-0000-0000-0000-000000000001");

    // =========================================================================
    // 4. Synthetic Listings (Component A contract mock)
    // =========================================================================

    public record SyntheticListing(
        Guid Id,
        Guid FarmerId,
        Guid CropId,
        Guid RegionId,
        decimal Price,
        decimal? AiSuggestedPriceMin,
        decimal? AiSuggestedPriceMax,
        string ClaimedGrade,
        string Status,
        DateTimeOffset CreatedAt);

    public static readonly IReadOnlyList<SyntheticListing> Listings =
    [
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            FarmerUserId,
            CropCarrot.Id,
            RegionNuwaraEliya.Id,
            Price: 320.00m,
            AiSuggestedPriceMin: 220.00m,
            AiSuggestedPriceMax: 250.00m,
            ClaimedGrade: "A",
            Status: "Published",
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-2)),
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            FarmerUserId,
            CropTomato.Id,
            RegionDambulla.Id,
            Price: 110.00m,
            AiSuggestedPriceMin: 150.00m,
            AiSuggestedPriceMax: 170.00m,
            ClaimedGrade: "B",
            Status: "Published",
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-4)),
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000003"),
            FarmerUserId,
            CropPotato.Id,
            RegionBadulla.Id,
            Price: 420.00m,
            AiSuggestedPriceMin: 280.00m,
            AiSuggestedPriceMax: 300.00m,
            ClaimedGrade: "A",
            Status: "Published",
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-10)),
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000004"),
            FarmerUserId,
            CropGreenChilli.Id,
            RegionJaffna.Id,
            Price: 980.00m,
            AiSuggestedPriceMin: 650.00m,
            AiSuggestedPriceMax: 720.00m,
            ClaimedGrade: "A",
            Status: "Published",
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-15)),
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000005"),
            FarmerUserId,
            CropRedOnion.Id,
            RegionJaffna.Id,
            Price: 420.00m,
            AiSuggestedPriceMin: 340.00m,
            AiSuggestedPriceMax: 360.00m,
            ClaimedGrade: "B",
            Status: "Published",
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-20)),
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000006"),
            FarmerUserId,
            CropTomato.Id,
            RegionNuwaraEliya.Id,
            Price: 130.00m,
            AiSuggestedPriceMin: 170.00m,
            AiSuggestedPriceMax: 190.00m,
            ClaimedGrade: "C",
            Status: "Published",
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-25))
    ];

    // =========================================================================
    // 5. Price Trend Snapshots Generation (16 Weeks across 4 crops × 3 regions)
    // =========================================================================

    /// <summary>
    /// Generates 16 weeks of realistic historical snapshots per crop and region.
    /// Anchor week starts at the current week's Monday and steps backward 16 weeks.
    /// </summary>
    public static List<PriceTrendSnapshot> GetHistoricalSnapshots()
    {
        var snapshots = new List<PriceTrendSnapshot>();

        // Align anchor to the most recent Monday
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var currentMonday = today.AddDays(-daysSinceMonday);

        // Define crops and regions to generate combinations for (4 crops × 3 regions = 12 series)
        var combinations = new (CropReference crop, RegionReference region, decimal basePrice, decimal volatility)[]
        {
            // Carrot (Nuwara Eliya: production source, slightly lower prices)
            (CropCarrot, RegionNuwaraEliya, 195.00m, 12.00m),
            // Carrot (Dambulla: wholesale transit hub)
            (CropCarrot, RegionDambulla, 235.00m, 15.00m),
            // Carrot (Badulla: alternative cool highland)
            (CropCarrot, RegionBadulla, 210.00m, 14.00m),

            // Tomato (Dambulla: high seasonal volatility)
            (CropTomato, RegionDambulla, 185.00m, 35.00m),
            // Tomato (Nuwara Eliya)
            (CropTomato, RegionNuwaraEliya, 220.00m, 30.00m),
            // Tomato (Badulla)
            (CropTomato, RegionBadulla, 195.00m, 28.00m),

            // Potato (Badulla/Welimada: primary potato belt)
            (CropPotato, RegionBadulla, 260.00m, 18.00m),
            // Potato (Nuwara Eliya: premium highland potato)
            (CropPotato, RegionNuwaraEliya, 290.00m, 20.00m),
            // Potato (Dambulla: distribution hub)
            (CropPotato, RegionDambulla, 310.00m, 22.00m),

            // Green Chilli (Jaffna: major chilli harvest)
            (CropGreenChilli, RegionJaffna, 540.00m, 65.00m),
            // Green Chilli (Dambulla: wholesale price)
            (CropGreenChilli, RegionDambulla, 620.00m, 75.00m),
            // Green Chilli (Nuwara Eliya)
            (CropGreenChilli, RegionNuwaraEliya, 680.00m, 80.00m),
        };

        const int weeksBack = 16;

        foreach (var (crop, region, basePrice, volatility) in combinations)
        {
            for (int w = weeksBack - 1; w >= 0; w--)
            {
                var period = currentMonday.AddDays(-7 * w);

                // Deterministic seasonal wave based on week number and crop
                double phase = (weeksBack - w) * 0.45 + crop.Name.Length;
                decimal seasonalDelta = (decimal)Math.Sin(phase) * volatility;
                decimal trendDelta = ((weeksBack - w) - 8) * 1.50m; // Mild inflation/supply shift

                decimal avg = Math.Round(basePrice + seasonalDelta + trendDelta, 2);
                if (avg < 50.00m) avg = 50.00m;

                decimal minSpread = Math.Round(avg * 0.12m, 2);
                decimal maxSpread = Math.Round(avg * 0.15m, 2);

                decimal min = Math.Round(avg - minSpread, 2);
                decimal max = Math.Round(avg + maxSpread, 2);

                int sampleCount = 12 + ((weeksBack - w) * 3) % 25;

                // Deterministic ID derived from crop, region, and period
                string idSeed = $"{crop.Id:N}-{region.Id:N}-{period:yyyyMMdd}";
                var id = CreateDeterministicGuid(idSeed);

                snapshots.Add(new PriceTrendSnapshot
                {
                    Id = id,
                    CropId = crop.Id,
                    RegionId = region.Id,
                    Period = period,
                    AvgPrice = avg,
                    MinPrice = min,
                    MaxPrice = max,
                    SampleCount = sampleCount
                });
            }
        }

        return snapshots;
    }

    // =========================================================================
    // 6. Sample Price Anomaly Flags (FR16)
    // =========================================================================

    public static List<PriceAnomalyFlag> GetSampleAnomalyFlags()
    {
        return
        [
            new PriceAnomalyFlag
            {
                Id = Guid.Parse("e0000001-0000-0000-0000-000000000001"),
                ListingId = Listings[0].Id, // Carrot Nuwara Eliya @ 320 vs midpoint 235 (+36.17%)
                DeviationPercent = 36.17m,
                FlaggedAt = DateTimeOffset.UtcNow.AddDays(-2),
                Status = AnomalyStatus.Open
            },
            new PriceAnomalyFlag
            {
                Id = Guid.Parse("e0000002-0000-0000-0000-000000000002"),
                ListingId = Listings[1].Id, // Tomato Dambulla @ 110 vs midpoint 160 (-31.25%)
                DeviationPercent = -31.25m,
                FlaggedAt = DateTimeOffset.UtcNow.AddDays(-4),
                Status = AnomalyStatus.Open
            },
            new PriceAnomalyFlag
            {
                Id = Guid.Parse("e0000003-0000-0000-0000-000000000003"),
                ListingId = Listings[2].Id, // Potato Badulla @ 420 vs midpoint 290 (+44.83%)
                DeviationPercent = 44.83m,
                FlaggedAt = DateTimeOffset.UtcNow.AddDays(-10),
                Status = AnomalyStatus.Reviewed
            },
            new PriceAnomalyFlag
            {
                Id = Guid.Parse("e0000004-0000-0000-0000-000000000004"),
                ListingId = Listings[3].Id, // Green Chilli Jaffna @ 980 vs midpoint 685 (+43.07%)
                DeviationPercent = 43.07m,
                FlaggedAt = DateTimeOffset.UtcNow.AddDays(-15),
                Status = AnomalyStatus.Reviewed
            },
            new PriceAnomalyFlag
            {
                Id = Guid.Parse("e0000005-0000-0000-0000-000000000005"),
                ListingId = Listings[4].Id, // Red Onion Jaffna @ 420 vs midpoint 350 (+20.00%)
                DeviationPercent = 20.00m,
                FlaggedAt = DateTimeOffset.UtcNow.AddDays(-20),
                Status = AnomalyStatus.Dismissed
            },
            new PriceAnomalyFlag
            {
                Id = Guid.Parse("e0000006-0000-0000-0000-000000000006"),
                ListingId = Listings[5].Id, // Tomato Nuwara Eliya @ 130 vs midpoint 180 (-27.78%)
                DeviationPercent = -27.78m,
                FlaggedAt = DateTimeOffset.UtcNow.AddDays(-25),
                Status = AnomalyStatus.Dismissed
            }
        ];
    }

    // =========================================================================
    // 7. Sample Shortage & Oversupply Events (FR17)
    // =========================================================================

    public static List<ShortageOversupplyEvent> GetSampleSupplyEvents()
    {
        return
        [
            new ShortageOversupplyEvent
            {
                Id = Guid.Parse("c0000001-0000-0000-0000-000000000001"),
                CropId = CropTomato.Id,
                RegionId = RegionDambulla.Id,
                Type = SupplyEventType.Shortage,
                Severity = SupplyEventSeverity.High,
                DetectedAt = DateTimeOffset.UtcNow.AddDays(-3),
                Notes = "Heavy monsoonal rains in Matale/Dambulla border dropped incoming supply by 48% below the 4-week rolling baseline. Wholesale prices spiked accordingly."
            },
            new ShortageOversupplyEvent
            {
                Id = Guid.Parse("c0000002-0000-0000-0000-000000000002"),
                CropId = CropCarrot.Id,
                RegionId = RegionNuwaraEliya.Id,
                Type = SupplyEventType.Oversupply,
                Severity = SupplyEventSeverity.Medium,
                DetectedAt = DateTimeOffset.UtcNow.AddDays(-7),
                Notes = "Simultaneous peak harvesting across Kandapola and Nuwara Eliya produced a 34% volume surplus above normal regional intake."
            },
            new ShortageOversupplyEvent
            {
                Id = Guid.Parse("c0000003-0000-0000-0000-000000000003"),
                CropId = CropGreenChilli.Id,
                RegionId = RegionJaffna.Id,
                Type = SupplyEventType.Shortage,
                Severity = SupplyEventSeverity.Medium,
                DetectedAt = DateTimeOffset.UtcNow.AddDays(-12),
                Notes = "Prolonged dry spell in northern cultivation sectors caused a 28% drop in published listings over 3 consecutive periods."
            },
            new ShortageOversupplyEvent
            {
                Id = Guid.Parse("c0000004-0000-0000-0000-000000000004"),
                CropId = CropPotato.Id,
                RegionId = RegionBadulla.Id,
                Type = SupplyEventType.Oversupply,
                Severity = SupplyEventSeverity.Low,
                DetectedAt = DateTimeOffset.UtcNow.AddDays(-18),
                Notes = "Welimada seasonal harvest delivered 15% above forecast; local cold storage facilities are operating near maximum capacity."
            }
        ];
    }

    // =========================================================================
    // 8. Sample Report Exports (FR18)
    // =========================================================================

    public static List<ReportExport> GetSampleReportExports()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return
        [
            new ReportExport
            {
                Id = Guid.Parse("d0000001-0000-0000-0000-000000000001"),
                RequestedBy = AdminUserId,
                Type = "PriceTrends",
                DateRangeStart = today.AddDays(-90),
                DateRangeEnd = today,
                GeneratedAt = DateTimeOffset.UtcNow.AddDays(-2),
                FileUrl = "/exports/price-trends-2026-q3.csv"
            },
            new ReportExport
            {
                Id = Guid.Parse("d0000002-0000-0000-0000-000000000002"),
                RequestedBy = AdminUserId,
                Type = "PriceAnomalies",
                DateRangeStart = today.AddDays(-30),
                DateRangeEnd = today,
                GeneratedAt = DateTimeOffset.UtcNow.AddDays(-5),
                FileUrl = "/exports/anomalies-2026-sep.csv"
            },
            new ReportExport
            {
                Id = Guid.Parse("d0000003-0000-0000-0000-000000000003"),
                RequestedBy = AdminUserId,
                Type = "SupplyEvents",
                DateRangeStart = today.AddDays(-60),
                DateRangeEnd = today,
                GeneratedAt = DateTimeOffset.UtcNow.AddDays(-10),
                FileUrl = "/exports/supply-events-2026.csv"
            }
        ];
    }

    // =========================================================================
    // Helper: Deterministic UUID generation from a seed string
    // =========================================================================

    private static Guid CreateDeterministicGuid(string input)
    {
        byte[] hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
