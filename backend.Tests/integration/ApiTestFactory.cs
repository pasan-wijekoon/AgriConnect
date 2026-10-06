using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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

        // Production default is a 48h hold; tests run against a long-lived shared dev
        // database, so keep their Pending reservations short-lived (the expiry sweep
        // then clears them) instead of accumulating hold on the shared demo listings.
        builder.UseSetting("Orders:ReservationTtlMinutes", "30");

        builder.ConfigureServices(services =>
        {
            var distanceServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDistanceService));
            if (distanceServiceDescriptor is not null)
            {
                services.Remove(distanceServiceDescriptor);
            }

            services.AddScoped<IDistanceService, FakeDistanceService>();

            // The real port calls the Logistics Scheduling Agent over HTTP; a developer
            // running it locally would otherwise change these tests' results.
            var schedulingPortDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ILogisticsSchedulingPort));
            if (schedulingPortDescriptor is not null)
            {
                services.Remove(schedulingPortDescriptor);
            }

            services.AddScoped<ILogisticsSchedulingPort, StubLogisticsSchedulingPort>();
        });
    }

    /// <summary>An <see cref="HttpClient"/> carrying a real signed JWT for the given
    /// role/user id (the dev-header bypass no longer exists), or no credentials at all
    /// if the role is omitted (for testing 401s). The token's account must exist and be
    /// active (Program.cs checks it on every request), so a missing user row is created
    /// here; seeded fixture users are left untouched.</summary>
    /// <summary>The account <see cref="CreateAuthedClient"/> uses when only a role is given.</summary>
    public static Guid StableUserId(string role) =>
        new(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes($"agriconnect-test-{role}")));

    public HttpClient CreateAuthedClient(string? role = null, Guid? userId = null)
    {
        var client = CreateClient();
        // Role-only callers (the analytics tests) get one stable test account per role.
        if (role is not null) userId ??= StableUserId(role);
        if (role is not null && userId is not null)
        {
            var token = IssueToken(role, userId.Value);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    private string IssueToken(string role, Guid userId)
    {
        using var scope = Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();

        var user = db.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            user = new User
            {
                Id = userId,
                FullName = $"Test {role}",
                Email = $"test-{userId:N}@agriconnect.test",
                PasswordHash = "not-a-real-hash",
                Role = role,
            };
            db.Users.Add(user);
            try
            {
                db.SaveChanges();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Another test class created the same stable account at the same moment (xUnit runs
                // classes in parallel): use the row that won.
                db.ChangeTracker.Clear();
                user = db.Users.First(u => u.Id == userId);
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "AgriConnect",
            audience: config["Jwt:Audience"] ?? "AgriConnect",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
