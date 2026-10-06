using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos.Analytics;
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

    public static readonly IReadOnlyList<string> Buckets = ["week", "month"];

    /// <summary>
    /// Merges snapshot rows into one point per period (Monday-start week, or the 1st of the
    /// month), combining regions. Averages are weighted by sample count; empty periods are
    /// never produced because only periods that have rows appear.
    /// </summary>
    public static List<PriceTrendPointDto> BuildPoints(IEnumerable<PriceTrendSnapshot> rows, string bucket)
    {
        var monthly = bucket.Equals("month", StringComparison.OrdinalIgnoreCase);
        if (!monthly && !bucket.Equals("week", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unknown bucket '{bucket}'. Expected one of: {string.Join(", ", Buckets)}.");
        }

        return rows
            .Where(s => s.SampleCount > 0)
            .GroupBy(s => monthly ? new DateOnly(s.Period.Year, s.Period.Month, 1) : WeekStart(s.Period))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var samples = g.Sum(s => s.SampleCount);
                return new PriceTrendPointDto
                {
                    Period = g.Key,
                    AvgPrice = Math.Round(g.Sum(s => s.AvgPrice * s.SampleCount) / samples, 2),
                    MinPrice = g.Min(s => s.MinPrice),
                    MaxPrice = g.Max(s => s.MaxPrice),
                    SampleCount = samples,
                };
            })
            .ToList();
    }

    /// <summary>Re-aggregates every crop × region combination over all stored history.</summary>
    public async Task<SnapshotRefreshResultDto> RefreshAllAsync()
    {
        var range = await db.PriceTrendSnapshots
            .GroupBy(_ => 1)
            .Select(g => new { From = g.Min(s => s.Period), To = g.Max(s => s.Period) })
            .FirstOrDefaultAsync();
        if (range is null)
        {
            return new SnapshotRefreshResultDto();
        }

        var cropIds = await db.Crops.Select(c => c.Id).ToListAsync();
        var regionIds = await db.Regions.Select(r => r.Id).ToListAsync();

        var upserted = 0;
        foreach (var cropId in cropIds)
        {
            foreach (var regionId in regionIds)
            {
                upserted += await AggregateAsync(cropId, regionId, range.From, range.To);
            }
        }

        var periods = await db.PriceTrendSnapshots.Select(s => s.Period).Distinct().CountAsync();
        return new SnapshotRefreshResultDto { PeriodsProcessed = periods, SnapshotsUpserted = upserted };
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
