using System.Security.Claims;
using AgriConnect.Api.Config;

namespace backend.Tests.config;

public class DevClaimsPrincipalBuilderTests
{
    private static readonly string ValidUserId = Guid.NewGuid().ToString();

    [Fact]
    public void TryBuild_WithValidRoleAndUserId_ReturnsPrincipalWithBothClaims()
    {
        var ok = DevClaimsPrincipalBuilder.TryBuild(Roles.Buyer, ValidUserId, out var principal, out var failureReason);

        Assert.True(ok);
        Assert.Null(failureReason);
        Assert.Equal(ValidUserId, principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.True(principal.IsInRole(Roles.Buyer));
    }

    [Theory]
    [InlineData(null, "not-blank")]
    [InlineData("", "not-blank")]
    [InlineData("   ", "not-blank")]
    [InlineData("Buyer", null)]
    [InlineData("Buyer", "")]
    public void TryBuild_WithMissingHeader_Fails(string? role, string? userId)
    {
        var ok = DevClaimsPrincipalBuilder.TryBuild(role, userId, out _, out var failureReason);

        Assert.False(ok);
        Assert.NotNull(failureReason);
    }

    [Fact]
    public void TryBuild_WithUnknownRole_Fails()
    {
        var ok = DevClaimsPrincipalBuilder.TryBuild("SuperAdmin", ValidUserId, out _, out var failureReason);

        Assert.False(ok);
        Assert.Contains("Unknown role", failureReason);
    }

    [Fact]
    public void TryBuild_WithNonGuidUserId_Fails()
    {
        var ok = DevClaimsPrincipalBuilder.TryBuild(Roles.Officer, "not-a-guid", out _, out var failureReason);

        Assert.False(ok);
        Assert.Contains("not a valid GUID", failureReason);
    }

    [Theory]
    [InlineData(Roles.Buyer)]
    [InlineData(Roles.Farmer)]
    [InlineData(Roles.Officer)]
    [InlineData(Roles.Admin)]
    public void TryBuild_AcceptsEveryDocumentedRole(string role)
    {
        var ok = DevClaimsPrincipalBuilder.TryBuild(role, ValidUserId, out var principal, out _);

        Assert.True(ok);
        Assert.True(principal.IsInRole(role));
    }
}
