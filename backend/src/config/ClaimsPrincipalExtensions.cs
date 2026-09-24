using System.Security.Claims;

namespace AgriConnect.Api.Config;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Throws if the principal has no valid NameIdentifier claim — every
    /// authenticated request must have one (dev or real auth alike), so a missing
    /// claim here means a bug in the auth scheme, not a client error to recover from.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !Guid.TryParse(value, out var id))
        {
            throw new InvalidOperationException(
                "Authenticated principal is missing a valid NameIdentifier claim.");
        }

        return id;
    }

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("Authenticated principal is missing a Role claim.");
}
