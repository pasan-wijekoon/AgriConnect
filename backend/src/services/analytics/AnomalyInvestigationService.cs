using System.Globalization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos.Analytics;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Analytics;

/// <summary>
/// Explains why a listing was flagged (FR16) in words an officer can act on: how the asking
/// price compares with the AI fair price and the regional history, what the listing's
/// inspection and orders look like, and the likely reasons, most likely first.
/// </summary>
public class AnomalyInvestigationService(AgriConnectDbContext db, AnomalyDetectionService anomalies)
{
    private const decimal DataEntryErrorDeviation = 40m;
    private const decimal SignificantDeviation = 25m;
    private const int PremiumPercentile = 90;

    // Matches the numeric(5,2) deviation column; a clamped value no longer carries the real ratio.
    private const decimal MaxStoredDeviation = 999.99m;

    public async Task<InvestigationResultDto> InvestigateAsync(Guid listingId)
    {
        var flag = await anomalies.GetFlagByListingIdAsync(listingId)
            ?? throw new NotFoundException($"No anomaly flag exists for listing {listingId}.");

        var listing = await db.Listings.AsNoTracking()
            .Where(l => l.Id == listingId)
            .Select(l => new { l.ClaimedGrade, l.Unit })
            .FirstOrDefaultAsync();

        var priceContext = await BuildPriceContextAsync(flag);

        return new InvestigationResultDto
        {
            ListingId = listingId,
            Flag = new FlagSummaryDto
            {
                DeviationPercent = flag.DeviationPercent,
                FlaggedAt = flag.FlaggedAt,
                Status = flag.Status.ToString(),
            },
            PriceContext = priceContext,
            InspectionContext = await BuildInspectionContextAsync(listingId, listing?.ClaimedGrade),
            OrderContext = await BuildOrderContextAsync(listingId, listing?.Unit ?? "kg"),
            LikelyCauses = RankLikelyCauses(flag.DeviationPercent, priceContext.PercentileInRegion),
        };
    }

    private async Task<PriceContextDto> BuildPriceContextAsync(PriceAnomalyFlag flag)
    {
        var history = await db.PriceTrendSnapshots
            .AsNoTracking()
            .Where(s => s.CropId == flag.CropId && s.RegionId == flag.RegionId && s.SampleCount > 0)
            .Select(s => new { s.Period, s.AvgPrice })
            .ToListAsync();

        // The week containing FlaggedAt; if that week is a gap, the most recent week before it.
        var flaggedOn = DateOnly.FromDateTime(flag.FlaggedAt.UtcDateTime);
        var regionalAvg = history
            .Where(s => s.Period <= flaggedOn)
            .OrderByDescending(s => s.Period)
            .Select(s => (decimal?)s.AvgPrice)
            .FirstOrDefault();

        var percentile = history.Count == 0
            ? 0
            : (int)Math.Round(100.0 * history.Count(s => s.AvgPrice < flag.ListingPrice) / history.Count);

        return new PriceContextDto
        {
            RegionalAvgPrice = regionalAvg,
            AiFairPrice = RecoverAiFairPrice(flag.ListingPrice, flag.DeviationPercent),
            ListingPrice = flag.ListingPrice,
            PercentileInRegion = percentile,
        };
    }

    /// <summary>
    /// The flag stores the deviation, not the AI range, so the AI fair price (the midpoint of
    /// its range) is recovered from deviation = (price - fair) / fair * 100. Null when the
    /// stored deviation was clamped and the ratio is no longer exact.
    /// </summary>
    public static decimal? RecoverAiFairPrice(decimal listingPrice, decimal deviationPercent)
    {
        if (Math.Abs(deviationPercent) >= MaxStoredDeviation)
        {
            return null;
        }

        var ratio = 1 + deviationPercent / 100;
        return ratio > 0 ? Math.Round(listingPrice / ratio, 2) : null;
    }

    private async Task<ExternalContextDto> BuildInspectionContextAsync(Guid listingId, string? claimedGrade)
    {
        var latest = await db.Inspections.AsNoTracking()
            .Where(i => i.ListingId == listingId)
            .OrderByDescending(i => i.InspectedAt)
            .FirstOrDefaultAsync();
        if (latest is null)
        {
            return Known("Not inspected yet.");
        }

        var when = latest.InspectedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var matches = string.IsNullOrWhiteSpace(claimedGrade)
            || string.Equals(claimedGrade, latest.ConfirmedGrade, StringComparison.OrdinalIgnoreCase);

        return Known(matches
            ? $"Inspected on {when}: {latest.ConfirmedGrade} confirmed."
            : $"Inspected on {when}: the officer confirmed {latest.ConfirmedGrade}, but the farmer claimed {claimedGrade}.");
    }

    private async Task<ExternalContextDto> BuildOrderContextAsync(Guid listingId, string unit)
    {
        var orders = await db.Orders.AsNoTracking()
            .Where(o => o.ListingId == listingId)
            .Select(o => new { o.Quantity, o.CreatedAt })
            .ToListAsync();
        if (orders.Count == 0)
        {
            return Known("No orders yet.");
        }

        var latest = orders.Max(o => o.CreatedAt).ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        var noun = orders.Count == 1 ? "order" : "orders";
        return Known($"{orders.Count} {noun}, {orders.Sum(o => o.Quantity):0.##} {unit} in total. Latest order: {latest}.");
    }

    public static List<LikelyCauseDto> RankLikelyCauses(decimal deviationPercent, int percentileInRegion)
    {
        var causes = new List<LikelyCauseDto>();
        var direction = deviationPercent >= 0 ? "above" : "below";

        if (Math.Abs(deviationPercent) > DataEntryErrorDeviation)
        {
            causes.Add(Cause("PotentialDataEntryError", "High",
                $"The price is more than 40% {direction} the AI fair price. A digit may have been typed wrongly."));
        }
        if (deviationPercent > SignificantDeviation && percentileInRegion > PremiumPercentile)
        {
            causes.Add(Cause("PremiumQualityGrade", "Medium",
                "This is one of the highest prices in the region. The farmer may be asking extra for top quality."));
        }
        if (deviationPercent < -SignificantDeviation)
        {
            causes.Add(Cause("DistressedSale", "Medium",
                "The price is far below the fair price. The farmer may need to sell quickly."));
        }
        if (causes.Count == 0)
        {
            causes.Add(Cause("MarketVolatility", "Low",
                "Nothing unusual found. Market prices move up and down, so this is the most likely reason."));
        }

        return causes;
    }

    private static LikelyCauseDto Cause(string cause, string confidence, string explanation) =>
        new() { Cause = cause, Confidence = confidence, Explanation = explanation };

    private static ExternalContextDto Known(string summary) =>
        new() { Available = true, Summary = summary };
}
