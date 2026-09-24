using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos.Analytics;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Analytics;

/// <summary>
/// Explains why a listing was flagged (FR16): combines the flag, the crop's regional
/// price history, and — once Components B and C land — order and inspection history,
/// then ranks the likely causes.
/// </summary>
public class AnomalyInvestigationService(AgriConnectDbContext db, AnomalyDetectionService anomalies)
{
    private const decimal DataEntryErrorDeviation = 40m;
    private const decimal SignificantDeviation = 25m;
    private const int PremiumPercentile = 90;

    public async Task<InvestigationResultDto> InvestigateAsync(Guid listingId)
    {
        var flag = await anomalies.GetFlagByListingIdAsync(listingId)
            ?? throw new NotFoundException($"No anomaly flag exists for listing {listingId}.");

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
            InspectionContext = Unavailable("Component C inspection data not yet integrated"),
            OrderContext = Unavailable("Component B order data not yet integrated"),
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
            ListingPrice = flag.ListingPrice,
            PercentileInRegion = percentile,
        };
    }

    public static List<LikelyCauseDto> RankLikelyCauses(decimal deviationPercent, int percentileInRegion)
    {
        var causes = new List<LikelyCauseDto>();

        if (deviationPercent > DataEntryErrorDeviation)
        {
            causes.Add(Cause("PotentialDataEntryError", "High",
                "Price is more than 40% above the AI-suggested midpoint, which may indicate an input error."));
        }
        if (deviationPercent > SignificantDeviation && percentileInRegion > PremiumPercentile)
        {
            causes.Add(Cause("PremiumQualityGrade", "Medium",
                "Listing is priced in the top 10% for this region. Seller may be claiming premium grade."));
        }
        if (deviationPercent < -SignificantDeviation)
        {
            causes.Add(Cause("DistressedSale", "Medium",
                "Price is significantly below fair range. Farmer may be under pressure to sell quickly."));
        }
        if (causes.Count == 0)
        {
            causes.Add(Cause("MarketVolatility", "Low",
                "No specific pattern identified. Market fluctuation is the most likely cause."));
        }

        return causes;
    }

    private static LikelyCauseDto Cause(string cause, string confidence, string explanation) =>
        new() { Cause = cause, Confidence = confidence, Explanation = explanation };

    private static ExternalContextDto Unavailable(string reason) =>
        new() { Available = false, Reason = reason };
}
