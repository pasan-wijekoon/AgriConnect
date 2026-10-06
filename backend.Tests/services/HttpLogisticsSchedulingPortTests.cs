using System.Net;
using System.Text;
using System.Text.Json;
using AgriConnect.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.Tests.services;

/// <summary>
/// Tests the HTTP client for Student 4's Logistics Scheduling Agent against fake
/// handlers: the request it sends matches the agent's camelCase contract, a good
/// reply is used as-is, and every failure falls back to the stub's answer instead
/// of failing scheduling.
/// </summary>
public class HttpLogisticsSchedulingPortTests
{
    private static readonly Guid OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CentreId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly TimeSpan SriLanka = TimeSpan.FromHours(5.5);

    private static readonly SchedulingWindow Window = new(
        new DateTimeOffset(2026, 10, 1, 8, 0, 0, SriLanka),
        new DateTimeOffset(2026, 10, 1, 17, 0, 0, SriLanka));

    private static readonly List<ExistingBooking> Bookings =
    [
        new(new DateTimeOffset(2026, 10, 1, 8, 0, 0, SriLanka), new DateTimeOffset(2026, 10, 1, 9, 0, 0, SriLanka)),
    ];

    private static IConfiguration Config(string? apiKey = null, int timeoutSeconds = 5) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection([
                new("AgenticAi:TimeoutSeconds", timeoutSeconds.ToString()),
                new("AgenticAi:ApiKey", apiKey ?? ""),
            ])
            .Build();

    private static HttpLogisticsSchedulingPort NewPort(HttpMessageHandler handler, IConfiguration? config = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://agent.test/") },
            config ?? Config(),
            NullLogger<HttpLogisticsSchedulingPort>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private const string AgentReply = """
        {"proposedSlotStart":"2026-10-01T10:30:00+05:30","proposedSlotEnd":"2026-10-01T11:30:00+05:30",
         "conflictChecked":true,"reasoning":"First free hour after the 08:00 booking."}
        """;

    [Fact]
    public async Task ProposeSlotAsync_OnSuccess_ReturnsTheAgentsSlot()
    {
        var port = NewPort(new FakeHttpMessageHandler(_ => Json(HttpStatusCode.OK, AgentReply)));

        var proposal = await port.ProposeSlotAsync(OrderId, CentreId, Window, Bookings);

        Assert.Equal(new DateTimeOffset(2026, 10, 1, 10, 30, 0, SriLanka), proposal.ProposedSlotStart);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 11, 30, 0, SriLanka), proposal.ProposedSlotEnd);
        Assert.True(proposal.ConflictChecked);
    }

    [Fact]
    public async Task ProposeSlotAsync_ReturnsUtc_BecauseNpgsqlRejectsOtherOffsets()
    {
        var agentUp = NewPort(new FakeHttpMessageHandler(_ => Json(HttpStatusCode.OK, AgentReply)));
        var agentDown = NewPort(new ThrowingHttpMessageHandler());

        foreach (var proposal in new[]
        {
            await agentUp.ProposeSlotAsync(OrderId, CentreId, Window, Bookings),
            await agentDown.ProposeSlotAsync(OrderId, CentreId, Window, Bookings),
        })
        {
            Assert.Equal(TimeSpan.Zero, proposal.ProposedSlotStart.Offset);
            Assert.Equal(TimeSpan.Zero, proposal.ProposedSlotEnd.Offset);
        }
    }

    [Fact]
    public async Task ProposeSlotAsync_SendsTheAgentsCamelCaseContract()
    {
        HttpRequestMessage? sent = null;
        string? body = null;
        var port = NewPort(new FakeHttpMessageHandler(request =>
        {
            sent = request;
            body = request.Content!.ReadAsStringAsync().Result;
            return Json(HttpStatusCode.OK, AgentReply);
        }));

        await port.ProposeSlotAsync(OrderId, CentreId, Window, Bookings);

        Assert.Equal(HttpMethod.Post, sent!.Method);
        Assert.Equal("http://agent.test/agents/logistics/schedule", sent.RequestUri!.ToString());

        using var json = JsonDocument.Parse(body!);
        var root = json.RootElement;
        Assert.Equal(OrderId.ToString(), root.GetProperty("orderId").GetString());
        Assert.Equal(CentreId.ToString(), root.GetProperty("centreId").GetString());
        Assert.Equal(Window.Start, root.GetProperty("preferredWindow").GetProperty("start").GetDateTimeOffset());
        Assert.Equal(Window.End, root.GetProperty("preferredWindow").GetProperty("end").GetDateTimeOffset());
        var booking = Assert.Single(root.GetProperty("existingBookings").EnumerateArray().ToList());
        Assert.Equal(Bookings[0].Start, booking.GetProperty("slotStart").GetDateTimeOffset());
        Assert.Equal(Bookings[0].End, booking.GetProperty("slotEnd").GetDateTimeOffset());
        // The agent rejects unknown fields, so nothing beyond the contract may be sent.
        Assert.Equal(["orderId", "centreId", "preferredWindow", "existingBookings"], root.EnumerateObject().Select(p => p.Name));
    }

    [Theory]
    [InlineData("secret-key", true)]
    [InlineData("", false)]
    public async Task ProposeSlotAsync_SendsTheInternalApiKeyOnlyWhenConfigured(string apiKey, bool expectHeader)
    {
        HttpRequestMessage? sent = null;
        var port = NewPort(
            new FakeHttpMessageHandler(request => { sent = request; return Json(HttpStatusCode.OK, AgentReply); }),
            Config(apiKey));

        await port.ProposeSlotAsync(OrderId, CentreId, Window, Bookings);

        Assert.Equal(expectHeader, sent!.Headers.Contains("X-Internal-Api-Key"));
    }

    public static TheoryData<HttpMessageHandler> FailingAgents => new()
    {
        // The agent found no free slot within its search horizon.
        new FakeHttpMessageHandler(_ => Json(HttpStatusCode.Conflict, """{"detail":"No free slot found."}""")),
        new FakeHttpMessageHandler(_ => Json(HttpStatusCode.BadGateway, """{"detail":"The language model is unavailable."}""")),
        new FakeHttpMessageHandler(_ => Json(HttpStatusCode.OK, "not json")),
        // End before start: never trust a slot that makes no sense.
        new FakeHttpMessageHandler(_ => Json(HttpStatusCode.OK,
            """{"proposedSlotStart":"2026-10-01T11:00:00+05:30","proposedSlotEnd":"2026-10-01T10:00:00+05:30","conflictChecked":true}""")),
        new ThrowingHttpMessageHandler(),
    };

    [Theory]
    [MemberData(nameof(FailingAgents))]
    public async Task ProposeSlotAsync_WhenTheAgentFails_FallsBackToTheStubsAnswer(HttpMessageHandler handler)
    {
        var port = NewPort(handler);

        var proposal = await port.ProposeSlotAsync(OrderId, CentreId, Window, Bookings);
        var stub = await new StubLogisticsSchedulingPort().ProposeSlotAsync(OrderId, CentreId, Window, Bookings);

        Assert.Equal(stub, proposal);
    }

    [Fact]
    public async Task ProposeSlotAsync_WhenTheAgentTimesOut_FallsBack()
    {
        var port = NewPort(new DelayingHttpMessageHandler(TimeSpan.FromSeconds(10)), Config(timeoutSeconds: 1));

        var proposal = await port.ProposeSlotAsync(OrderId, CentreId, Window, Bookings);

        Assert.Equal(Window.Start, proposal.ProposedSlotStart);
    }

    [Fact]
    public async Task ProposeSlotAsync_WhenTheCallerCancels_DoesNotSwallowTheCancellation()
    {
        var port = NewPort(new DelayingHttpMessageHandler(TimeSpan.FromSeconds(10)));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => port.ProposeSlotAsync(OrderId, CentreId, Window, Bookings, cts.Token));
    }

    private class DelayingHttpMessageHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return Json(HttpStatusCode.OK, AgentReply);
        }
    }
}
