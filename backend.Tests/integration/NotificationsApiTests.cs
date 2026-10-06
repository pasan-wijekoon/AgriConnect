using System.Net;
using System.Net.Http.Json;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Xunit;

namespace backend.Tests.integration;

/// <summary>Real-HTTP integration tests for NotificationsController (Phase 11/12),
/// filling the gap flagged in PROGRESS.md Known Issues: unit tests cover
/// NotificationService's own logic, but nothing previously pinned the controller's
/// routes/status codes/IDOR behavior automatically.</summary>
public class NotificationsApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private static readonly Guid ListingCarrots = Guid.Parse("4f2b1001-0000-0000-0000-000000000001");
    private static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");

    public NotificationsApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateAuthedClient();

        var response = await client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_AfterPlacingAnOrder_IncludesTheOrderPlacedNotification()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var placed = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 1m, DeliveryPreference.Pickup));
        placed.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>(ApiTestFactory.JsonOptions);
        Assert.Contains(notifications!, n => n.Type == "OrderPlaced");
    }

    [Fact]
    public async Task MarkRead_UnknownId_Returns404()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.PutAsync($"/api/notifications/{Guid.NewGuid()}/read", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_AnotherUsersNotification_Returns404_NotTheirsToMark()
    {
        using var ownerClient = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var placed = await ownerClient.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 1m, DeliveryPreference.Pickup));
        placed.EnsureSuccessStatusCode();
        var mine = await ownerClient.GetFromJsonAsync<List<NotificationResponse>>(
            "/api/notifications", ApiTestFactory.JsonOptions);
        var notificationId = mine!.First(n => n.Type == "OrderPlaced").Id;

        using var attackerClient = _factory.CreateAuthedClient(Roles.Buyer, Guid.NewGuid());
        var response = await attackerClient.PutAsync($"/api/notifications/{notificationId}/read", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
