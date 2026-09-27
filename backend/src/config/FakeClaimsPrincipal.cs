using System.Security.Claims;

namespace AgriConnect.Api.Config;

/// <summary>
/// Development-only middleware that signs a request in as a fake user when it explicitly
/// asks to, so endpoints protected by <c>[Authorize(Roles = "Officer,Administrator")]</c>
/// can be exercised before a real login flow exists.
///
/// Requests that carry an Authorization header are left alone, so a real or deliberately
/// invalid token still goes through normal JWT validation. A request with no
/// Authorization header AND no X-Dev-Role header is left genuinely unauthenticated
/// (falls through to [Authorize] -> 401) — this does NOT default to any role, so the
/// "caller sent nothing" case stays testable (see the 2026-09-27 note below).
///
/// Send <c>X-Dev-Role: Administrator</c> (or another role) to act as that role, and
/// optionally <c>X-Dev-UserId: &lt;guid&gt;</c> to act as a specific user of that role
/// rather than the fixed default — needed by Component B's IDOR tests, which exercise
/// two different Buyers/Farmers, not just one fixed identity per role. This merges
/// Component D's original design with Component B's DevAuthenticationHandler header
/// contract (same header names, same behaviour) so both components' existing tests
/// keep working unchanged against one auth mechanism.
/// </summary>
public class FakeClaimsPrincipalMiddleware
{
    // All defaults are valid GUIDs (not the original component's opaque string ids)
    // because ClaimsPrincipalExtensions.GetUserId() requires the NameIdentifier claim
    // to parse as a Guid — every controller in the app relies on that, not just Component D's.
    public const string DevUserId = "01111111-0000-0000-0000-000000000001";
    public const string DevRoleHeader = "X-Dev-Role";
    public const string DevUserIdHeader = "X-Dev-UserId";

    // The admin user seeded by SharedReferenceSeeder. A GUID is required because reports
    // record the requester in ReportExport.RequestedBy.
    public const string DevAdminId = "a1111111-0000-0000-0000-000000000001";
    public const string DevFarmerId = "d1111111-0000-0000-0000-000000000001";
    public const string DevBuyerId = "b1111111-0000-0000-0000-000000000001";

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
        //
        // Integration decision (2026-09-27): a request with NO X-Dev-Role header is left
        // genuinely unauthenticated (falls through to [Authorize] -> 401), rather than the
        // original design's default-to-Officer. Defaulting every headerless request to
        // Officer meant there was no way to exercise the "caller sent nothing" 401 path at
        // all — it silently broke Component B's existing fail-closed security tests
        // (Create_Unauthenticated_Returns401 and friends). Sending an explicit
        // X-Dev-Role still works exactly as before for quick manual testing.
        if (_environment.IsDevelopment()
            && context.User.Identity?.IsAuthenticated != true
            && !context.Request.Headers.ContainsKey("Authorization")
            && context.Request.Headers.ContainsKey(DevRoleHeader))
        {
            var requested = context.Request.Headers[DevRoleHeader].ToString();
            var role = Roles.FirstOrDefault(r => r.Equals(requested, StringComparison.OrdinalIgnoreCase));

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

            var requestedUserId = context.Request.Headers[DevUserIdHeader].ToString();
            string userId;
            if (!string.IsNullOrEmpty(requestedUserId))
            {
                if (!Guid.TryParse(requestedUserId, out _))
                {
                    await Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid dev user id",
                            detail: $"'{requestedUserId}' is not a valid GUID for {DevUserIdHeader}.",
                            instance: context.Request.Path)
                        .ExecuteAsync(context);
                    return;
                }

                userId = requestedUserId;
            }
            else
            {
                userId = role switch
                {
                    "Administrator" => DevAdminId,
                    "Farmer" => DevFarmerId,
                    "Buyer" => DevBuyerId,
                    _ => DevUserId,
                };
            }

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
