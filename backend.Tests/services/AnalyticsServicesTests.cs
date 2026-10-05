using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using AgriConnect.Api.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.services;

/// <summary>
/// Component D (Market Price Analytics, FR15-FR17): trend aggregation, price-anomaly
/// detection and shortage/oversupply events. Test ids (TC-D-xx) match the Test Case Document.
/// </summary>
public class AnalyticsServicesTests
{
    private static readonly Guid Crop = Guid.NewGuid();
    private static readonly Guid RegionA = Guid.NewGuid();
    private static readonly Guid RegionB = Guid.NewGuid();

    // 2026-09-07 is a Monday.
    private static readonly DateOnly Monday = new(2026, 9, 7);

    private static AgriConnectDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static PriceTrendSnapshot Snapshot(
        DateOnly period, decimal avg, int samples, Guid? region = null, decimal? min = null, decimal? max = null) => new()
    {
        Id = Guid.NewGuid(),
        CropId = Crop,
        RegionId = region ?? RegionA,
        Period = period,
        AvgPrice = avg,
        MinPrice = min ?? avg - 10,
        MaxPrice = max ?? avg + 10,
        SampleCount = samples,
    };

    private static AnomalyDetectionService NewAnomalyService(AgriConnectDbContext db, decimal? threshold = null)
    {
        var settings = new Dictionary<string, string?>();
        if (threshold is { } t)
        {
            settings["Analytics:AnomalyThresholdPercent"] = t.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return new AnomalyDetectionService(db, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    // ---------------------------------------------------------------- Trend aggregation (FR15)

    [Theory] // TC-D-01
    [InlineData("2026-09-07", "2026-09-07")] // Monday stays Monday
    [InlineData("2026-09-09", "2026-09-07")] // Wednesday
    [InlineData("2026-09-13", "2026-09-07")] // Sunday belongs to the week that started the previous Monday
    [InlineData("2026-09-14", "2026-09-14")] // next Monday starts a new week
    public void WeekStart_ReturnsTheMondayOfThatWeek(string date, string expectedMonday)
    {
        Assert.Equal(DateOnly.Parse(expectedMonday), TrendAggregationService.WeekStart(DateOnly.Parse(date)));
    }

    [Fact] // TC-D-02
    public void BuildPoints_WeightsTheAverageBySampleCount()
    {
        // (100 * 10 + 200 * 30) / 40 = 175 — a week from 30 listings outweighs one from 10.
        var rows = new[]
        {
            Snapshot(Monday, 100m, 10, RegionA, min: 90m, max: 110m),
            Snapshot(Monday, 200m, 30, RegionB, min: 150m, max: 220m),
        };

        var point = Assert.Single(TrendAggregationService.BuildPoints(rows, "week"));

        Assert.Equal(Monday, point.Period);
        Assert.Equal(175m, point.AvgPrice);
        Assert.Equal(90m, point.MinPrice);
        Assert.Equal(220m, point.MaxPrice);
        Assert.Equal(40, point.SampleCount);
    }

    [Fact] // TC-D-03
    public void BuildPoints_LeavesAGapForAWeekWithNoData_NotAZeroPrice()
    {
        var rows = new[]
        {
            Snapshot(Monday, 120m, 5),
            Snapshot(Monday.AddDays(7), 0m, 0),   // empty week stored with zero samples
            Snapshot(Monday.AddDays(14), 130m, 5),
        };

        var points = TrendAggregationService.BuildPoints(rows, "week");

        Assert.Equal([Monday, Monday.AddDays(14)], points.Select(p => p.Period));
        Assert.DoesNotContain(points, p => p.AvgPrice == 0m);
    }

    [Fact] // TC-D-04
    public void BuildPoints_MonthBucketGroupsByTheFirstOfTheMonth()
    {
        var rows = new[]
        {
            Snapshot(new DateOnly(2026, 9, 7), 100m, 10),
            Snapshot(new DateOnly(2026, 9, 28), 140m, 10),
            Snapshot(new DateOnly(2026, 10, 5), 160m, 10),
        };

        var points = TrendAggregationService.BuildPoints(rows, "MONTH");

        Assert.Equal([new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1)], points.Select(p => p.Period));
        Assert.Equal(120m, points[0].AvgPrice);
    }

    [Fact] // TC-D-05
    public void BuildPoints_RejectsAnUnknownBucket()
    {
        var ex = Assert.Throws<ArgumentException>(() => TrendAggregationService.BuildPoints([], "year"));

        Assert.Contains("week", ex.Message);
        Assert.Contains("month", ex.Message);
    }

    [Fact] // TC-D-06
    public async Task GetSnapshotsAsync_RejectsARangeWhereFromIsAfterTo()
    {
        await using var db = NewDb();
        var service = new TrendAggregationService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetSnapshotsAsync(Crop, null, new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1)));
    }

    [Fact] // TC-D-07
    public async Task GetSnapshotsAsync_FiltersByCropRegionAndRange_AndSkipsEmptyWeeks()
    {
        await using var db = NewDb();
        db.PriceTrendSnapshots.AddRange(
            Snapshot(Monday, 100m, 5, RegionA),
            Snapshot(Monday, 110m, 5, RegionB),
            Snapshot(Monday.AddDays(7), 0m, 0, RegionA),                    // empty
            Snapshot(Monday.AddDays(-70), 90m, 5, RegionA),                 // outside the range
            new PriceTrendSnapshot { Id = Guid.NewGuid(), CropId = Guid.NewGuid(), RegionId = RegionA, Period = Monday, AvgPrice = 1m, MinPrice = 1m, MaxPrice = 1m, SampleCount = 5 }); // other crop
        await db.SaveChangesAsync();

        var all = await new TrendAggregationService(db).GetSnapshotsAsync(Crop, null, Monday, Monday.AddDays(14));
        var regionA = await new TrendAggregationService(db).GetSnapshotsAsync(Crop, RegionA, Monday, Monday.AddDays(14));

        Assert.Equal(2, all.Count);
        Assert.Equal(RegionA, Assert.Single(regionA).RegionId);
    }

    [Fact] // TC-D-08
    public async Task AggregateAsync_OnAlreadyWeeklyData_IsARepeatableNoOpRewrite()
    {
        await using var db = NewDb();
        db.PriceTrendSnapshots.Add(Snapshot(Monday, 120m, 8));
        await db.SaveChangesAsync();
        var service = new TrendAggregationService(db);

        var first = await service.AggregateAsync(Crop, RegionA, Monday, Monday.AddDays(6));
        var second = await service.AggregateAsync(Crop, RegionA, Monday, Monday.AddDays(6));

        var row = await db.PriceTrendSnapshots.SingleAsync();
        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(120m, row.AvgPrice);
        Assert.Equal(8, row.SampleCount);
    }

    [Fact] // TC-D-09
    public async Task AggregateAsync_WithNoData_WritesNothing()
    {
        await using var db = NewDb();

        var written = await new TrendAggregationService(db).AggregateAsync(Crop, null, Monday, Monday.AddDays(6));

        Assert.Equal(0, written);
        Assert.Empty(db.PriceTrendSnapshots);
    }

    // ---------------------------------------------------------------- Anomaly detection (FR16)

    [Theory] // TC-D-10
    [InlineData(125, 80, 120, 25)]    // midpoint 100, price 25% above
    [InlineData(75, 80, 120, -25)]    // 25% below
    [InlineData(100, 80, 120, 0)]     // exactly the midpoint
    public void CalculateDeviationPercent_IsRelativeToTheMidpointOfTheSuggestedRange(
        decimal price, decimal min, decimal max, decimal expected)
    {
        Assert.Equal(expected, AnomalyDetectionService.CalculateDeviationPercent(price, min, max));
    }

    [Theory] // TC-D-11
    [InlineData(0, 0)]
    [InlineData(-10, -2)]
    public void CalculateDeviationPercent_RejectsANonPositiveSuggestedRange(decimal min, decimal max)
    {
        Assert.Throws<ArgumentException>(() => AnomalyDetectionService.CalculateDeviationPercent(100m, min, max));
    }

    [Fact] // TC-D-12 — boundary: exactly at the 25% default threshold is NOT flagged
    public async Task EvaluateListingAsync_ExactlyAtTheThreshold_IsNotFlagged()
    {
        await using var db = NewDb();

        var flag = await NewAnomalyService(db).EvaluateListingAsync(Guid.NewGuid(), Crop, RegionA, 125m, 80m, 120m);

        Assert.Null(flag);
        Assert.Empty(db.PriceAnomalyFlags);
    }

    [Fact] // TC-D-13 — boundary: just above the threshold IS flagged
    public async Task EvaluateListingAsync_JustAboveTheThreshold_CreatesAnOpenFlag()
    {
        await using var db = NewDb();
        var listingId = Guid.NewGuid();

        var flag = await NewAnomalyService(db).EvaluateListingAsync(listingId, Crop, RegionA, 125.01m, 80m, 120m);

        Assert.NotNull(flag);
        Assert.Equal(AnomalyStatus.Open, flag.Status);
        Assert.Equal(listingId, flag.ListingId);
        Assert.Equal(25.01m, flag.DeviationPercent);
    }

    [Fact] // TC-D-14
    public async Task EvaluateListingAsync_FlagsAPriceTooFarBelowTheRangeToo()
    {
        await using var db = NewDb();

        var flag = await NewAnomalyService(db).EvaluateListingAsync(Guid.NewGuid(), Crop, RegionA, 50m, 80m, 120m);

        Assert.Equal(-50m, flag!.DeviationPercent);
    }

    [Fact] // TC-D-15
    public async Task EvaluateListingAsync_UsesTheConfiguredThreshold()
    {
        await using var db = NewDb();
        var strict = NewAnomalyService(db, threshold: 10m);

        var flag = await strict.EvaluateListingAsync(Guid.NewGuid(), Crop, RegionA, 115m, 80m, 120m);

        Assert.Equal(10m, strict.ThresholdPercent);
        Assert.NotNull(flag); // 15% > 10%
    }

    [Fact] // TC-D-16
    public async Task EvaluateListingAsync_IsIdempotentWhileAFlagIsStillOpen()
    {
        await using var db = NewDb();
        var service = NewAnomalyService(db);
        var listingId = Guid.NewGuid();

        var first = await service.EvaluateListingAsync(listingId, Crop, RegionA, 200m, 80m, 120m);
        var second = await service.EvaluateListingAsync(listingId, Crop, RegionA, 210m, 80m, 120m);

        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(1, await db.PriceAnomalyFlags.CountAsync());
    }

    [Fact] // TC-D-25 — a farmer who raises the price again must not create a second flag, and the officer must see the new price
    public async Task EvaluateListingAsync_WhenTheListingIsPricedAgain_UpdatesItsOpenFlag()
    {
        await using var db = NewDb();
        var service = NewAnomalyService(db);
        var listingId = Guid.NewGuid();

        var first = await service.EvaluateListingAsync(listingId, Crop, RegionA, 200m, 80m, 120m);
        var second = await service.EvaluateListingAsync(listingId, Crop, RegionA, 300m, 80m, 120m);

        var only = await db.PriceAnomalyFlags.SingleAsync();
        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(300m, only.ListingPrice);
        Assert.Equal(200m, only.DeviationPercent);   // (300 - 100) / 100
    }

    [Fact] // TC-D-17 — the deviation column is numeric(5,2); an extreme price must not overflow it
    public async Task EvaluateListingAsync_ClampsAnExtremeDeviationToWhatTheColumnCanStore()
    {
        await using var db = NewDb();

        var flag = await NewAnomalyService(db).EvaluateListingAsync(Guid.NewGuid(), Crop, RegionA, 1_000_000m, 80m, 120m);

        Assert.Equal(999.99m, flag!.DeviationPercent);
    }

    [Theory] // TC-D-18
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetFlagsAsync_RejectsInvalidPaging(int page, int size)
    {
        await using var db = NewDb();

        await Assert.ThrowsAsync<ArgumentException>(() => NewAnomalyService(db).GetFlagsAsync(null, null, page, size));
    }

    [Fact] // TC-D-19
    public async Task GetFlagsAsync_PagesNewestFirst_AndFiltersByStatus()
    {
        await using var db = NewDb();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            db.PriceAnomalyFlags.Add(new PriceAnomalyFlag
            {
                Id = Guid.NewGuid(), ListingId = Guid.NewGuid(), CropId = Crop, RegionId = RegionA,
                ListingPrice = 100m + i, DeviationPercent = 30m, FlaggedAt = now.AddMinutes(i),
                Status = i == 0 ? AnomalyStatus.Reviewed : AnomalyStatus.Open,
            });
        }
        await db.SaveChangesAsync();
        var service = NewAnomalyService(db);

        var firstPage = await service.GetFlagsAsync("open", null, page: 1, size: 2);
        var open = await service.CountFlagsAsync("Open", null);

        Assert.Equal([104m, 103m], firstPage.Select(f => f.ListingPrice));
        Assert.Equal(4, open);
    }

    [Theory] // TC-D-20
    [InlineData("Reviewed", AnomalyStatus.Reviewed)]
    [InlineData("dismissed", AnomalyStatus.Dismissed)]
    public async Task UpdateStatusAsync_AcceptsReviewedOrDismissed_CaseInsensitively(string input, AnomalyStatus expected)
    {
        await using var db = NewDb();
        var flag = await NewAnomalyService(db).EvaluateListingAsync(Guid.NewGuid(), Crop, RegionA, 200m, 80m, 120m);

        await NewAnomalyService(db).UpdateStatusAsync(flag!.Id, input);

        Assert.Equal(expected, (await db.PriceAnomalyFlags.SingleAsync()).Status);
    }

    [Theory] // TC-D-21 — flags never go back to Open, and numeric enum values are not accepted
    [InlineData("Open")]
    [InlineData("1")]
    [InlineData("Banana")]
    public async Task UpdateStatusAsync_RejectsOpenNumbersAndUnknownNames(string input)
    {
        await using var db = NewDb();
        var flag = await NewAnomalyService(db).EvaluateListingAsync(Guid.NewGuid(), Crop, RegionA, 200m, 80m, 120m);

        await Assert.ThrowsAsync<ArgumentException>(() => NewAnomalyService(db).UpdateStatusAsync(flag!.Id, input));
        Assert.Equal(AnomalyStatus.Open, (await db.PriceAnomalyFlags.SingleAsync()).Status);
    }

    [Fact] // TC-D-22
    public async Task UpdateStatusAsync_ForAMissingFlag_ThrowsNotFound()
    {
        await using var db = NewDb();

        await Assert.ThrowsAsync<NotFoundException>(() => NewAnomalyService(db).UpdateStatusAsync(Guid.NewGuid(), "Reviewed"));
    }

    // ---------------------------------------------------------------- Shortage / oversupply (FR17)

    private static async Task<AgriConnectDbContext> SeedEventsAsync()
    {
        var db = NewDb();
        var now = DateTimeOffset.UtcNow;
        db.ShortageOversupplyEvents.AddRange(
            new ShortageOversupplyEvent { Id = Guid.NewGuid(), CropId = Crop, RegionId = RegionA, Type = SupplyEventType.Shortage, Severity = SupplyEventSeverity.High, DetectedAt = now.AddDays(-2) },
            new ShortageOversupplyEvent { Id = Guid.NewGuid(), CropId = Crop, RegionId = RegionB, Type = SupplyEventType.Oversupply, Severity = SupplyEventSeverity.Low, DetectedAt = now.AddDays(-1) },
            new ShortageOversupplyEvent { Id = Guid.NewGuid(), CropId = Guid.NewGuid(), RegionId = RegionA, Type = SupplyEventType.Shortage, Severity = SupplyEventSeverity.Low, DetectedAt = now });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact] // TC-D-23
    public async Task GetEventsAsync_ReturnsNewestFirst_AndFiltersByCropRegionTypeAndSeverity()
    {
        await using var db = await SeedEventsAsync();
        var service = new ShortageDetectionService(db);

        var everything = await service.GetEventsAsync(null, null, null, null);
        var cropOnly = await service.GetEventsAsync(Crop, null, null, null);
        var shortagesInA = await service.GetEventsAsync(Crop, RegionA, "shortage", null);
        var lowOversupply = await service.GetEventsAsync(null, null, "Oversupply", "low");

        Assert.Equal(3, everything.Count);
        Assert.True(everything[0].DetectedAt > everything[1].DetectedAt);
        Assert.Equal(2, cropOnly.Count);
        Assert.Equal(SupplyEventSeverity.High, Assert.Single(shortagesInA).Severity);
        Assert.Equal(RegionB, Assert.Single(lowOversupply).RegionId);
    }

    [Theory] // TC-D-24
    [InlineData("flood", null)]
    [InlineData(null, "extreme")]
    [InlineData("0", null)]
    public async Task GetEventsAsync_RejectsAnUnknownTypeOrSeverity(string? type, string? severity)
    {
        await using var db = NewDb();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new ShortageDetectionService(db).GetEventsAsync(null, null, type, severity));
    }
}
