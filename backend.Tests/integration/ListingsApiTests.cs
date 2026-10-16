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

    // ── Component C: GET /api/listings/{id}/inspections authorization ──

    /// <summary>
    /// Creates a listing as the given farmer and returns its ID.
    /// </summary>
    private async Task<Guid> CreateListingAsFarmer(HttpClient farmerClient)
    {
        var body = await ListingBodyAsync(farmerClient, Utc(2), Utc(3));
        var response = await farmerClient.PostAsJsonAsync("/api/listings", body);
        response.EnsureSuccessStatusCode();
        var json = await JsonBody(response);
        return json.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Records an inspection for a listing (requires Officer/Admin).
    /// </summary>
    private async Task RecordInspectionAsync(HttpClient officerClient, Guid listingId, string grade = "Grade A")
    {
        var body = new
        {
            listingId,
            confirmedGrade = grade,
            notes = "Test inspection",
            photoUrls = Array.Empty<string>()
        };
        var response = await officerClient.PostAsJsonAsync("/api/inspections", body);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetInspections_OwnerFarmer_Returns200WithInspections()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);
        using var officer = _factory.CreateAuthedClient(Roles.Officer);

        var listingId = await CreateListingAsFarmer(farmer);
        await RecordInspectionAsync(officer, listingId, "Grade A");

        var response = await farmer.GetAsync($"/api/listings/{listingId}/inspections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await JsonBody(response);
        var items = json.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("Grade A", items[0].GetProperty("confirmedGrade").GetString());
    }

    [Fact]
    public async Task GetInspections_DifferentFarmer_Returns403()
    {
        using var farmer1 = _factory.CreateAuthedClient(Roles.Farmer, ApiTestFactory.StableUserId("Farmer1"));
        using var farmer2 = _factory.CreateAuthedClient(Roles.Farmer, ApiTestFactory.StableUserId("Farmer2"));
        using var officer = _factory.CreateAuthedClient(Roles.Officer);

        var listingId = await CreateListingAsFarmer(farmer1);
        await RecordInspectionAsync(officer, listingId);

        var response = await farmer2.GetAsync($"/api/listings/{listingId}/inspections");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInspections_Officer_Returns200()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);
        using var officer = _factory.CreateAuthedClient(Roles.Officer);

        var listingId = await CreateListingAsFarmer(farmer);
        await RecordInspectionAsync(officer, listingId);

        var response = await officer.GetAsync($"/api/listings/{listingId}/inspections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await JsonBody(response);
        Assert.NotEmpty(json.EnumerateArray());
    }

    [Fact]
    public async Task GetInspections_Administrator_Returns200()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        using var officer = _factory.CreateAuthedClient(Roles.Officer);

        var listingId = await CreateListingAsFarmer(farmer);
        await RecordInspectionAsync(officer, listingId);

        var response = await admin.GetAsync($"/api/listings/{listingId}/inspections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await JsonBody(response);
        Assert.NotEmpty(json.EnumerateArray());
    }

    [Fact]
    public async Task GetInspections_UnknownListingId_Returns404()
    {
        using var farmer = _factory.CreateAuthedClient(Roles.Farmer);

        var unknownId = Guid.NewGuid();
        var response = await farmer.GetAsync($"/api/listings/{unknownId}/inspections");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInspections_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient(); // No auth header

        var response = await client.GetAsync("/api/listings/00000000-0000-0000-0000-000000000001/inspections");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
