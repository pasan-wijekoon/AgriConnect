using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgriConnect.Api.Services;

/// <summary>
/// Server-side client for OSRM's public Table (matrix) API (plan §9). Only ever
/// receives raw (lat, lng) pairs — no buyer/farmer identity, no listing data
/// (DFD §8 data-minimisation rule). Retries on transient failure, then falls back
/// to a local haversine estimate with <c>Degraded = true</c> rather than surfacing
/// an error, per plan §9's "graceful degraded response" requirement.
///
/// Switched from OpenRouteService to OSRM's public demo server
/// (router.project-osrm.org) — free, unlimited, and needs no API key at all,
/// which OpenRouteService does require. <c>MapsApi:ApiKey</c> is left wired
/// (via Program.cs's conditional Authorization header) for a self-hosted OSRM
/// instance or a future provider that does need one; it's simply unused against
/// the public demo server. See PROGRESS.md's Decisions for the full reasoning
/// and the demo server's own caveats (no uptime/rate-limit guarantee).
/// </summary>
public class DistanceService(HttpClient http, IConfiguration configuration) : IDistanceService
{
    private record TableResponse(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("distances")] double[][]? Distances,
        [property: JsonPropertyName("durations")] double[][]? Durations);

    public async Task<DistanceResult> GetDistanceAsync(
        decimal originLat, decimal originLng, decimal destLat, decimal destLng,
        CancellationToken cancellationToken = default)
    {
        var maxRetries = configuration.GetValue("MapsApi:MaxRetries", 2);
        var timeoutSeconds = configuration.GetValue("MapsApi:TimeoutSeconds", 5);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                // OSRM's Table service: GET /table/v1/{profile}/{lng,lat;lng,lat;...}
                // — coordinates in the URL path (lng,lat order, same as ORS used),
                // sources/destinations as 0-based indexes into that coordinate list.
                var coords = string.Join(';',
                    FormatCoordinate(originLng, originLat), FormatCoordinate(destLng, destLat));
                var requestUri = $"table/v1/driving/{coords}?sources=0&destinations=1&annotations=distance,duration";

                using var response = await http.GetAsync(requestUri, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<TableResponse>(cancellationToken: timeoutCts.Token);
                if (payload?.Code != "Ok")
                {
                    throw new InvalidOperationException($"OSRM table request did not succeed (code: {payload?.Code}).");
                }

                var distanceMeters = payload.Distances?.ElementAtOrDefault(0)?.ElementAtOrDefault(0);
                if (distanceMeters is null)
                {
                    throw new InvalidOperationException("OSRM response did not contain a distance value.");
                }

                var durationSeconds = payload.Durations?.ElementAtOrDefault(0)?.ElementAtOrDefault(0);
                return new DistanceResult(
                    (decimal)(distanceMeters.Value / 1000.0),
                    durationSeconds.HasValue ? durationSeconds.Value / 60.0 : null,
                    Degraded: false);
            }
            catch (Exception ex) when (
                ex is HttpRequestException or InvalidOperationException
                || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                if (attempt == maxRetries)
                {
                    break; // exhausted retries — fall through to the haversine fallback below
                }

                await Task.Delay(TimeSpan.FromMilliseconds(150 * (attempt + 1)), cancellationToken);
            }
        }

        var haversineKm = HaversineCalculator.DistanceKm((double)originLat, (double)originLng, (double)destLat, (double)destLng);
        return new DistanceResult((decimal)haversineKm, EtaMinutes: null, Degraded: true);
    }

    private static string FormatCoordinate(decimal lng, decimal lat) =>
        $"{lng.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)}";
}
