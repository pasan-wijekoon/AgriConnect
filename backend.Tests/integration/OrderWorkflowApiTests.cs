using System.Net;
using System.Net.Http.Json;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// End-to-end order workflow over real HTTP: approval auto-proposes a schedule,
/// the Officer reviews it (approve / request revision), and Officers only see
/// orders for their own collection centre. Seeded demo officers are bound to
/// centres by SharedReferenceSeeder: officer@ (f0...050) = Kandy Central, officer2@ = Colombo Metro;
/// the Carrots demo listing is in the Kandy region.
/// </summary>
public class OrderWorkflowApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    private static readonly Guid ListingCarrots = Guid.Parse("4f2b1001-0000-0000-0000-000000000001");
    private static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");
    private static readonly Guid KandyOfficer = Guid.Parse("f0000000-0000-0000-0000-000000000050");
    private static readonly Guid ColomboOfficer = Guid.Parse("a2222222-0000-0000-0000-000000000002");
    private static readonly Guid KandyCentre = Guid.Parse("4f2b4001-0000-0000-0000-000000000001");

    public OrderWorkflowApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Approve_AutoProposesAScheduleAtTheRegionCentre_AndNeverConfirmsIt()
    {
        var order = await PlaceOrderAsync();

        var approved = await ApproveAsync(order.Id);

        Assert.Equal(OrderStatus.Approved, approved.Status);
        Assert.Equal(ScheduleStatus.Proposed, approved.ScheduleStatus);
        Assert.Equal(KandyCentre, approved.CollectionCentreId);

        using var officer = _factory.CreateAuthedClient(Roles.Officer, KandyOfficer);
        var schedule = await officer.GetFromJsonAsync<ScheduleResponse>(
            $"/api/orders/{order.Id}/schedule", ApiTestFactory.JsonOptions);
        Assert.Equal(ScheduleStatus.Proposed, schedule!.Status);
    }

    [Fact]
    public async Task Order_ResponseCarriesDisplayFields()
    {
        var order = await PlaceOrderAsync();

        Assert.Equal("Carrots", order.CropName);
        Assert.Equal("Kandy", order.RegionName);
        Assert.Equal("Kandy Central Collection Centre", order.CollectionCentreName);
    }

    [Fact]
    public async Task RequestRevision_MovesTheProposalToTheNewWindow_AndKeepsItProposed()
    {
        var order = await PlaceOrderAsync();
        await ApproveAsync(order.Id);

        var start = FarFutureSlot();
        using var officer = _factory.CreateAuthedClient(Roles.Officer, KandyOfficer);
        var response = await officer.PutAsJsonAsync($"/api/orders/{order.Id}/schedule/decision",
            new ScheduleDecisionRequest(ScheduleDecision.RequestRevision, "Centre closed that day",
                new ScheduleWindowDto(start, start.AddHours(1))));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var schedule = await response.Content.ReadFromJsonAsync<ScheduleResponse>(ApiTestFactory.JsonOptions);
        Assert.Equal(ScheduleStatus.Proposed, schedule!.Status);
        Assert.Equal(start, schedule.SlotStart);
    }

    [Fact]
    public async Task RequestRevision_WithWindowInANonUtcTimeZone_IsAcceptedAndStoredAsTheSameInstant()
    {
        var order = await PlaceOrderAsync();
        await ApproveAsync(order.Id);

        // Same instant expressed at +05:30 (Sri Lanka) - Npgsql rejects non-zero offsets
        // for timestamptz unless the service normalises them to UTC first.
        var startUtc = FarFutureSlot();
        var startLocal = startUtc.ToOffset(TimeSpan.FromHours(5.5));
        using var officer = _factory.CreateAuthedClient(Roles.Officer, KandyOfficer);
        var response = await officer.PutAsJsonAsync($"/api/orders/{order.Id}/schedule/decision",
            new ScheduleDecisionRequest(ScheduleDecision.RequestRevision, null,
                new ScheduleWindowDto(startLocal, startLocal.AddHours(1))));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var schedule = await response.Content.ReadFromJsonAsync<ScheduleResponse>(ApiTestFactory.JsonOptions);
        Assert.Equal(startUtc, schedule!.SlotStart);
    }

    [Fact]
    public async Task ApprovingTheProposal_ConfirmsTheScheduleAndMovesOrderToScheduled()
    {
        var order = await PlaceOrderAsync();
        await ApproveAsync(order.Id);

        // A unique far-future window so repeated runs never collide with (or fill
        // the capacity of) Confirmed bookings earlier runs left in the shared DB.
        var start = FarFutureSlot();
        using var officer = _factory.CreateAuthedClient(Roles.Officer, KandyOfficer);
        (await officer.PutAsJsonAsync($"/api/orders/{order.Id}/schedule/decision",
            new ScheduleDecisionRequest(ScheduleDecision.RequestRevision, null,
                new ScheduleWindowDto(start, start.AddHours(1))))).EnsureSuccessStatusCode();

        var decision = await officer.PutAsJsonAsync($"/api/orders/{order.Id}/schedule/decision",
            new ScheduleDecisionRequest(ScheduleDecision.Approve));

        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        var updated = await officer.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}", ApiTestFactory.JsonOptions);
        Assert.Equal(OrderStatus.Scheduled, updated!.Status);
        Assert.Equal(ScheduleStatus.Confirmed, updated.ScheduleStatus);
    }

    [Fact]
    public async Task OfficerOfAnotherCentre_CannotViewApproveOrScheduleTheOrder()
    {
        var order = await PlaceOrderAsync();
        using var other = _factory.CreateAuthedClient(Roles.Officer, ColomboOfficer);

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/orders/{order.Id}")).StatusCode);

        var approve = await other.PutAsJsonAsync($"/api/orders/{order.Id}/status",
            new OrderStatusUpdateRequest(OrderStatus.Approved));
        Assert.Equal(HttpStatusCode.NotFound, approve.StatusCode);

        var list = await other.GetFromJsonAsync<PagedResult<OrderResponse>>(
            "/api/orders?size=100", ApiTestFactory.JsonOptions);
        Assert.DoesNotContain(list!.Items, o => o.Id == order.Id);
    }

    [Fact]
    public async Task OfficerOfAnotherCentre_CannotReadThisCentresCalendar()
    {
        using var other = _factory.CreateAuthedClient(Roles.Officer, ColomboOfficer);

        var response = await other.GetAsync($"/api/collection-centres/{KandyCentre}/schedules");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OfficerCentreList_IsLimitedToTheirOwnCentre()
    {
        using var officer = _factory.CreateAuthedClient(Roles.Officer, KandyOfficer);

        var centres = await officer.GetFromJsonAsync<List<CollectionCentreResponse>>(
            "/api/collection-centres", ApiTestFactory.JsonOptions);

        Assert.Single(centres!);
        Assert.Equal(KandyCentre, centres![0].Id);
    }

    [Fact]
    public async Task Notifications_UnreadCountAndReadAll()
    {
        var buyer = Guid.NewGuid();
        using var client = _factory.CreateAuthedClient(Roles.Buyer, buyer);
        (await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 1m, DeliveryPreference.Pickup))).EnsureSuccessStatusCode();

        var before = await client.GetFromJsonAsync<UnreadCount>("/api/notifications/unread-count", ApiTestFactory.JsonOptions);
        Assert.True(before!.Count >= 1);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/notifications/read-all", null)).StatusCode);

        var after = await client.GetFromJsonAsync<UnreadCount>("/api/notifications/unread-count", ApiTestFactory.JsonOptions);
        Assert.Equal(0, after!.Count);
    }

    private record UnreadCount(int Count);

    // Unique per call so parallel/repeated runs never book the same slot.
    private static DateTimeOffset FarFutureSlot() =>
        new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero)
            .AddDays(200 + Random.Shared.Next(0, 5000)).AddMinutes(Random.Shared.Next(0, 24 * 60));

    private async Task<OrderResponse> PlaceOrderAsync()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 1m, DeliveryPreference.Pickup));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ApiTestFactory.JsonOptions))!;
    }

    private async Task<OrderResponse> ApproveAsync(Guid orderId)
    {
        using var officer = _factory.CreateAuthedClient(Roles.Officer, KandyOfficer);
        var response = await officer.PutAsJsonAsync($"/api/orders/{orderId}/status",
            new OrderStatusUpdateRequest(OrderStatus.Approved));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ApiTestFactory.JsonOptions))!;
    }
}
