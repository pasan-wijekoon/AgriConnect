using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Config;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Cross-component test (A -> D, FR16): when a farmer lists produce, the listing's asking
/// price is compared with the AI fair-price range and an Open flag appears on the
/// officer's Anomaly Queue if it is too far off. Regression test for DEF-D-01, where
/// nothing outside Component D ever called AnomalyDetectionService, so the queue could
/// never receive a real listing. Needs the AI agent reachable (see AgenticAi:BaseUrl).
/// Test ids TC-D-50.. match the Test Case Document.
/// </summary>
public class ListingAnomalyIntegrationTests : IClassFixture<ApiTestFactory>
{
    private static readonly Guid SeededFarmer = Guid.Parse("f0000000-0000-0000-0000-000000000001");
    private readonly ApiTestFactory _factory;

    public ListingAnomalyIntegrationTests(ApiTestFactory factory) => _factory = factory;

    private async Task<(Guid cropId, Guid regionId)> ReferenceIdsAsync()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer, SeededFarmer);
        var crops = await client.GetFromJsonAsync<JsonElement>("/api/crops");
        var regions = await client.GetFromJsonAsync<JsonElement>("/api/regions");
        var carrots = crops.EnumerateArray().First(c => c.GetProperty("name").GetString() == "Carrots");
        return (carrots.GetProperty("id").GetGuid(), regions.EnumerateArray().First().GetProperty("id").GetGuid());
    }

    private async Task<Guid> CreateListingAsync(decimal? minPrice)
    {
        var (cropId, regionId) = await ReferenceIdsAsync();
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer, SeededFarmer);
        var response = await farmer.PostAsJsonAsync("/api/listings", new
        {
            cropId,
            regionId,
            quantity = 100,
            unit = "kg",
            claimedGrade = "Grade A",
            pickupWindowStart = DateTime.UtcNow.AddDays(3),
            pickupWindowEnd = DateTime.UtcNow.AddDays(6),
            minPrice,
            description = "Anomaly integration test listing",
            photoUrls = new[] { "https://example.com/test.jpg" },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<JsonElement?> FlagForAsync(Guid listingId)
    {
        using var officer = _factory.CreateAuthedClient(Roles.Officer);
        var page = await officer.GetFromJsonAsync<JsonElement>("/api/analytics/anomalies?page=1&size=100");
        foreach (var flag in page.GetProperty("items").EnumerateArray())
        {
            if (flag.GetProperty("listingId").GetGuid() == listingId)
            {
                return flag;
            }
        }
        return null;
    }

    [Fact] // TC-D-50
    public async Task AnOverpricedListing_AppearsOnTheOfficersAnomalyQueue()
    {
        var listingId = await CreateListingAsync(minPrice: 50_000m);

        var flag = await FlagForAsync(listingId);

        Assert.NotNull(flag);
        Assert.Equal("Open", flag.Value.GetProperty("status").GetString());
        Assert.Equal(50_000m, flag.Value.GetProperty("listingPrice").GetDecimal());
        Assert.True(flag.Value.GetProperty("deviationPercent").GetDecimal() > 25m);
    }

    [Fact] // TC-D-51 — analytics must not break or flag a listing that has no asking price
    public async Task AListingWithoutAnAskingPrice_IsCreatedAndNotFlagged()
    {
        var listingId = await CreateListingAsync(minPrice: null);

        Assert.Null(await FlagForAsync(listingId));
    }

    [Fact] // TC-D-52 — the flag is visible to staff only
    public async Task TheAnomalyQueue_StaysClosedToFarmers()
    {
        await CreateListingAsync(minPrice: 50_000m);
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer, SeededFarmer);

        var response = await farmer.GetAsync("/api/analytics/anomalies");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
