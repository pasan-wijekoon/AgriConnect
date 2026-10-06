using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>One reservation that currently holds stock back from other buyers. A class with
/// init properties (not a positional record) so EF Core can keep composing queries on it.</summary>
public sealed class ActiveReservation
{
    public Guid ListingId { get; init; }
    public decimal ReservedQuantity { get; init; }
}

/// <summary>
/// The single definition of "stock that is currently held" (FR9). A Pending order holds
/// stock only until its reservation expires; once an Officer has approved it
/// (Approved/Scheduled/Completed) the stock stays committed regardless of ExpiresAt.
/// <c>Listing.Quantity</c> is never decremented, so this sum is the only thing that
/// keeps stock from being resold. Shared by <see cref="StockReservationService"/> (the
/// check that guards ordering) and the listing queries (the "available now" figure
/// clients display), so the two can never disagree.
/// </summary>
public static class ReservationQueries
{
    public static IQueryable<ActiveReservation> ActiveReservations(AgriConnectDbContext db, DateTimeOffset now) =>
        db.StockReservations
            .Join(db.Orders, r => r.OrderId, o => o.Id,
                (r, o) => new { r.ListingId, r.ReservedQuantity, r.ExpiresAt, o.Status })
            .Where(x => x.Status == OrderStatus.Approved
                        || x.Status == OrderStatus.Scheduled
                        || x.Status == OrderStatus.Completed
                        || (x.Status == OrderStatus.Pending && x.ExpiresAt > now))
            .Select(x => new ActiveReservation { ListingId = x.ListingId, ReservedQuantity = x.ReservedQuantity });

    /// <summary>Held quantity per listing for the given listings, in one grouped query.</summary>
    public static async Task<Dictionary<Guid, decimal>> ActiveReservedByListingAsync(
        AgriConnectDbContext db, IReadOnlyCollection<Guid> listingIds, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (listingIds.Count == 0)
        {
            return [];
        }

        return await ActiveReservations(db, now)
            .Where(r => listingIds.Contains(r.ListingId))
            .GroupBy(r => r.ListingId)
            .Select(g => new { ListingId = g.Key, Reserved = g.Sum(r => r.ReservedQuantity) })
            .ToDictionaryAsync(x => x.ListingId, x => x.Reserved, cancellationToken);
    }
}
