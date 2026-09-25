using System.Net.Http.Json;
using System.Text.Json;

namespace AgriConnect.Api.Services;

/// <summary>
/// HTTP client for the Buyer-Farmer Matching Agent (<c>agentic-ai/</c>,
/// <c>POST /agents/buyer-farmer-matching</c>). The agent's FastAPI/Pydantic
/// contract uses snake_case field names throughout (<c>order_id</c>, not
/// <c>orderId</c>) — unlike the rest of this backend's own JSON convention —
/// so this client uses its own <see cref="JsonSerializerOptions"/> with
/// <see cref="JsonNamingPolicy.SnakeCaseLower"/> rather than the MVC-wide
/// camelCase options `Program.cs` configures for the backend's own API.
///
/// A single attempt with a short timeout, not multi-attempt retry like
/// <see cref="DistanceService"/>: unlike the Maps integration (which has no
/// fallback data source of its own), <see cref="SchedulingService"/> already
/// has a complete, tested fallback (the pre-Phase-13 "first centre in region"
/// logic) for whenever this call doesn't succeed, so a failed attempt here
/// degrades immediately rather than blocking the scheduling operation on
/// retries for a service whose absence already has a safe answer.
/// </summary>
public class HttpBuyerFarmerMatchingPort(HttpClient http, IConfiguration configuration, ILogger<HttpBuyerFarmerMatchingPort> logger)
    : IBuyerFarmerMatchingPort
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private record BuyerLocationBody(double Lat, double Lng);

    private record CandidateCentreBody(
        string CentreId, string Name, double Lat, double Lng, int Capacity, int CurrentConfirmedBookings);

    private record MatchRequestBody(
        string OrderId,
        BuyerLocationBody BuyerLocation,
        string ListingId,
        decimal RequestedQuantity,
        List<CandidateCentreBody> CandidateCentres);

    private record MatchResponseBody(
        string OrderId, string? MatchedCentreId, double MatchConfidence, string Notes,
        int CandidatesConsidered, bool Degraded);

    public async Task<MatchResult?> MatchAsync(
        Guid orderId,
        decimal buyerLat,
        decimal buyerLng,
        Guid listingId,
        decimal requestedQuantity,
        IReadOnlyList<MatchCandidateCentre> candidateCentres,
        CancellationToken cancellationToken = default)
    {
        var timeoutSeconds = configuration.GetValue("AgenticAi:TimeoutSeconds", 5);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var requestBody = new MatchRequestBody(
                orderId.ToString(),
                new BuyerLocationBody((double)buyerLat, (double)buyerLng),
                listingId.ToString(),
                requestedQuantity,
                candidateCentres.Select(c => new CandidateCentreBody(
                    c.CentreId.ToString(), c.Name, (double)c.Lat, (double)c.Lng, c.Capacity, c.CurrentConfirmedBookings))
                    .ToList());

            using var request = new HttpRequestMessage(HttpMethod.Post, "agents/buyer-farmer-matching")
            {
                Content = JsonContent.Create(requestBody, options: JsonOptions)
            };
            var apiKey = configuration["AgenticAi:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("X-Internal-Api-Key", apiKey);
            }

            using var response = await http.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Buyer-Farmer Matching Agent returned {StatusCode}; falling back to region-based centre selection.",
                    response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<MatchResponseBody>(JsonOptions, timeoutCts.Token);
            if (payload is null)
            {
                return null;
            }

            Guid? matchedCentreId = Guid.TryParse(payload.MatchedCentreId, out var parsed) ? parsed : null;
            return new MatchResult(
                matchedCentreId, payload.MatchConfidence, payload.Notes, payload.CandidatesConsidered, payload.Degraded);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Service unreachable, timed out, or returned something unparseable —
            // never let an AI suggestion's unavailability fail the underlying
            // scheduling operation (same posture as DistanceService's fallback).
            logger.LogWarning(ex, "Buyer-Farmer Matching Agent call failed; falling back to region-based centre selection.");
            return null;
        }
    }
}
