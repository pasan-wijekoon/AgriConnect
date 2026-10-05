using System.Net;
using System.Text.Json;
using AgriConnect.Api.Config;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Component A "Today's Prices" discovery (GET /api/prices/today) and its admin-only catalog
/// management. Live prices come from the Fair-Price agent; when it is unreachable the backend's
/// internal estimate answers, so these assertions hold with or without the agent running.
/// </summary>
public class TodayPricesApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public TodayPricesApiTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Today_WithoutCredentials_Returns401()
    {
        using var client = _factory.CreateAuthedClient();

        var response = await client.GetAsync("/api/prices/today");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Farmer)]
    [InlineData(Roles.Buyer)]
    [InlineData(Roles.Officer)]
    [InlineData(Roles.Admin)]
    public async Task Today_AsAnyRole_ReturnsThePricedCatalog(string role)
    {
        using var client = _factory.CreateAuthedClient(role);

        var response = await client.GetAsync("/api/prices/today?grade=A");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement;
        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i =>
        {
            Assert.True(i.GetProperty("suggestedPriceMin").GetDecimal() > 0);
            Assert.True(i.GetProperty("suggestedPriceMax").GetDecimal() >= i.GetProperty("suggestedPriceMin").GetDecimal());
        });
    }

    [Fact]
    public async Task Today_WithRegion_PricesEveryItemForThatRegion()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer);

        var response = await client.GetAsync("/api/prices/today?region=Kandy&grade=B");

        var json = (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement;
        Assert.All(json.GetProperty("items").EnumerateArray(),
            i => Assert.Equal("Kandy", i.GetProperty("region").GetString()));
    }

    [Theory]
    [InlineData(Roles.Farmer)]
    [InlineData(Roles.Buyer)]
    [InlineData(Roles.Officer)]
    public async Task Catalog_ManagementIsAdministratorOnly(string role)
    {
        using var client = _factory.CreateAuthedClient(role);

        var response = await client.GetAsync("/api/admin/today-prices-catalog");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_AsAdministrator_ListsTheItems()
    {
        using var client = _factory.CreateAuthedClient(Roles.Admin);

        var response = await client.GetAsync("/api/admin/today-prices-catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
