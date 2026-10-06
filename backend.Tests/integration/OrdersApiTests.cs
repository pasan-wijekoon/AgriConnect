using System.Net;
using System.Net.Http.Json;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Real-HTTP integration tests over the full ASP.NET Core pipeline (plan §13's
/// "API/integration" row: success/validation/401/403/404/409 per endpoint,
/// role-rejection, IDOR). Complements the service-level unit tests in
/// OrderServiceTests, which exercise the same business rules directly without
/// going through routing/model-binding/authorization/RFC-7807 formatting.
/// </summary>
public class OrdersApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    // OrderLogisticsFixtures' known, seeded demo orders/listings/buyers — the
    // same fixed GUIDs every prior phase's manual curl testing has relied on.
    private static readonly Guid ListingCarrots = Guid.Parse("4f2b1001-0000-0000-0000-000000000001");
    private static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");
    private static readonly Guid BuyerTwo = Guid.Parse("4f2b3002-0000-0000-0000-000000000002");
    private static readonly Guid PendingOrderId = Guid.Parse("4f2b5001-0000-0000-0000-000000000001");
    private static readonly Guid OfficerId = Guid.NewGuid();

    public OrdersApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    // ---- POST /api/orders ----

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateAuthedClient();

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 5m, DeliveryPreference.Pickup));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsFarmer_Returns403()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer, Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 5m, DeliveryPreference.Pickup));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidQuantity_Returns400()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 0m, DeliveryPreference.Pickup));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownListing_Returns404()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(Guid.NewGuid(), 5m, DeliveryPreference.Pickup));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithReservation()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 5m, DeliveryPreference.Pickup));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(ApiTestFactory.JsonOptions);
        Assert.Equal(OrderStatus.Pending, body!.Status);
        Assert.Equal(BuyerOne, body.BuyerId);
        Assert.NotNull(body.ReservationExpiresAt);
    }

    // ---- GET /api/orders/{id} — ownership / IDOR ----

    [Fact]
    public async Task GetById_OwningBuyer_Returns200()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.GetAsync($"/api/orders/{PendingOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_DifferentBuyer_Returns404NotForbidden()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerTwo);

        var response = await client.GetAsync($"/api/orders/{PendingOrderId}");

        // IDOR prevention (plan §6): existence of another buyer's order must
        // never be revealed via a 403 — only ever 404.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Officer_CanViewAnyOrder()
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer, OfficerId);

        var response = await client.GetAsync($"/api/orders/{PendingOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- PUT /api/orders/{id}/status ----

    [Fact]
    public async Task UpdateStatus_AsBuyer_Returns403()
    {
        var order = await CreateOrderAsync();
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status", new OrderStatusUpdateRequest(OrderStatus.Approved));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_IllegalTransition_Returns400()
    {
        var order = await CreateOrderAsync();
        using var client = _factory.CreateAuthedClient(Roles.Officer, OfficerId);

        // Pending -> Completed is not in the allow-list (must go through
        // Approved -> Scheduled -> Completed).
        var response = await client.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status", new OrderStatusUpdateRequest(OrderStatus.Completed));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_LegalTransition_Returns200()
    {
        var order = await CreateOrderAsync();
        using var client = _factory.CreateAuthedClient(Roles.Officer, OfficerId);

        var response = await client.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status", new OrderStatusUpdateRequest(OrderStatus.Approved));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(ApiTestFactory.JsonOptions);
        Assert.Equal(OrderStatus.Approved, body!.Status);
    }

    // ---- POST /api/orders/{id}/cancel ----

    [Fact]
    public async Task Cancel_DifferentBuyer_Returns404()
    {
        var order = await CreateOrderAsync();
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerTwo);

        var response = await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/cancel", new OrderCancelRequest(null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ThenCancelAgain_SecondCallReturns409()
    {
        var order = await CreateOrderAsync();
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var first = await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/cancel", new OrderCancelRequest("Changed my mind"));
        var second = await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/cancel", new OrderCancelRequest(null));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private async Task<OrderResponse> CreateOrderAsync()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var response = await client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 1m, DeliveryPreference.Pickup));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(ApiTestFactory.JsonOptions))!;
    }
}
