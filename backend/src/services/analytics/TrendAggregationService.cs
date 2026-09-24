using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Analytics;

/// <summary>
/// Weekly price-trend aggregation (FR15). Weeks start on Monday; a week with no data is
/// omitted, never stored or returned as a zero price.
/// </summary>
public class TrendAggregationService(AgriConnectDbContext db)
{
    /// <summary>
    /// Re-buckets snapshots in the range into Monday-start weeks, per region, and upserts
    /// one row per (CropId, RegionId, week). Returns the number of rows written.
    ///
    /// Until Component A's Listing table exists, the snapshots themselves are the only
    /// source, so on already-weekly data this is a no-op rewrite. Swap the source query
    /// for published listings once they land.
    /// </summary>
    public async Task<int> AggregateAsync(Guid cropId, Guid? regionId, DateOnly from, DateOnly to)
    {
        EnsureValidRange(from, to);

        var source = await Filter(cropId, regionId, from, to).ToListAsync();

        var buckets = source
            .GroupBy(s => (s.RegionId, Week: WeekStart(s.Period)))
            .Where(g => g.Sum(s => s.SampleCount) > 0)
            .Select(g =>
            {
                var samples = g.Sum(s => s.SampleCount);
                return new
                {
                    g.Key.RegionId,
                    g.Key.Week,
                    // Weighted by sample count: a week built from 30 listings outweighs one from 3.
                    Avg = Math.Round(g.Sum(s => s.AvgPrice * s.SampleCount) / samples, 2),
                    Min = g.Min(s => s.MinPrice),
                    Max = g.Max(s => s.MaxPrice),
                    Samples = samples,
                };
            })
            .ToList();

        if (buckets.Count == 0)
        {
            return 0;
        }

        // A bucket's Monday can fall before 'from' when 'from' is mid-week.
        var targets = await Filter(cropId, regionId, WeekStart(from), to)
            .ToDictionaryAsync(s => (s.RegionId, s.Period));

        foreach (var b in buckets)
        {
            if (!targets.TryGetValue((b.RegionId, b.Week), out var row))
            {
                row = new PriceTrendSnapshot
                {
                    Id = Guid.NewGuid(),
                    CropId = cropId,
                    RegionId = b.RegionId,
                    Period = b.Week,
                };
                db.PriceTrendSnapshots.Add(row);
            }

            row.AvgPrice = b.Avg;
            row.MinPrice = b.Min;
            row.MaxPrice = b.Max;
            row.SampleCount = b.Samples;
        }

        await db.SaveChangesAsync();
        return buckets.Count;
    }

    public async Task<List<PriceTrendSnapshot>> GetSnapshotsAsync(
        Guid cropId, Guid? regionId, DateOnly from, DateOnly to)
    {
        EnsureValidRange(from, to);

        return await Filter(cropId, regionId, from, to)
            .AsNoTracking()
            .Where(s => s.SampleCount > 0)
            .OrderBy(s => s.RegionId)
            .ThenBy(s => s.Period)
            .ToListAsync();
    }

    public static DateOnly WeekStart(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private IQueryable<PriceTrendSnapshot> Filter(Guid cropId, Guid? regionId, DateOnly from, DateOnly to)
    {
        var query = db.PriceTrendSnapshots
            .Where(s => s.CropId == cropId && s.Period >= from && s.Period <= to);

        return regionId is { } id ? query.Where(s => s.RegionId == id) : query;
    }

    private static void EnsureValidRange(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("'from' must be earlier than or equal to 'to'.");
        }
    }
}
