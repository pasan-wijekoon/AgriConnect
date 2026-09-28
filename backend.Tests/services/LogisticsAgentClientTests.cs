using System.Net;
using System.Text;
using System.Text.Json;
using AgriConnect.Api.Services.Agents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.Tests.services;

/// <summary>
/// The AI Scheduling preview client: sends the agent's camelCase contract, returns its
/// answer, and turns each agent failure into a specific error the page can explain
/// (unlike the order-scheduling port, it must not silently fall back).
/// </summary>
public class LogisticsAgentClientTests
{
    private static readonly TimeSpan SriLanka = TimeSpan.FromHours(5.5);
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 8, 0, 0, SriLanka);
    private static readonly DateTimeOffset End = new(2026, 10, 5, 17, 0, 0, SriLanka);
    private static readonly List<(DateTimeOffset, DateTimeOffset)> Bookings =
        [(new DateTimeOffset(2026, 10, 5, 8, 0, 0, SriLanka), new DateTimeOffset(2026, 10, 5, 9, 0, 0, SriLanka))];

    private static LogisticsAgentClient NewClient(HttpMessageHandler handler, int timeoutSeconds = 30) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://agent.test/") },
            new ConfigurationBuilder()
                .AddInMemoryCollection([new("AgenticAi:SchedulingPreviewTimeoutSeconds", timeoutSeconds.ToString())])
                .Build(),
            NullLogger<LogisticsAgentClient>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private const string Reply = """
        {"proposedSlotStart":"2026-10-05T09:00:00+05:30","proposedSlotEnd":"2026-10-05T10:00:00+05:30",
         "conflictChecked":true,"reasoning":"First free hour after the 08:00 booking."}
        """;

    private static Task<AgriConnect.Api.Dtos.Agents.SchedulingPreviewResponseDto> Preview(LogisticsAgentClient client) =>
        client.PreviewAsync("CC-DAMBULLA", Start, End, Bookings, CancellationToken.None);

    [Fact]
    public async Task PreviewAsync_ReturnsTheAgentsSlotAndReasoning()
    {
        var result = await Preview(NewClient(new FakeHttpMessageHandler(_ => Json(HttpStatusCode.OK, Reply))));

        Assert.Equal(new DateTimeOffset(2026, 10, 5, 9, 0, 0, SriLanka), result.ProposedSlotStart);
        Assert.True(result.ConflictChecked);
        Assert.Equal("First free hour after the 08:00 booking.", result.Reasoning);
    }

    [Fact]
    public async Task PreviewAsync_SendsOnlyTheAgentsContractFields()
    {
        string? body = null;
        HttpRequestMessage? sent = null;
        await Preview(NewClient(new FakeHttpMessageHandler(request =>
        {
            sent = request;
            body = request.Content!.ReadAsStringAsync().Result;
            return Json(HttpStatusCode.OK, Reply);
        })));

        Assert.Equal("http://agent.test/agents/logistics/schedule", sent!.RequestUri!.ToString());
        using var json = JsonDocument.Parse(body!);
        Assert.Equal(["orderId", "centreId", "preferredWindow", "existingBookings"], json.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.StartsWith("PREVIEW-", json.RootElement.GetProperty("orderId").GetString());
        Assert.Equal("CC-DAMBULLA", json.RootElement.GetProperty("centreId").GetString());
        Assert.Single(json.RootElement.GetProperty("existingBookings").EnumerateArray());
    }

    [Fact]
    public async Task PreviewAsync_WhenNoSlot_ThrowsNoSlotAvailableWithTheAgentsExplanation()
    {
        var client = NewClient(new FakeHttpMessageHandler(_ =>
            Json(HttpStatusCode.Conflict, """{"detail":"No free slot found. 2026-10-05: centre at capacity."}""")));

        var ex = await Assert.ThrowsAsync<NoSlotAvailableException>(() => Preview(client));

        Assert.Contains("centre at capacity", ex.Message);
    }

    [Fact]
    public async Task PreviewAsync_WhenTheAgentRejectsTheRequest_ThrowsArgumentExceptionWithItsReason()
    {
        var client = NewClient(new FakeHttpMessageHandler(_ =>
            Json(HttpStatusCode.UnprocessableEntity, """{"detail":[{"msg":"Input should have timezone info"}]}""")));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Preview(client));

        Assert.Contains("timezone", ex.Message);
    }

    public static TheoryData<HttpMessageHandler> Unavailable => new()
    {
        new ThrowingHttpMessageHandler(),
        new FakeHttpMessageHandler(_ => Json(HttpStatusCode.BadGateway, """{"detail":"The language model is unavailable."}""")),
        new FakeHttpMessageHandler(_ => Json(HttpStatusCode.OK,
            """{"proposedSlotStart":"2026-10-05T10:00:00+05:30","proposedSlotEnd":"2026-10-05T09:00:00+05:30","conflictChecked":true,"reasoning":"x"}""")),
    };

    [Theory]
    [MemberData(nameof(Unavailable))]
    public async Task PreviewAsync_WhenTheAgentFails_ThrowsAgentUnavailable(HttpMessageHandler handler)
    {
        await Assert.ThrowsAsync<AgentUnavailableException>(() => Preview(NewClient(handler)));
    }

    [Fact]
    public async Task PreviewAsync_WhenTheAgentIsTooSlow_ThrowsAgentUnavailable()
    {
        var client = NewClient(new SlowHandler(TimeSpan.FromSeconds(10)), timeoutSeconds: 1);

        await Assert.ThrowsAsync<AgentUnavailableException>(() => Preview(client));
    }

    private class SlowHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return Json(HttpStatusCode.OK, Reply);
        }
    }
}
