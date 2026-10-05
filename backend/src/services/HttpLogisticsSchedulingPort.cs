using System.Net.Http.Json;
using System.Text.Json;

namespace AgriConnect.Api.Services;

/// <summary>
/// HTTP client for the Logistics Scheduling Agent (Student 4 / Component D,
/// <c>agentic-ai/</c>, <c>POST /agents/logistics/schedule</c>). Replaces
/// <see cref="StubLogisticsSchedulingPort"/> behind the same
/// <see cref="ILogisticsSchedulingPort"/> contract.
///
/// Unlike the Buyer-Farmer Matching Agent, this agent's contract is camelCase
/// (<c>orderId</c>, <c>preferredWindow</c>), matching the DFD §4.4 input/output
/// contract, so it uses the web (camelCase) JSON defaults.
///
/// Same posture as <see cref="HttpBuyerFarmerMatchingPort"/>: one attempt with a
/// short timeout, and any failure — unreachable, timed out, no free slot (409),
/// or an unusable reply — falls back to the stub's behaviour, so the agent's
/// absence never fails scheduling. <see cref="SchedulingService"/> still runs its
/// own capacity check on whatever comes back, and nothing is confirmed without an
/// Officer.
/// </summary>
public class HttpLogisticsSchedulingPort(HttpClient http, IConfiguration configuration, ILogger<HttpLogisticsSchedulingPort> logger)
    : ILogisticsSchedulingPort
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly StubLogisticsSchedulingPort _fallback = new();

    private record WindowBody(DateTimeOffset Start, DateTimeOffset End);

    private record BookingBody(DateTimeOffset SlotStart, DateTimeOffset SlotEnd);

    private record ScheduleRequestBody(string OrderId, string CentreId, WindowBody PreferredWindow, List<BookingBody> ExistingBookings);

    private record ScheduleResponseBody(DateTimeOffset ProposedSlotStart, DateTimeOffset ProposedSlotEnd, bool ConflictChecked, string? Reasoning);

    public async Task<SchedulingProposal> ProposeSlotAsync(
        Guid orderId,
        Guid centreId,
        SchedulingWindow preferredWindow,
        IReadOnlyList<ExistingBooking> existingBookings,
        CancellationToken cancellationToken = default)
    {
        var timeoutSeconds = configuration.GetValue("AgenticAi:TimeoutSeconds", 5);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var requestBody = new ScheduleRequestBody(
                orderId.ToString(),
                centreId.ToString(),
                new WindowBody(preferredWindow.Start, preferredWindow.End),
                existingBookings.Select(b => new BookingBody(b.Start, b.End)).ToList());

            using var request = new HttpRequestMessage(HttpMethod.Post, "agents/logistics/schedule")
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
                // 409 = the agent found no free slot within its search horizon;
                // anything else is a failure. Either way the stub's answer lets
                // SchedulingService decide against the requested window.
                logger.LogWarning(
                    "Logistics Scheduling Agent returned {StatusCode} for order {OrderId}; falling back to the requested window.",
                    (int)response.StatusCode, orderId);
                return await FallbackAsync(orderId, centreId, preferredWindow, existingBookings, cancellationToken);
            }

            var payload = await response.Content.ReadFromJsonAsync<ScheduleResponseBody>(JsonOptions, timeoutCts.Token);
            if (payload is null || payload.ProposedSlotEnd <= payload.ProposedSlotStart)
            {
                logger.LogWarning(
                    "Logistics Scheduling Agent returned an unusable slot for order {OrderId}; falling back to the requested window.",
                    orderId);
                return await FallbackAsync(orderId, centreId, preferredWindow, existingBookings, cancellationToken);
            }

            logger.LogInformation(
                "Logistics Scheduling Agent proposed {Start}–{End} for order {OrderId}: {Reasoning}",
                payload.ProposedSlotStart, payload.ProposedSlotEnd, orderId, payload.Reasoning);
            return ToUtc(new SchedulingProposal(payload.ProposedSlotStart, payload.ProposedSlotEnd, payload.ConflictChecked));
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException or NotSupportedException
            || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Logistics Scheduling Agent call failed for order {OrderId}; falling back to the requested window.", orderId);
            return await FallbackAsync(orderId, centreId, preferredWindow, existingBookings, cancellationToken);
        }
    }

    private async Task<SchedulingProposal> FallbackAsync(
        Guid orderId, Guid centreId, SchedulingWindow preferredWindow,
        IReadOnlyList<ExistingBooking> existingBookings, CancellationToken cancellationToken) =>
        ToUtc(await _fallback.ProposeSlotAsync(orderId, centreId, preferredWindow, existingBookings, cancellationToken));

    // The agent answers in the caller's offset (e.g. +05:30), but Npgsql only writes
    // UTC DateTimeOffsets to timestamptz; anything else fails SaveChanges with a 500.
    private static SchedulingProposal ToUtc(SchedulingProposal p) =>
        p with { ProposedSlotStart = p.ProposedSlotStart.ToUniversalTime(), ProposedSlotEnd = p.ProposedSlotEnd.ToUniversalTime() };
}
