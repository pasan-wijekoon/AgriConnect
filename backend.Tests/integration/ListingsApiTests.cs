using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Config;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Component A listing endpoints (FR3/FR6) through the real pipeline. Regression tests for
/// the 500s and the missing validation found in the 2026-10-03 full-system check.
/// </summary>
public class ListingsApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ListingsApiTests(ApiTestFactory factory) => _factory = factory;

    private static async Task<JsonElement> JsonBody(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();

    private async Task<Guid> FirstIdAsync(HttpClient client, string url)
    {
        var json = await JsonBody(await client.GetAsync(url));
        return json.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private async Task<object> ListingBodyAsync(HttpClient client, string start, string end) => new
    {
        cropId = await FirstIdAsync(client, "/api/crops"),
        regionId = await FirstIdAsync(client, "/api/regions"),
        quantity = 10,
        unit = "kg",
        claimedGrade = "Grade A",
        pickupWindowStart = start,
        pickupWindowEnd = end,
        minPrice = 100,
        photoUrls = new[] { "https://example.com/p.jpg" },
    };

    private static string Utc(int daysFromNow) => DateTime.UtcNow.AddDays(daysFromNow).ToString("yyyy-MM-ddTHH:mm:ssZ");

    [Fact]
    public async Task Create_WithFutureWindow_ReturnsPendingApproval()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.PostAsJsonAsync("/api/listings", await ListingBodyAsync(client, Utc(2), Utc(3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("PendingApproval", (await JsonBody(response)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_WithPickupWindowAlreadyOver_Returns400()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.PostAsJsonAsync("/api/listings", await ListingBodyAsync(client, Utc(-5), Utc(-4)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithOffsetDates_IsAcceptedNotA500()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);
        // "+00:00" binds as DateTimeKind.Local, which Npgsql refuses to write to timestamptz.
        var start = DateTimeOffset.UtcNow.AddDays(2).ToString("o");
        var end = DateTimeOffset.UtcNow.AddDays(3).ToString("o");

        var response = await client.PostAsJsonAsync("/api/listings", await ListingBodyAsync(client, start, end));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsBuyer_Returns403()
    {
        using var buyer = _factory.CreateAuthedClient(Roles.Buyer);
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await buyer.PostAsJsonAsync("/api/listings", await ListingBodyAsync(farmer, Utc(2), Utc(3)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("page=-1")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=-5")]
    public async Task Browse_WithInvalidPaging_DoesNotFail(string query)
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer);

        var response = await client.GetAsync("/api/listings?" + query);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Browse_PageSizeIsCapped()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer);

        var json = await JsonBody(await client.GetAsync("/api/listings?pageSize=100000"));

        Assert.True(json.GetProperty("pageSize").GetInt32() <= 100);
    }

    [Fact]
    public async Task Browse_AsBuyer_NeverShowsUnpublishedListings()
    {
        using var buyer = _factory.CreateAuthedClient(Roles.Buyer);

        var json = await JsonBody(await buyer.GetAsync("/api/listings?status=PendingApproval&pageSize=100"));

        Assert.All(json.GetProperty("items").EnumerateArray(),
            l => Assert.Equal("Published", l.GetProperty("status").GetString()));
    }
}
