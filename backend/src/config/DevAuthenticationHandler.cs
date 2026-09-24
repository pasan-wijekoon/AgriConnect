using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AgriConnect.Api.Config;

/// <summary>
/// Dev-mode auth: no shared User/Auth/JWT implementation exists yet anywhere in
/// the repo (plan §0.3/§6), so every component develops against a fake claims
/// principal until it lands. This is that seam for Component B. Swapping to real
/// JWT bearer validation later requires no controller changes, since controllers
/// only ever read User.FindFirst(...)/User.IsInRole(...).
/// </summary>
public static class DevAuthDefaults
{
    public const string Scheme = "Dev";
    public const string RoleHeader = "X-Dev-Role";
    public const string UserIdHeader = "X-Dev-UserId";
}

/// <summary>
/// Builds the ClaimsPrincipal from the X-Dev-Role / X-Dev-UserId headers. Extracted
/// as a pure function, separate from <see cref="DevAuthenticationHandler"/>, so the
/// header-parsing/validation logic is unit-testable without standing up an
/// AuthenticationHandler or a test host.
/// </summary>
public static class DevClaimsPrincipalBuilder
{
    public static readonly IReadOnlyCollection<string> ValidRoles =
        [Roles.Buyer, Roles.Farmer, Roles.Officer, Roles.Admin];

    public static bool TryBuild(
        string? role,
        string? userId,
        out ClaimsPrincipal principal,
        out string? failureReason)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());

        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(userId))
        {
            failureReason = $"Missing required headers: {DevAuthDefaults.RoleHeader} and {DevAuthDefaults.UserIdHeader}.";
            return false;
        }

        if (!ValidRoles.Contains(role, StringComparer.Ordinal))
        {
            failureReason = $"Unknown role '{role}'. Expected one of: {string.Join(", ", ValidRoles)}.";
            return false;
        }

        if (!Guid.TryParse(userId, out _))
        {
            failureReason = $"'{userId}' is not a valid GUID for {DevAuthDefaults.UserIdHeader}.";
            return false;
        }

        var identity = new ClaimsIdentity(DevAuthDefaults.Scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        identity.AddClaim(new Claim(ClaimTypes.Role, role));
        principal = new ClaimsPrincipal(identity);
        failureReason = null;
        return true;
    }
}

public class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers[DevAuthDefaults.RoleHeader].FirstOrDefault();
        var userId = Request.Headers[DevAuthDefaults.UserIdHeader].FirstOrDefault();

        if (!DevClaimsPrincipalBuilder.TryBuild(role, userId, out var principal, out var failureReason))
        {
            return Task.FromResult(AuthenticateResult.Fail(failureReason ?? "Invalid dev auth headers."));
        }

        var ticket = new AuthenticationTicket(principal, DevAuthDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
