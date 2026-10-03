using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Config;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Real-HTTP tests for Component D's AnalyticsController (FR15-FR17): authentication,
/// role-based access, request validation and the shape of a successful answer. Runs the
/// real Program.cs pipeline via <see cref="ApiTestFactory"/>; point it at the migrated
/// database with ConnectionStrings__Default (see testing-evidence/README.md). Test ids
/// (TC-D-3x) match the Test Case Document.
/// </summary>
public class AnalyticsApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AnalyticsApiTests(ApiTestFactory factory) => _factory = factory;

    private const string From = "2026-06-01";
    private const string To = "2026-12-31";

    private static async Task<JsonElement> JsonBody(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();

    /// <summary>A crop that has price history, discovered from the API rather than hard-coded.</summary>
    private async Task<Guid> CropWithPricesAsync()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);
        var filters = await JsonBody(await client.GetAsync("/api/analytics/filters"));
        return filters.GetProperty("crops").EnumerateArray()
            .First(c => c.GetProperty("hasPriceHistory").GetBoolean())
            .GetProperty("id").GetGuid();
    }

    // ------------------------------------------------------- Authentication and roles

    [Theory] // TC-D-30
    [InlineData("/api/analytics/filters")]
    [InlineData("/api/analytics/anomalies")]
    [InlineData("/api/analytics/shortages")]
    public async Task Endpoints_WithoutCredentials_Return401(string url)
    {
        using var client = _factory.CreateAuthedClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // TC-D-31
    public async Task Filters_AsABuyer_Returns403()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer);

        var response = await client.GetAsync("/api/analytics/filters");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory] // TC-D-32 — farmers may read trends, but the review queue and shortages are back-office only
    [InlineData("/api/analytics/anomalies")]
    [InlineData("/api/analytics/shortages")]
    public async Task BackOfficeEndpoints_AsAFarmer_Return403(string url)
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory] // TC-D-33
    [InlineData("/api/analytics/anomalies")]
    [InlineData("/api/analytics/shortages")]
    public async Task BackOfficeEndpoints_AsAnOfficer_Return200(string url)
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] // TC-D-34
    public async Task SnapshotRefresh_IsAdministratorOnly()
    {
        using var officer = _factory.CreateAuthedClient(Roles.Officer);
        using var admin = _factory.CreateAuthedClient(Roles.Admin);

        var asOfficer = await officer.PostAsync("/api/analytics/snapshots/refresh", null);
        var asAdmin = await admin.PostAsync("/api/analytics/snapshots/refresh", null);

        Assert.Equal(HttpStatusCode.Forbidden, asOfficer.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
    }

    [Fact] // TC-D-35
    public async Task ReportExport_AsAnOfficer_Returns403()
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.PostAsJsonAsync("/api/reports/export", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------- Price trends: success and validation

    [Fact] // TC-D-36
    public async Task Filters_FlagWhichCropsHavePriceHistory()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var body = await JsonBody(await client.GetAsync("/api/analytics/filters"));

        var crops = body.GetProperty("crops").EnumerateArray().ToList();
        Assert.Contains(crops, c => c.GetProperty("hasPriceHistory").GetBoolean());
        Assert.Contains(crops, c => !c.GetProperty("hasPriceHistory").GetBoolean());
    }

    [Fact] // TC-D-37
    public async Task PriceTrends_ForACropWithHistory_ReturnsOrderedPointsWithRealPrices()
    {
        var cropId = await CropWithPricesAsync();
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.GetAsync($"/api/analytics/price-trends?cropId={cropId}&from={From}&to={To}&bucket=week");
        var body = await JsonBody(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("week", body.GetProperty("bucket").GetString());
        var points = body.GetProperty("points").EnumerateArray().ToList();
        Assert.NotEmpty(points);
        Assert.All(points, p => Assert.True(p.GetProperty("avgPrice").GetDecimal() > 0));
        var periods = points.Select(p => p.GetProperty("period").GetString()!).ToList();
        Assert.Equal(periods.OrderBy(x => x).ToList(), periods);
    }

    [Fact] // TC-D-38
    public async Task PriceTrends_ForACropWithNoData_ReturnsAnEmptyList_Not404()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.GetAsync($"/api/analytics/price-trends?cropId={Guid.NewGuid()}&from={From}&to={To}");
        var body = await JsonBody(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(body.GetProperty("points").EnumerateArray());
    }

    [Theory] // TC-D-39
    [InlineData("from=2026-06-01&to=2026-12-31")]                                                // cropId missing
    [InlineData("cropId=00000000-0000-0000-0000-000000000001&to=2026-12-31")]                    // from missing
    [InlineData("cropId=00000000-0000-0000-0000-000000000001&from=2026-12-31&to=2026-06-01")]    // to before from
    [InlineData("cropId=00000000-0000-0000-0000-000000000001&from=2026-06-01&to=2026-12-31&bucket=year")]
    [InlineData("cropId=not-a-guid&from=2026-06-01&to=2026-12-31")]
    [InlineData("cropId=00000000-0000-0000-0000-000000000001&from=31-31-2026&to=2026-12-31")]
    public async Task PriceTrends_WithInvalidInput_Returns400(string query)
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.GetAsync($"/api/analytics/price-trends?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ------------------------------------------------------- Anomaly queue and shortages: validation

    [Theory] // TC-D-40
    [InlineData("page=0")]
    [InlineData("size=0")]
    [InlineData("size=101")]
    [InlineData("status=Banana")]
    public async Task Anomalies_WithInvalidPagingOrStatus_Returns400(string query)
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.GetAsync($"/api/analytics/anomalies?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // TC-D-41
    public async Task Anomalies_AtTheMaximumPageSize_Returns200()
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.GetAsync("/api/analytics/anomalies?page=1&size=100");
        var body = await JsonBody(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100, body.GetProperty("size").GetInt32());
    }

    [Fact] // TC-D-42
    public async Task UpdateAnomalyStatus_ForAnUnknownFlag_Returns404()
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/analytics/anomalies/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new { status = "Reviewed" }),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory] // TC-D-43 — flags can only move to Reviewed or Dismissed
    [InlineData("Open")]
    [InlineData("1")]
    public async Task UpdateAnomalyStatus_ToAnInvalidStatus_Returns400(string status)
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/analytics/anomalies/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new { status }),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory] // TC-D-44
    [InlineData("type=flood")]
    [InlineData("severity=extreme")]
    public async Task Shortages_WithAnUnknownFilter_Returns400(string query)
    {
        using var client = _factory.CreateAuthedClient(Roles.Officer);

        var response = await client.GetAsync($"/api/analytics/shortages?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // TC-D-45
    public async Task Errors_AreReturnedAsRfc7807ProblemDetails()
    {
        using var client = _factory.CreateAuthedClient(Roles.Farmer);

        var response = await client.GetAsync("/api/analytics/price-trends?bucket=year");
        var body = await JsonBody(response);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("errors", out _));
    }
}
