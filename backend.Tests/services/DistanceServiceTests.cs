using System.Net;
using System.Text;
using AgriConnect.Api.Services;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.services;

internal class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(respond(request));
    }
}

internal class ThrowingHttpMessageHandler : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        throw new HttpRequestException("simulated network failure");
    }
}

/// <summary>
/// Tests DistanceService against a fake HttpMessageHandler rather than the real
/// OpenRouteService API, since no live MAPS_API_KEY is provisioned in this dev
/// environment (docker/.env.example ships it empty). This still exercises the
/// real code path: request construction, response parsing, retry count, and the
/// haversine fallback with the Degraded flag — the part plan §9 actually requires
/// to be correct, independent of whether a live key is ever configured.
/// </summary>
public class DistanceServiceTests
{
    private static IConfiguration NoRetryDelayConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection([
                new("MapsApi:MaxRetries", "1"),
                new("MapsApi:TimeoutSeconds", "5")
            ])
            .Build();

    private static HttpClient NewClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://fake.example/") };

    [Fact]
    public async Task GetDistanceAsync_OnSuccessfulResponse_ReturnsParsedDistanceNotDegraded()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"distances":[[42.5]],"durations":[[1800.0]]}""", Encoding.UTF8, "application/json")
        });
        var service = new DistanceService(NewClient(handler), NoRetryDelayConfig());

        var result = await service.GetDistanceAsync(7.29m, 80.63m, 6.93m, 79.86m);

        Assert.False(result.Degraded);
        Assert.Equal(42.5m, result.DistanceKm);
        Assert.Equal(30.0, result.EtaMinutes); // 1800s / 60
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetDistanceAsync_WhenApiAlwaysFails_FallsBackToHaversineWithDegradedFlag()
    {
        var handler = new ThrowingHttpMessageHandler();
        var service = new DistanceService(NewClient(handler), NoRetryDelayConfig());

        var result = await service.GetDistanceAsync(7.2906m, 80.6337m, 6.9271m, 79.8612m);

        Assert.True(result.Degraded);
        Assert.Null(result.EtaMinutes);
        Assert.InRange(result.DistanceKm, 85m, 105m); // matches the known Kandy-Colombo range
    }

    [Fact]
    public async Task GetDistanceAsync_WhenApiFails_RetriesUpToConfiguredMaxBeforeFallingBack()
    {
        var handler = new ThrowingHttpMessageHandler();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new("MapsApi:MaxRetries", "2"), new("MapsApi:TimeoutSeconds", "5")])
            .Build();
        var service = new DistanceService(NewClient(handler), config);

        var result = await service.GetDistanceAsync(7.29m, 80.63m, 6.93m, 79.86m);

        Assert.True(result.Degraded);
        Assert.Equal(3, handler.CallCount); // initial attempt + 2 retries
    }

    [Fact]
    public async Task GetDistanceAsync_WhenApiReturnsMalformedBody_FallsBackToHaversine()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"distances":[]}""", Encoding.UTF8, "application/json")
        });
        var service = new DistanceService(NewClient(handler), NoRetryDelayConfig());

        var result = await service.GetDistanceAsync(7.29m, 80.63m, 6.93m, 79.86m);

        Assert.True(result.Degraded);
    }

    [Fact]
    public async Task GetDistanceAsync_WhenApiReturnsErrorStatus_FallsBackToHaversine()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = new DistanceService(NewClient(handler), NoRetryDelayConfig());

        var result = await service.GetDistanceAsync(7.29m, 80.63m, 6.93m, 79.86m);

        Assert.True(result.Degraded);
    }
}
