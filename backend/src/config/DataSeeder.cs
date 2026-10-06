using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriConnect.Api.Config;

/// <summary>
/// Database seeder for Component B (Order &amp; Collection-Centre Logistics).
///
/// Idempotently populates the database with collection centres and demo orders
/// (with their reservations/schedules) across all five order statuses.
/// </summary>
public static class DataSeeder
{
    public record SeedResult(
        int CollectionCentresAdded,
        int ListingsAdded,
        int OrdersAdded,
        int ReservationsAdded,
        int SchedulesAdded);

    /// <summary>
    /// Seeds the Component B tables. Safe to call multiple times; existing records
    /// will not be duplicated.
    /// </summary>
    public static async Task<SeedResult> SeedAsync(
        AgriConnectDbContext context,
        ILogger? logger = null)
    {
        logger?.LogInformation("[DataSeeder] Checking Component B order/logistics fixtures...");

        // 1. Seed CollectionCentres (Idempotent: matches on Id). Existing fixture
        //    centres are also re-synced (region/name/coordinates/capacity) — earlier
        //    fixture versions used region ids that don't exist in the real Region
        //    table, so a database seeded back then still carries the stale values and
        //    scheduling could never find a centre in a listing's real region.
        var existingCentres = await context.CollectionCentres.ToDictionaryAsync(c => c.Id);
        var newCentres = new List<CollectionCentre>();
        var updatedCentres = 0;

        foreach (var fixture in OrderLogisticsFixtures.GetCollectionCentres())
        {
            if (!existingCentres.TryGetValue(fixture.Id, out var existing))
            {
                newCentres.Add(fixture);
                continue;
            }

            if (existing.RegionId != fixture.RegionId || existing.Name != fixture.Name
                || existing.Latitude != fixture.Latitude || existing.Longitude != fixture.Longitude
                || existing.Capacity != fixture.Capacity)
            {
                existing.RegionId = fixture.RegionId;
                existing.Name = fixture.Name;
                existing.Latitude = fixture.Latitude;
                existing.Longitude = fixture.Longitude;
                existing.Capacity = fixture.Capacity;
                updatedCentres++;
            }
        }

        if (newCentres.Count > 0)
        {
            await context.CollectionCentres.AddRangeAsync(newCentres);
            logger?.LogInformation("[DataSeeder] Adding {Count} collection centres.", newCentres.Count);
        }

        if (updatedCentres > 0)
        {
            logger?.LogInformation("[DataSeeder] Re-synced {Count} existing collection centres.", updatedCentres);
        }

        // 2. Seed the real demo Listing rows the fixture Orders below reference
        //    (Idempotent: matches on Id). Added 2026-09-27 — DbListingAvailabilityPort
        //    now reads the real Listing table, so these must actually exist there,
        //    not just as in-memory ListingReference records.
        var existingListingIds = (await context.Listings
            .AsNoTracking()
            .Select(l => l.Id)
            .ToListAsync())
            .ToHashSet();

        var newListings = OrderLogisticsFixtures.GetDemoListings()
            .Where(l => !existingListingIds.Contains(l.Id))
            .ToList();

        if (newListings.Count > 0)
        {
            await context.Listings.AddRangeAsync(newListings);
            logger?.LogInformation("[DataSeeder] Adding {Count} demo listings.", newListings.Count);
        }

        // 3. Seed demo Orders + StockReservations + PickupSchedules (Idempotent: matches on Id)
        var demo = OrderLogisticsFixtures.GetDemoOrders();

        var existingOrderIds = (await context.Orders
            .AsNoTracking()
            .Select(o => o.Id)
            .ToListAsync())
            .ToHashSet();

        var newOrders = demo.Orders.Where(o => !existingOrderIds.Contains(o.Id)).ToList();
        if (newOrders.Count > 0)
        {
            await context.Orders.AddRangeAsync(newOrders);
            logger?.LogInformation("[DataSeeder] Adding {Count} demo orders.", newOrders.Count);
        }

        var existingReservationIds = (await context.StockReservations
            .AsNoTracking()
            .Select(r => r.Id)
            .ToListAsync())
            .ToHashSet();

        var newReservations = demo.Reservations
            .Where(r => !existingReservationIds.Contains(r.Id))
            .ToList();
        if (newReservations.Count > 0)
        {
            await context.StockReservations.AddRangeAsync(newReservations);
            logger?.LogInformation("[DataSeeder] Adding {Count} demo stock reservations.", newReservations.Count);
        }

        var existingScheduleIds = (await context.PickupSchedules
            .AsNoTracking()
            .Select(s => s.Id)
            .ToListAsync())
            .ToHashSet();

        var newSchedules = demo.Schedules
            .Where(s => !existingScheduleIds.Contains(s.Id))
            .ToList();
        if (newSchedules.Count > 0)
        {
            await context.PickupSchedules.AddRangeAsync(newSchedules);
            logger?.LogInformation("[DataSeeder] Adding {Count} demo pickup schedules.", newSchedules.Count);
        }

        int totalNew = newCentres.Count + updatedCentres + newListings.Count + newOrders.Count + newReservations.Count + newSchedules.Count;
        if (totalNew > 0)
        {
            await context.SaveChangesAsync();
            logger?.LogInformation("[DataSeeder] Successfully saved {Count} new fixture rows to database.", totalNew);
        }
        else
        {
            logger?.LogInformation("[DataSeeder] Database already contains all Component B fixtures; no changes needed.");
        }

        return new SeedResult(
            newCentres.Count,
            newListings.Count,
            newOrders.Count,
            newReservations.Count,
            newSchedules.Count);
    }
}
