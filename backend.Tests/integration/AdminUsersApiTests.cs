using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.integration;

/// <summary>
/// Administrator staff management (/api/admin/users): creating Officers bound to a
/// collection centre, validation, authorisation, deactivation and the self-lockout guard.
/// </summary>
public class AdminUsersApiTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly ApiTestFactory _factory;

    public AdminUsersApiTests(ApiTestFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>The suite runs against a shared dev database, so remove the accounts it created
    /// (otherwise they pile up in the Administrator's Officers list).</summary>
    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgriConnect.Api.Config.AgriConnectDbContext>();
        await db.Users
            .Where(u => u.Email.StartsWith("officer-") && u.Email.EndsWith("@agriconnect.test"))
            .ExecuteDeleteAsync();
    }

    private static async Task<JsonElement> JsonBody(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();

    private static string NewEmail() => $"officer-{Guid.NewGuid():N}@agriconnect.test";

    private async Task<Guid> AnyCentreIdAsync(HttpClient admin)
    {
        var centres = await JsonBody(await admin.GetAsync("/api/collection-centres"));
        return centres.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static object Body(string email, Guid? centreId, string role = "Officer", string password = "Officer123") => new
    {
        fullName = "Test Officer",
        email,
        password,
        role,
        phone = "+94710000000",
        collectionCentreId = centreId,
    };

    [Fact]
    public async Task CreateOfficer_WithCentre_Returns201_AndTheOfficerCanSignInBoundToThatCentre()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);
        var email = NewEmail();

        var response = await admin.PostAsJsonAsync("/api/admin/users", Body(email, centreId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await JsonBody(response);
        Assert.Equal("Officer", created.GetProperty("role").GetString());
        Assert.Equal(centreId, created.GetProperty("collectionCentreId").GetGuid());

        // Typed in a different case: admin-created emails are lower-cased, login is case-insensitive.
        using var anonymous = _factory.CreateAuthedClient();
        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = email.ToUpperInvariant(), password = "Officer123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await JsonBody(login);
        Assert.Equal("Officer", session.GetProperty("role").GetString());
        Assert.Equal(centreId, session.GetProperty("collectionCentreId").GetGuid());
    }

    [Fact]
    public async Task CreateOfficer_WithoutACentre_Returns400()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);

        var response = await admin.PostAsJsonAsync("/api/admin/users", Body(NewEmail(), null));

        // An unbound Officer would be unscoped and see every centre's orders.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOfficer_WithUnknownCentre_Returns400()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);

        var response = await admin.PostAsJsonAsync("/api/admin/users", Body(NewEmail(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short1")]       // under 8 characters
    [InlineData("onlyletters")]  // no digit
    [InlineData("12345678")]     // no letter
    public async Task CreateOfficer_WithWeakPassword_Returns400(string password)
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);

        var response = await admin.PostAsJsonAsync("/api/admin/users", Body(NewEmail(), centreId, password: password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Farmer")]
    [InlineData("Buyer")]
    [InlineData("Nonsense")]
    public async Task CreateUser_WithARoleAdministratorsMayNotCreate_Returns400(string role)
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);

        var response = await admin.PostAsJsonAsync("/api/admin/users", Body(NewEmail(), centreId, role));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithAnInvalidEmail_Returns400()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);

        var response = await admin.PostAsJsonAsync("/api/admin/users", Body("not-an-email", centreId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOfficer_WithAnEmailAlreadyInUse_Returns409_EvenInADifferentCase()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/users", Body(email, centreId))).StatusCode);

        var again = await admin.PostAsJsonAsync("/api/admin/users", Body(email.ToUpperInvariant(), centreId));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Officer)]
    [InlineData(Roles.Farmer)]
    [InlineData(Roles.Buyer)]
    public async Task StaffManagement_IsAdministratorOnly(string role)
    {
        using var client = _factory.CreateAuthedClient(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/admin/users", Body(NewEmail(), Guid.NewGuid()))).StatusCode);
    }

    [Fact]
    public async Task StaffManagement_WithoutCredentials_Returns401()
    {
        using var anonymous = _factory.CreateAuthedClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task DeactivatedOfficer_CanNoLongerSignIn_AndCanBeReactivated()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);
        var email = NewEmail();
        var created = await JsonBody(await admin.PostAsJsonAsync("/api/admin/users", Body(email, centreId)));
        var id = created.GetProperty("id").GetGuid();
        using var anonymous = _factory.CreateAuthedClient();

        var off = await admin.PatchAsJsonAsync($"/api/admin/users/{id}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "Officer123" })).StatusCode);

        await admin.PatchAsJsonAsync($"/api/admin/users/{id}/status", new { isActive = true });
        Assert.Equal(HttpStatusCode.OK,
            (await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "Officer123" })).StatusCode);
    }

    [Fact]
    public async Task Administrator_CannotDeactivateTheirOwnAccount()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var self = ApiTestFactory.StableUserId(Roles.Admin);

        var response = await admin.PatchAsJsonAsync($"/api/admin/users/{self}/status", new { isActive = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetCredentials_SetsANewPassword_AndRejectsAWeakOne()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);
        var centreId = await AnyCentreIdAsync(admin);
        var email = NewEmail();
        var id = (await JsonBody(await admin.PostAsJsonAsync("/api/admin/users", Body(email, centreId)))).GetProperty("id").GetGuid();
        using var anonymous = _factory.CreateAuthedClient();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"/api/admin/users/{id}/reset-credentials", new { newPassword = "weak" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PostAsJsonAsync($"/api/admin/users/{id}/reset-credentials", new { newPassword = "NewSecret99" })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "Officer123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "NewSecret99" })).StatusCode);
    }

    [Fact]
    public async Task ListUsers_FilteredToOfficers_ReturnsOnlyOfficers()
    {
        using var admin = _factory.CreateAuthedClient(Roles.Admin);

        var json = await JsonBody(await admin.GetAsync("/api/admin/users?role=Officer&size=100"));

        Assert.All(json.GetProperty("items").EnumerateArray(), u => Assert.Equal("Officer", u.GetProperty("role").GetString()));
    }
}
