using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using AgriConnect.Api.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.services;

/// <summary>
/// The officer's "see details" panel for a flagged listing (FR16): the AI fair price it is
/// compared with, plain-language likely causes, and the listing's inspection and order
/// history. Test ids TC-D-6x match the Test Case Document.
/// </summary>
public class AnomalyInvestigationTests
{
    private static readonly Guid Crop = Guid.NewGuid();
    private static readonly Guid Region = Guid.NewGuid();
    private static readonly DateOnly Monday = new(2026, 9, 21);

    private static AgriConnectDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AnomalyInvestigationService NewService(AgriConnectDbContext db) =>
        new(db, new AnomalyDetectionService(db, new ConfigurationBuilder().Build()));

    private static async Task<Guid> SeedFlaggedListingAsync(
        AgriConnectDbContext db, decimal price = 168m, decimal deviation = 52.7m, string claimedGrade = "Grade A")
    {
        var listingId = Guid.NewGuid();
        db.Listings.Add(new Listing { Id = listingId, FarmerId = Guid.NewGuid(), CropId = Crop, RegionId = Region, Quantity = 100, Unit = "kg", ClaimedGrade = claimedGrade });
        db.PriceAnomalyFlags.Add(new PriceAnomalyFlag
        {
            Id = Guid.NewGuid(), ListingId = listingId, CropId = Crop, RegionId = Region,
            ListingPrice = price, DeviationPercent = deviation, FlaggedAt = new DateTimeOffset(2026, 9, 27, 7, 5, 0, TimeSpan.Zero),
            Status = AnomalyStatus.Open,
        });
        db.PriceTrendSnapshots.AddRange(
            new PriceTrendSnapshot { Id = Guid.NewGuid(), CropId = Crop, RegionId = Region, Period = Monday, AvgPrice = 120m, MinPrice = 110m, MaxPrice = 130m, SampleCount = 10 },
            new PriceTrendSnapshot { Id = Guid.NewGuid(), CropId = Crop, RegionId = Region, Period = Monday.AddDays(-7), AvgPrice = 100m, MinPrice = 90m, MaxPrice = 110m, SampleCount = 10 });
        await db.SaveChangesAsync();
        return listingId;
    }

    [Theory] // TC-D-60
    [InlineData(168, 52.7, 110.02)]   // the officer-panel example: LKR 168 is 52.7% above about LKR 110
    [InlineData(49, -30, 70)]
    [InlineData(125, 25, 100)]
    public void RecoverAiFairPrice_WorksBackFromThePriceAndTheDeviation(decimal price, decimal deviation, decimal expected)
    {
        Assert.Equal(expected, AnomalyInvestigationService.RecoverAiFairPrice(price, deviation));
    }

    [Theory] // TC-D-61 — a clamped or impossible deviation cannot give an honest fair price
    [InlineData(1_000_000, 999.99)]
    [InlineData(1_000_000, -999.99)]
    [InlineData(10, -100)]
    public void RecoverAiFairPrice_ReturnsNullWhenTheRatioIsNotExact(decimal price, decimal deviation)
    {
        Assert.Null(AnomalyInvestigationService.RecoverAiFairPrice(price, deviation));
    }

    [Fact] // TC-D-62
    public void RankLikelyCauses_ForAHighPriceAtTheTopOfTheRegion_PutsATypingMistakeFirst()
    {
        var causes = AnomalyInvestigationService.RankLikelyCauses(52.7m, percentileInRegion: 100);

        Assert.Equal(["PotentialDataEntryError", "PremiumQualityGrade"], causes.Select(c => c.Cause));
        Assert.Equal("High", causes[0].Confidence);
        Assert.Contains("above the AI fair price", causes[0].Explanation);
    }

    [Fact] // TC-D-63 — a price far BELOW fair can be a missing digit too, not only a distressed sale
    public void RankLikelyCauses_ForAVeryLowPrice_AlsoSuggestsATypingMistake()
    {
        var causes = AnomalyInvestigationService.RankLikelyCauses(-60m, percentileInRegion: 0);

        Assert.Equal(["PotentialDataEntryError", "DistressedSale"], causes.Select(c => c.Cause));
        Assert.Contains("below the AI fair price", causes[0].Explanation);
    }

    [Fact] // TC-D-64
    public void RankLikelyCauses_ForAModerateLowPrice_SuggestsAFastSaleOnly()
    {
        var causes = AnomalyInvestigationService.RankLikelyCauses(-30m, percentileInRegion: 5);

        Assert.Equal("DistressedSale", Assert.Single(causes).Cause);
    }

    [Theory] // TC-D-65 — boundaries: exactly 40% is not a typing mistake; a small deviation is just market movement
    [InlineData(40, 50)]
    [InlineData(10, 50)]
    public void RankLikelyCauses_BelowTheThresholds_FallsBackToNormalMarketMovement(decimal deviation, int percentile)
    {
        var cause = Assert.Single(AnomalyInvestigationService.RankLikelyCauses(deviation, percentile));

        Assert.Equal("MarketVolatility", cause.Cause);
        Assert.Equal("Low", cause.Confidence);
    }

    [Fact] // TC-D-66
    public async Task Investigate_ComparesTheAskingPriceWithTheAiFairPriceAndTheRegionalAverage()
    {
        await using var db = NewDb();
        var listingId = await SeedFlaggedListingAsync(db);

        var result = await NewService(db).InvestigateAsync(listingId);

        Assert.Equal(168m, result.PriceContext.ListingPrice);
        Assert.Equal(110.02m, result.PriceContext.AiFairPrice);
        Assert.Equal(120m, result.PriceContext.RegionalAvgPrice);   // the week containing the flag date
        Assert.Equal(100, result.PriceContext.PercentileInRegion);  // dearer than every weekly average
    }

    [Fact] // TC-D-67
    public async Task Investigate_WithNoInspectionOrOrders_SaysSoInPlainWords()
    {
        await using var db = NewDb();
        var listingId = await SeedFlaggedListingAsync(db);

        var result = await NewService(db).InvestigateAsync(listingId);

        Assert.True(result.InspectionContext.Available);
        Assert.Equal("Not inspected yet.", result.InspectionContext.Summary);
        Assert.Equal("No orders yet.", result.OrderContext.Summary);
    }

    [Fact] // TC-D-68 — the officer sees when the inspector disagreed with the farmer's claimed grade
    public async Task Investigate_ReportsAnInspectionThatDiffersFromTheClaimedGrade()
    {
        await using var db = NewDb();
        var listingId = await SeedFlaggedListingAsync(db, claimedGrade: "Grade A");
        db.Inspections.Add(new Inspection { Id = Guid.NewGuid(), ListingId = listingId, OfficerId = Guid.NewGuid(), ConfirmedGrade = "Grade B", InspectedAt = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();

        var result = await NewService(db).InvestigateAsync(listingId);

        Assert.Equal("Inspected on 2 Oct 2026: the officer confirmed Grade B, but the farmer claimed Grade A.", result.InspectionContext.Summary);
    }

    [Fact] // TC-D-69
    public async Task Investigate_ReportsAnInspectionThatMatchesTheClaimedGrade()
    {
        await using var db = NewDb();
        var listingId = await SeedFlaggedListingAsync(db, claimedGrade: "Grade A");
        db.Inspections.Add(new Inspection { Id = Guid.NewGuid(), ListingId = listingId, OfficerId = Guid.NewGuid(), ConfirmedGrade = "Grade A", InspectedAt = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();

        var result = await NewService(db).InvestigateAsync(listingId);

        Assert.Equal("Inspected on 2 Oct 2026: Grade A confirmed.", result.InspectionContext.Summary);
    }

    [Fact] // TC-D-70
    public async Task Investigate_SummarisesTheListingsOrders()
    {
        await using var db = NewDb();
        var listingId = await SeedFlaggedListingAsync(db);
        db.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), ListingId = listingId, BuyerId = Guid.NewGuid(), Quantity = 30m, CreatedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero) },
            new Order { Id = Guid.NewGuid(), ListingId = listingId, BuyerId = Guid.NewGuid(), Quantity = 30m, CreatedAt = new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero) },
            new Order { Id = Guid.NewGuid(), ListingId = Guid.NewGuid(), BuyerId = Guid.NewGuid(), Quantity = 999m, CreatedAt = DateTimeOffset.UtcNow }); // another listing
        await db.SaveChangesAsync();

        var result = await NewService(db).InvestigateAsync(listingId);

        Assert.Equal("2 orders, 60 kg in total. Latest order: 3 Oct 2026.", result.OrderContext.Summary);
    }

    [Fact] // TC-D-71
    public async Task Investigate_ForAListingThatWasNeverFlagged_ThrowsNotFound()
    {
        await using var db = NewDb();

        await Assert.ThrowsAsync<NotFoundException>(() => NewService(db).InvestigateAsync(Guid.NewGuid()));
    }
}
