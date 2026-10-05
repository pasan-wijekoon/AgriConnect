using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// Collection-centre scoping for Officers (CLAUDE.md §20 — "an Officer must not
/// access another centre's data unless explicitly authorized"). An Officer bound
/// to a centre (<see cref="User.CollectionCentreId"/>) only sees/acts on orders
/// for listings in that centre's region, or orders whose schedule is booked at
/// that centre. An Officer with no binding is unscoped (sees everything) — the
/// pre-existing behaviour, kept so accounts created before centre binding existed
/// keep working.
/// </summary>
public sealed record OfficerScope(Guid? CentreId, Guid? RegionId)
{
    public static readonly OfficerScope Unscoped = new(null, null);

    public bool IsScoped => CentreId.HasValue;

    public static async Task<OfficerScope> ForAsync(
        AgriConnectDbContext db, Guid userId, CancellationToken ct = default)
    {
        var centreId = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CollectionCentreId)
            .FirstOrDefaultAsync(ct);

        if (centreId is null)
        {
            return Unscoped;
        }

        var regionId = await db.CollectionCentres
            .AsNoTracking()
            .Where(c => c.Id == centreId)
            .Select(c => (Guid?)c.RegionId)
            .FirstOrDefaultAsync(ct);

        return new OfficerScope(centreId, regionId);
    }

    public IQueryable<Order> Apply(AgriConnectDbContext db, IQueryable<Order> orders)
    {
        if (!IsScoped)
        {
            return orders;
        }

        var centreId = CentreId!.Value;
        var regionId = RegionId;
        return orders.Where(o =>
            (regionId != null && db.Listings.Any(l => l.Id == o.ListingId && l.RegionId == regionId))
            || (o.PickupSchedule != null && o.PickupSchedule.CollectionCentreId == centreId));
    }

    public async Task<bool> CanAccessAsync(AgriConnectDbContext db, Guid orderId, CancellationToken ct = default)
    {
        if (!IsScoped)
        {
            return true;
        }

        return await Apply(db, db.Orders.AsNoTracking().Where(o => o.Id == orderId)).AnyAsync(ct);
    }
}
