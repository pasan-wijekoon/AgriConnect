using System.Security.Claims;

namespace AgriConnect.Api.Config;

/// <summary>
/// Development-only middleware that signs every tokenless request in as a fake Officer,
/// so endpoints protected by <c>[Authorize(Roles = "Officer,Administrator")]</c> can be
/// exercised before a real login flow exists.
///
/// Requests that carry an Authorization header are left alone, so a real or deliberately
/// invalid token still goes through normal JWT validation.
///
/// Send <c>X-Dev-Role: Administrator</c> (or another role) to act as that role instead.
/// </summary>
public class FakeClaimsPrincipalMiddleware
{
    public const string DevUserId = "dev-officer-id";
    public const string DevRole = "Officer";
    public const string DevRoleHeader = "X-Dev-Role";

    // The admin user seeded by SharedReferenceSeeder. A GUID is required because reports
    // record the requester in ReportExport.RequestedBy.
    public const string DevAdminId = "a1111111-0000-0000-0000-000000000001";

    private static readonly string[] Roles = ["Farmer", "Buyer", "Officer", "Administrator"];
    private const string AuthenticationType = "DevFake";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public FakeClaimsPrincipalMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Re-checked here as well as at registration: this bypasses authentication,
        // so it must never activate outside Development even if registered by mistake.
        if (_environment.IsDevelopment()
            && context.User.Identity?.IsAuthenticated != true
            && !context.Request.Headers.ContainsKey("Authorization"))
        {
            var requested = context.Request.Headers[DevRoleHeader].ToString();
            var role = string.IsNullOrEmpty(requested)
                ? DevRole
                : Roles.FirstOrDefault(r => r.Equals(requested, StringComparison.OrdinalIgnoreCase));

            if (role is null)
            {
                // Fail loudly: silently falling back to Officer would hide a typo as a 403.
                await Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid dev role",
                        detail: $"{DevRoleHeader} must be one of: {string.Join(", ", Roles)}.",
                        instance: context.Request.Path)
                    .ExecuteAsync(context);
                return;
            }

            var userId = role switch
            {
                "Administrator" => DevAdminId,
                "Officer" => DevUserId,
                _ => $"dev-{role.ToLowerInvariant()}-id",
            };

            var identity = new ClaimsIdentity(
                [
                    new Claim("sub", userId),
                    // JwtBearer maps "sub" to NameIdentifier on real tokens; mirror that so
                    // controllers read the user id the same way in both cases.
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, role),
                ],
                AuthenticationType);

            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}

public static class FakeClaimsPrincipalExtensions
{
    /// <summary>Must be placed after UseAuthentication() and before UseAuthorization().</summary>
    public static IApplicationBuilder UseFakeClaimsPrincipal(this IApplicationBuilder app) =>
        app.UseMiddleware<FakeClaimsPrincipalMiddleware>();
}
