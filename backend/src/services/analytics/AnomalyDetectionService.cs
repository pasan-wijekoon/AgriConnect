using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Analytics;

/// <summary>
/// Flags listings priced too far from the AI-suggested fair range, and serves the
/// officer review queue (FR16).
/// </summary>
public class AnomalyDetectionService(AgriConnectDbContext db, IConfiguration config)
{
    public const int MaxPageSize = 100;

    // DeviationPercent is numeric(5,2); anything wider would overflow the column.
    private const decimal MaxStorableDeviation = 999.99m;

    public decimal ThresholdPercent =>
        config.GetValue<decimal?>("Analytics:AnomalyThresholdPercent") ?? 25.0m;

    public static decimal CalculateDeviationPercent(decimal price, decimal aiSuggestedMin, decimal aiSuggestedMax)
    {
        var midpoint = (aiSuggestedMin + aiSuggestedMax) / 2;
        if (midpoint <= 0)
        {
            throw new ArgumentException("The AI-suggested price range must be positive.");
        }

        return (price - midpoint) / midpoint * 100;
    }

    /// <summary>
    /// Flags the listing when its deviation exceeds the threshold. Idempotent: if the
    /// listing already has an Open flag, that flag is returned and nothing is written.
    /// Returns null when the price is within the threshold.
    /// </summary>
    public async Task<PriceAnomalyFlag?> EvaluateListingAsync(
        Guid listingId, Guid cropId, Guid regionId,
        decimal price, decimal aiSuggestedMin, decimal aiSuggestedMax)
    {
        var deviation = CalculateDeviationPercent(price, aiSuggestedMin, aiSuggestedMax);
        if (Math.Abs(deviation) <= ThresholdPercent)
        {
            return null;
        }

        var open = await db.PriceAnomalyFlags
            .FirstOrDefaultAsync(f => f.ListingId == listingId && f.Status == AnomalyStatus.Open);
        if (open is not null)
        {
            return open;
        }

        var flag = new PriceAnomalyFlag
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            CropId = cropId,
            RegionId = regionId,
            ListingPrice = price,
            DeviationPercent = Math.Round(
                Math.Clamp(deviation, -MaxStorableDeviation, MaxStorableDeviation), 2),
            FlaggedAt = DateTimeOffset.UtcNow,
            Status = AnomalyStatus.Open,
        };

        db.PriceAnomalyFlags.Add(flag);
        await db.SaveChangesAsync();
        return flag;
    }

    public async Task<List<PriceAnomalyFlag>> GetFlagsAsync(string? status, Guid? cropId, int page, int size)
    {
        if (page < 1)
        {
            throw new ArgumentException("'page' must be 1 or greater.");
        }
        if (size is < 1 or > MaxPageSize)
        {
            throw new ArgumentException($"'size' must be between 1 and {MaxPageSize}.");
        }

        return await FilterFlags(status, cropId)
            .OrderByDescending(f => f.FlaggedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    /// <summary>Total matching flags across all pages, for the review queue's pager.</summary>
    public Task<int> CountFlagsAsync(string? status, Guid? cropId) =>
        FilterFlags(status, cropId).CountAsync();

    private IQueryable<PriceAnomalyFlag> FilterFlags(string? status, Guid? cropId)
    {
        var query = db.PriceAnomalyFlags.AsNoTracking();

        if (status is not null)
        {
            var parsed = ParseStatus(status);
            query = query.Where(f => f.Status == parsed);
        }
        if (cropId is { } crop)
        {
            query = query.Where(f => f.CropId == crop);
        }

        return query;
    }

    public Task<PriceAnomalyFlag?> GetFlagByIdAsync(Guid id) =>
        db.PriceAnomalyFlags.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);

    /// <summary>A listing can be flagged again after an earlier flag was reviewed; the newest wins.</summary>
    public Task<PriceAnomalyFlag?> GetFlagByListingIdAsync(Guid listingId) =>
        db.PriceAnomalyFlags.AsNoTracking()
            .Where(f => f.ListingId == listingId)
            .OrderByDescending(f => f.FlaggedAt)
            .FirstOrDefaultAsync();

    /// <summary>Officer triage. Only Reviewed or Dismissed are accepted; flags never go back to Open.</summary>
    public async Task UpdateStatusAsync(Guid id, string newStatus)
    {
        var parsed = ParseStatus(newStatus);
        if (parsed == AnomalyStatus.Open)
        {
            throw new ArgumentException("Status can only be changed to Reviewed or Dismissed.");
        }

        var flag = await db.PriceAnomalyFlags.FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NotFoundException($"Anomaly flag {id} was not found.");

        flag.Status = parsed;
        await db.SaveChangesAsync();
    }

    private static AnomalyStatus ParseStatus(string value) => EnumInput.Parse<AnomalyStatus>(value, "status");
}
