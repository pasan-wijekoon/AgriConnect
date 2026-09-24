using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// FR21 — nearest suitable collection centre + distance estimate. Filtering is
/// region-scoped first, then sorted by distance (plan §9, DFD §6.4). A centre at
/// full capacity is still included (plan §9 default — capacity isn't a documented
/// filter for this endpoint, since a full centre may free up before pickup); its
/// Capacity is surfaced in the response instead of hiding it.
/// </summary>
public class CollectionCentreService(AgriConnectDbContext db, IDistanceService distanceService)
{
    public async Task<IReadOnlyList<NearestCentreResponse>> FindNearestAsync(
        decimal lat, decimal lng, Guid? regionId, CancellationToken cancellationToken = default)
    {
        var query = db.CollectionCentres.AsNoTracking().AsQueryable();
        if (regionId.HasValue)
        {
            query = query.Where(c => c.RegionId == regionId.Value);
        }

        var centres = await query.ToListAsync(cancellationToken);

        var results = new List<NearestCentreResponse>(centres.Count);
        foreach (var centre in centres)
        {
            var distance = await distanceService.GetDistanceAsync(
                lat, lng, centre.Latitude, centre.Longitude, cancellationToken);

            results.Add(new NearestCentreResponse(
                centre.Id, centre.Name, distance.DistanceKm, distance.EtaMinutes, centre.Capacity, distance.Degraded));
        }

        return results.OrderBy(r => r.DistanceKm).ToList();
    }

    /// <summary>Plain listing, no distance calculation — backs the Officer
    /// scheduling calendar's centre selector (Design.md §33), which needs a
    /// simple "pick a centre" list rather than a nearest-to-coordinates query.</summary>
    public async Task<IReadOnlyList<CollectionCentreResponse>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.CollectionCentres
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CollectionCentreResponse(c.Id, c.Name, c.Latitude, c.Longitude, c.Capacity, c.RegionId))
            .ToListAsync(cancellationToken);
    }
}
