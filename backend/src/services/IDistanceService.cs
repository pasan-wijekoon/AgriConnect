namespace AgriConnect.Api.Services;

public record DistanceResult(decimal DistanceKm, double? EtaMinutes, bool Degraded);

/// <summary>
/// FR21 — server-side-only distance estimation. Coordinates never reach the client
/// as anything but display data; the Maps API key never leaves the server
/// (CLAUDE.md §19/plan §9).
/// </summary>
public interface IDistanceService
{
    Task<DistanceResult> GetDistanceAsync(
        decimal originLat, decimal originLng, decimal destLat, decimal destLng,
        CancellationToken cancellationToken = default);
}
