using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Dtos;

public sealed record AdminUserResponse(
    Guid Id, string FullName, string Email, string Role, string? Phone,
    string? Region, bool IsActive, DateTimeOffset CreatedAt, Guid? CollectionCentreId = null);

/// <summary>
/// Administrator creates an Officer or another Administrator. An Officer MUST be bound to
/// a collection centre: an Officer with no centre is unscoped and would see every centre's
/// orders (OfficerScope), so the service rejects an Officer without one.
/// </summary>
public sealed class CreateManagedUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    /// <summary>Required for the Officer role; ignored for Administrators.</summary>
    public Guid? CollectionCentreId { get; set; }
}

public sealed record ChangeUserRoleRequest(string Role, Guid? CollectionCentreId = null);
public sealed record ChangeUserStatusRequest(bool IsActive);

public sealed class ResetUserCredentialsRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
