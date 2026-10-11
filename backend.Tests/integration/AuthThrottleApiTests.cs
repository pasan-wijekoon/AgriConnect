using System.Net;
using System.Net.Http.Json;
using AgriConnect.Api.Services;
using Xunit;

namespace backend.Tests.integration;

/// <summary>SEC-06 over real HTTP: repeated wrong passwords for one account get 429 + Retry-After.</summary>
public class AuthThrottleApiTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AuthThrottleApiTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task RepeatedWrongPasswords_AreThrottled_WithRetryAfter()
    {
        var client = _factory.CreateClient();
        var email = $"nobody-{Guid.NewGuid():N}@agriconnect.test"; // unknown account: every attempt fails

        for (var i = 0; i < LoginAttemptTracker.MaxFailures; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong" });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task FailuresForOneAccount_DoNotBlockAnotherAccount()
    {
        var client = _factory.CreateClient();
        var attacked = $"nobody-{Guid.NewGuid():N}@agriconnect.test";
        for (var i = 0; i < LoginAttemptTracker.MaxFailures + 1; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new { email = attacked, password = "wrong" });
        }

        var other = await client.PostAsJsonAsync("/api/auth/login",
            new { email = $"someone-else-{Guid.NewGuid():N}@agriconnect.test", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, other.StatusCode); // answered normally, not 429
    }

    [Fact]
    public async Task EveryResponse_CarriesNosniff()
    {
        var response = await _factory.CreateClient().GetAsync("/api/listings");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }
}
