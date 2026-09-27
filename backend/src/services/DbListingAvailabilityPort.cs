using AgriConnect.Api.Config;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// Real, EF-Core-backed implementation of <see cref="IListingAvailabilityPort"/>
/// against Component A's actual <c>Listing</c> table. Replaces
/// <see cref="FixtureListingAvailabilityPort"/> now that the table exists —
/// found during a full-system integration audit (2026-09-27) that the fixture
/// was still wired in Program.cs, meaning orders could only ever be placed
/// against Component B's own hardcoded demo listing ids, never a real listing
/// created through the marketplace. <see cref="ListingAvailability.AvailableQuantity"/>
/// is the listing's total quantity, not "quantity minus existing reservations" —
/// <see cref="StockReservationService"/> already computes and subtracts active
/// reservations itself against whatever total this returns.
/// </summary>
public class DbListingAvailabilityPort(AgriConnectDbContext db) : IListingAvailabilityPort
{
    public async Task<ListingAvailability?> GetAvailabilityAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings
            .AsNoTracking()
            .Where(l => l.Id == listingId)
            .Select(l => new ListingAvailability(l.Id, l.FarmerId, l.RegionId, l.Status, l.Quantity))
            .FirstOrDefaultAsync(cancellationToken);

        return listing;
    }
}
