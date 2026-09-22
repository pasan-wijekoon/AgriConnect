using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriConnect.Api.Config;

/// <summary>
/// Database seeder for Component D (Market Price Analytics &amp; Reporting).
/// 
/// Idempotently populates the database with synthetic price history, reference snapshots,
/// sample price anomaly flags, supply events, and audit records.
/// </summary>
public static class DataSeeder
{
    public record SeedResult(
        int SnapshotsAdded,
        int AnomalyFlagsAdded,
        int SupplyEventsAdded,
        int ReportExportsAdded);

    /// <summary>
    /// Seeds the Component D tables. Safe to call multiple times; existing records will not be duplicated.
    /// </summary>
    /// <param name="context">The database context to seed into.</param>
    /// <param name="logger">Optional logger for outputting progress.</param>
    /// <returns>A summary of rows added per table.</returns>
    public static async Task<SeedResult> SeedAsync(
        AgriConnectDbContext context,
        ILogger? logger = null)
    {
        logger?.LogInformation("[DataSeeder] Checking Component D analytics fixtures...");

        // 1. Seed PriceTrendSnapshots (Idempotent: matches on unique CropId + RegionId + Period)
        var existingSnapshotKeys = (await context.PriceTrendSnapshots
            .AsNoTracking()
            .Select(s => new { s.CropId, s.RegionId, s.Period })
            .ToListAsync())
            .Select(s => (s.CropId, s.RegionId, s.Period))
            .ToHashSet();

        var candidateSnapshots = AnalyticsFixtures.GetHistoricalSnapshots();
        var newSnapshots = candidateSnapshots
            .Where(s => !existingSnapshotKeys.Contains((s.CropId, s.RegionId, s.Period)))
            .ToList();

        if (newSnapshots.Count > 0)
        {
            await context.PriceTrendSnapshots.AddRangeAsync(newSnapshots);
            logger?.LogInformation("[DataSeeder] Adding {Count} historical price trend snapshots across crops & regions.", newSnapshots.Count);
        }

        // 2. Seed ShortageOversupplyEvents (Idempotent: matches on Id)
        var existingEventIds = (await context.ShortageOversupplyEvents
            .AsNoTracking()
            .Select(e => e.Id)
            .ToListAsync())
            .ToHashSet();

        var newEvents = AnalyticsFixtures.GetSampleSupplyEvents()
            .Where(e => !existingEventIds.Contains(e.Id))
            .ToList();

        if (newEvents.Count > 0)
        {
            await context.ShortageOversupplyEvents.AddRangeAsync(newEvents);
            logger?.LogInformation("[DataSeeder] Adding {Count} sample shortage/oversupply events.", newEvents.Count);
        }

        // 3. Seed PriceAnomalyFlags (Idempotent: matches on Id)
        var existingAnomalyIds = (await context.PriceAnomalyFlags
            .AsNoTracking()
            .Select(a => a.Id)
            .ToListAsync())
            .ToHashSet();

        var newAnomalies = AnalyticsFixtures.GetSampleAnomalyFlags()
            .Where(a => !existingAnomalyIds.Contains(a.Id))
            .ToList();

        if (newAnomalies.Count > 0)
        {
            await context.PriceAnomalyFlags.AddRangeAsync(newAnomalies);
            logger?.LogInformation("[DataSeeder] Adding {Count} sample price anomaly flags.", newAnomalies.Count);
        }

        // 4. Seed ReportExports (Idempotent: matches on Id)
        var existingReportIds = (await context.ReportExports
            .AsNoTracking()
            .Select(r => r.Id)
            .ToListAsync())
            .ToHashSet();

        var newReports = AnalyticsFixtures.GetSampleReportExports()
            .Where(r => !existingReportIds.Contains(r.Id))
            .ToList();

        if (newReports.Count > 0)
        {
            await context.ReportExports.AddRangeAsync(newReports);
            logger?.LogInformation("[DataSeeder] Adding {Count} sample report export audit records.", newReports.Count);
        }

        int totalNew = newSnapshots.Count + newEvents.Count + newAnomalies.Count + newReports.Count;
        if (totalNew > 0)
        {
            await context.SaveChangesAsync();
            logger?.LogInformation("[DataSeeder] Successfully saved {Count} new fixture rows to database.", totalNew);
        }
        else
        {
            logger?.LogInformation("[DataSeeder] Database already contains all Component D fixtures; no changes needed.");
        }

        return new SeedResult(
            newSnapshots.Count,
            newAnomalies.Count,
            newEvents.Count,
            newReports.Count);
    }
}
