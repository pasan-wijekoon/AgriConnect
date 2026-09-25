using System.Text.Json;
using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests.integration;

/// <summary>
/// Deterministic stand-in for the real OpenRouteService-backed <see cref="IDistanceService"/>
/// — without this, FR21's nearest-centre endpoint would make real outbound HTTP calls in
/// every integration test run (same reason <c>DistanceServiceTests</c> uses a fake
/// <c>HttpMessageHandler</c> instead of a live API key). Deterministic non-degraded
/// distance so tests can assert on it directly.
/// </summary>
internal class FakeDistanceService : IDistanceService
{
    public Task<DistanceResult> GetDistanceAsync(
        decimal originLat, decimal originLng, decimal destLat, decimal destLng,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new DistanceResult(DistanceKm: 5m, EtaMinutes: 10, Degraded: false));
}

/// <summary>
/// Full-stack integration test host (Phase 12, plan §13's "API/integration" row) —
/// boots the real <c>Program.cs</c> pipeline (dev-auth, authorization, controllers,
/// RFC 7807 error responses) over real HTTP via <see cref="WebApplicationFactory{TEntryPoint}"/>,
/// against the same live local PostgreSQL database every other phase's manual
/// testing already uses — deliberately not the EF Core InMemory provider, which
/// does not support transactions at all, and <c>StockReservationService</c>'s
/// order-creation path always runs inside one (plan §7.2). This mirrors exactly
/// why <c>StockReservationServiceConcurrencyTests</c> already requires real
/// Postgres rather than InMemory (see its own doc comment).
///
/// One swap is still made: <see cref="IDistanceService"/> is replaced with
/// <see cref="FakeDistanceService"/> so the nearest-centre endpoint never makes a
/// real outbound HTTP call to OpenRouteService during a test run.
///
/// The Development environment is forced on so Program.cs's own seeding block
/// runs, guaranteeing the fixed, known <c>OrderLogisticsFixtures</c> demo data
/// exists — tests reuse those ids directly for read-only/ownership assertions,
/// and always create their own fresh orders (never mutate seeded fixtures) for
/// anything that changes state, so runs stay independent of what earlier manual
/// testing sessions left behind in this long-lived dev database.
/// </summary>
public class ApiTestFactory : WebApplicationFactory<Program>
{
    /// <summary>Matches Program.cs's global JsonStringEnumConverter MVC option —
    /// System.Net.Http.Json's ReadFromJsonAsync doesn't pick that server-side
    /// option up automatically, so response bodies containing an enum (e.g.
    /// OrderStatus) must be read with this explicitly on the client side too.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Without this, the server's camelCase JSON ("buyerId") silently fails
        // to bind to these PascalCase record properties (BuyerId) — records
        // deserialize "successfully" but every unmatched property is left at
        // its default (Guid.Empty, "", default DateTimeOffset), with no
        // exception thrown. Caught by every assertion in this suite that
        // checks an actual field value, not just a status code.
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var distanceServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDistanceService));
            if (distanceServiceDescriptor is not null)
            {
                services.Remove(distanceServiceDescriptor);
            }

            services.AddScoped<IDistanceService, FakeDistanceService>();
        });
    }

    /// <summary>An <see cref="HttpClient"/> pre-set with dev-auth headers for the
    /// given role/user id, or with no auth headers at all if both are omitted
    /// (for testing 401s).</summary>
    public HttpClient CreateAuthedClient(string? role = null, Guid? userId = null)
    {
        var client = CreateClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(DevAuthDefaults.RoleHeader, role);
        }
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add(DevAuthDefaults.UserIdHeader, userId.Value.ToString());
        }
        return client;
    }
}
