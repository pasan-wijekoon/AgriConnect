using System.Security.Claims;

namespace AgriConnect.Api.Config;

/// <summary>
/// Development-only middleware that signs every tokenless request in as a fake Officer,
/// so endpoints protected by <c>[Authorize(Roles = "Officer,Administrator")]</c> can be
/// exercised before a real login flow exists.
///
/// Requests that carry an Authorization header are left alone, so a real or deliberately
/// invalid token still goes through normal JWT validation.
/// </summary>
public class FakeClaimsPrincipalMiddleware
{
    public const string DevUserId = "dev-officer-id";
    public const string DevRole = "Officer";
    private const string AuthenticationType = "DevFake";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public FakeClaimsPrincipalMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Re-checked here as well as at registration: this bypasses authentication,
        // so it must never activate outside Development even if registered by mistake.
        if (_environment.IsDevelopment()
            && context.User.Identity?.IsAuthenticated != true
            && !context.Request.Headers.ContainsKey("Authorization"))
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim("sub", DevUserId),
                    // JwtBearer maps "sub" to NameIdentifier on real tokens; mirror that so
                    // controllers read the user id the same way in both cases.
                    new Claim(ClaimTypes.NameIdentifier, DevUserId),
                    new Claim(ClaimTypes.Role, DevRole),
                ],
                AuthenticationType);

            context.User = new ClaimsPrincipal(identity);
        }

        return _next(context);
    }
}

public static class FakeClaimsPrincipalExtensions
{
    /// <summary>Must be placed after UseAuthentication() and before UseAuthorization().</summary>
    public static IApplicationBuilder UseFakeClaimsPrincipal(this IApplicationBuilder app) =>
        app.UseMiddleware<FakeClaimsPrincipalMiddleware>();
}
