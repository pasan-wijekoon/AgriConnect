using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgriConnect.Api.Services;

/// <summary>
/// Server-side client for the OpenRouteService Matrix API (plan §9). Only ever
/// receives raw (lat, lng) pairs — no buyer/farmer identity, no listing data
/// (DFD §8 data-minimisation rule). Retries on transient failure, then falls back
/// to a local haversine estimate with <c>Degraded = true</c> rather than surfacing
/// an error, per plan §9's "graceful degraded response" requirement.
/// </summary>
public class DistanceService(HttpClient http, IConfiguration configuration) : IDistanceService
{
    private record MatrixResponse(
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
                var requestBody = new
                {
                    locations = new[]
                    {
                        new[] { (double)originLng, (double)originLat },
                        new[] { (double)destLng, (double)destLat }
                    },
                    sources = new[] { 0 },
                    destinations = new[] { 1 },
                    metrics = new[] { "distance", "duration" },
                    units = "km"
                };

                using var response = await http.PostAsJsonAsync("v2/matrix/driving-car", requestBody, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<MatrixResponse>(cancellationToken: timeoutCts.Token);
                var distanceKm = payload?.Distances?.ElementAtOrDefault(0)?.ElementAtOrDefault(0);
                if (distanceKm is null)
                {
                    throw new InvalidOperationException("Maps API response did not contain a distance value.");
                }

                var durationSeconds = payload?.Durations?.ElementAtOrDefault(0)?.ElementAtOrDefault(0);
                return new DistanceResult(
                    (decimal)distanceKm.Value,
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
}
