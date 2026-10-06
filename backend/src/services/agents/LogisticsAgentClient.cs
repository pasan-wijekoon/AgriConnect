using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Dtos.Agents;

namespace AgriConnect.Api.Services.Agents;

/// <summary>The agent could not be reached, timed out, or failed internally.</summary>
public class AgentUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The agent found no free slot in the window or the days after it.</summary>
public class NoSlotAvailableException(string message) : Exception(message);

/// <summary>
/// Calls Student 4's Logistics Scheduling Agent (<c>agentic-ai/</c>,
/// <c>POST /agents/logistics/schedule</c>) for the AI Scheduling preview page.
///
/// Unlike <see cref="ILogisticsSchedulingPort"/>, which quietly falls back to the
/// stub so an order can always be scheduled, this client reports failures: the
/// preview exists to show what the agent itself decides.
/// </summary>
public class LogisticsAgentClient(HttpClient http, IConfiguration configuration, ILogger<LogisticsAgentClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private record WindowBody(DateTimeOffset Start, DateTimeOffset End);

    private record BookingBody(DateTimeOffset SlotStart, DateTimeOffset SlotEnd);

    private record RequestBody(string OrderId, string CentreId, WindowBody PreferredWindow, List<BookingBody> ExistingBookings);

    public async Task<SchedulingPreviewResponseDto> PreviewAsync(
        string centreId, DateTimeOffset start, DateTimeOffset end,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> bookings,
        CancellationToken cancellationToken)
    {
        // An LLM-written explanation can take several seconds, unlike the other agents.
        var timeoutSeconds = configuration.GetValue("AgenticAi:SchedulingPreviewTimeoutSeconds", 30);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // The agent requires an order id; a preview has no real order behind it.
        var body = new RequestBody(
            $"PREVIEW-{Guid.NewGuid():N}",
            centreId,
            new WindowBody(start, end),
            bookings.Select(b => new BookingBody(b.Start, b.End)).ToList());

        using var request = new HttpRequestMessage(HttpMethod.Post, "agents/logistics/schedule")
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        var apiKey = configuration["AgenticAi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("X-Internal-Api-Key", apiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Logistics Scheduling Agent unreachable for scheduling preview.");
            throw new AgentUnavailableException(
                "The AI scheduling service isn't reachable. Start agentic-ai (port 8000) and try again.", ex);
        }

        using (response)
        {
            var detail = await ReadDetailAsync(response, timeout.Token);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadFromJsonAsync<SchedulingPreviewResponseDto>(JsonOptions, timeout.Token);
                    if (result is null || result.ProposedSlotEnd <= result.ProposedSlotStart)
                    {
                        throw new AgentUnavailableException("The AI scheduling service returned an unusable answer.");
                    }
                    return result;
                case HttpStatusCode.Conflict:
                    throw new NoSlotAvailableException(detail ?? "No free slot was found.");
                case HttpStatusCode.UnprocessableEntity:
                    // Validation passed here, so the agent disagreeing is a contract problem, not the user's.
                    logger.LogWarning("Logistics Scheduling Agent rejected a preview request: {Detail}", detail);
                    throw new ArgumentException($"The AI scheduling service rejected the request: {detail}");
                default:
                    logger.LogWarning("Logistics Scheduling Agent returned {Status}: {Detail}", (int)response.StatusCode, detail);
                    throw new AgentUnavailableException(detail ?? $"The AI scheduling service failed ({(int)response.StatusCode}).");
            }
        }
    }

    /// <summary>FastAPI errors are {"detail": "..."} or, for validation, {"detail": [...]}.</summary>
    private static async Task<string?> ReadDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return null;
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("detail", out var detail)) return null;
            return detail.ValueKind == JsonValueKind.String
                ? detail.GetString()
                : string.Join("; ", detail.EnumerateArray()
                    .Select(e => e.TryGetProperty("msg", out var msg) ? msg.GetString() : e.ToString()));
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
