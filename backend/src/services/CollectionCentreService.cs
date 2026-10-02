using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// FR21 — nearest suitable collection centre + distance estimate. Filtering is
/// region-scoped first, then sorted by distance (plan §9, DFD §6.4). A centre at
/// full capacity is still included (plan §9 default — capacity isn't a documented
/// filter for this endpoint, since a full centre may free up before pickup); its
/// Capacity is surfaced in the response instead of hiding it.
/// </summary>
public class CollectionCentreService(
    AgriConnectDbContext db, IDistanceService distanceService, IConfiguration? configuration = null)
{
    public async Task<IReadOnlyList<NearestCentreResponse>> FindNearestAsync(
        decimal lat, decimal lng, Guid? regionId, CancellationToken cancellationToken = default)
    {
        var centres = new List<CollectionCentre>();
        if (regionId.HasValue)
        {
            centres = await db.CollectionCentres.AsNoTracking()
                .Where(c => c.RegionId == regionId.Value).ToListAsync(cancellationToken);
        }

        if (centres.Count == 0)
        {
            // No region given, or no centre serves that region: rank every centre by
            // distance instead of returning nothing (FR21 - nearest suitable centre).
            centres = await db.CollectionCentres.AsNoTracking().ToListAsync(cancellationToken);
        }

        // Distances are looked up in parallel under one overall time budget: the
        // per-call retry/timeout in DistanceService is fine for one centre but would
        // add up to a minute-long hang across many sequential centres when the Maps
        // provider is unreachable. Anything not answered within the budget falls back
        // to a straight-line estimate flagged Degraded (plan §9).
        var budgetSeconds = configuration?.GetValue("MapsApi:TotalBudgetSeconds", 8) ?? 8;
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(budgetSeconds));

        var lookups = centres.Select(async centre =>
        {
            DistanceResult distance;
            try
            {
                distance = await distanceService.GetDistanceAsync(
                    lat, lng, centre.Latitude, centre.Longitude, budget.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var km = HaversineCalculator.DistanceKm((double)lat, (double)lng, (double)centre.Latitude, (double)centre.Longitude);
                distance = new DistanceResult((decimal)km, EtaMinutes: null, Degraded: true);
            }

            return new NearestCentreResponse(
                centre.Id, centre.Name, distance.DistanceKm, distance.EtaMinutes, centre.Capacity, distance.Degraded);
        }).ToList();

        var results = await Task.WhenAll(lookups);
        return results.OrderBy(r => r.DistanceKm).ToList();
    }

    /// <summary>Plain listing, no distance calculation — backs the Officer
    /// scheduling calendar's centre selector (Design.md §33), which needs a
    /// simple "pick a centre" list rather than a nearest-to-coordinates query.</summary>
    public async Task<IReadOnlyList<CollectionCentreResponse>> ListAllAsync(
        Guid? officerId = null, CancellationToken cancellationToken = default)
    {
        var scope = officerId is { } id ? await OfficerScope.ForAsync(db, id, cancellationToken) : OfficerScope.Unscoped;

        return await db.CollectionCentres
            .AsNoTracking()
            .Where(c => !scope.IsScoped || c.Id == scope.CentreId)
            .OrderBy(c => c.Name)
            .Select(c => new CollectionCentreResponse(c.Id, c.Name, c.Latitude, c.Longitude, c.Capacity, c.RegionId))
            .ToListAsync(cancellationToken);
    }
}
