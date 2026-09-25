using System.Net;
using System.Net.Http.Json;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using Xunit;

namespace backend.Tests.integration;

/// <summary>Real-HTTP integration tests for the FR21 nearest-centre endpoint
/// (plan §13's "API/integration" row). Uses <see cref="FakeDistanceService"/>
/// (see ApiTestFactory) so this never makes a real outbound call to
/// OpenRouteService.</summary>
public class CollectionCentresApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;
    private static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");
    private static readonly Guid OfficerId = Guid.NewGuid();

    public CollectionCentresApiTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Nearest_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateAuthedClient();

        var response = await client.GetAsync("/api/collection-centres/nearest?lat=7.29&lng=80.63");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Nearest_AsOfficer_Returns403()
    {
        // Officer/Admin are not in the Buyer/Farmer-only role list for this endpoint.
        using var client = _factory.CreateAuthedClient(Roles.Officer, OfficerId);

        var response = await client.GetAsync("/api/collection-centres/nearest?lat=7.29&lng=80.63");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Nearest_InvalidLatitude_Returns400()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.GetAsync("/api/collection-centres/nearest?lat=999&lng=80.63");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nearest_ValidRequest_ReturnsSortedNonDegradedResults()
    {
        using var client = _factory.CreateAuthedClient(Roles.Buyer, BuyerOne);

        var response = await client.GetAsync("/api/collection-centres/nearest?lat=7.29&lng=80.63");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<NearestCentreResponse>>(ApiTestFactory.JsonOptions);
        Assert.NotEmpty(results!);
        // FakeDistanceService always reports Degraded=false, unlike every other
        // phase's manual walkthrough (which had no live Maps key).
        Assert.All(results!, r => Assert.False(r.Degraded));
    }
}
