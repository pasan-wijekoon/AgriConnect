using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// The "available now" quantity clients show (FR9): the listing's total minus the stock
/// held by active orders. Measured relative to a reading taken first, because the demo
/// listing is shared and long-lived.
/// </summary>
public class ListingAvailabilityApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    private static readonly Guid ListingCarrots = Guid.Parse("4f2b1001-0000-0000-0000-000000000001");
    private static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");

    public ListingAvailabilityApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<(decimal Total, decimal Available)> ReadAsync()
    {
        using var buyer = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var response = await buyer.GetAsync($"/api/listings/{ListingCarrots}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("quantity").GetDecimal(), body.GetProperty("availableQuantity").GetDecimal());
    }

    [Fact]
    public async Task AvailableQuantity_DropsWhenAnOrderIsPlaced_AndIsRestoredOnCancel()
    {
        var before = await ReadAsync();
        Assert.True(before.Available <= before.Total);

        using var buyer = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var placed = await buyer.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(ListingCarrots, 3m, DeliveryPreference.Pickup));
        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
        var order = (await placed.Content.ReadFromJsonAsync<OrderResponse>(ApiTestFactory.JsonOptions))!;

        var held = await ReadAsync();
        Assert.Equal(before.Total, held.Total);
        Assert.Equal(before.Available - 3m, held.Available);

        var cancelled = await buyer.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", new OrderCancelRequest(null));
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);

        var after = await ReadAsync();
        Assert.Equal(before.Available, after.Available);
    }

    [Fact]
    public async Task Browse_ListsAvailableQuantityForEveryItem()
    {
        using var buyer = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);
        var response = await buyer.GetAsync("/api/listings?status=Published&pageSize=50");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        foreach (var item in items)
        {
            var total = item.GetProperty("quantity").GetDecimal();
            var available = item.GetProperty("availableQuantity").GetDecimal();
            Assert.InRange(available, 0m, total);
        }
    }
}
