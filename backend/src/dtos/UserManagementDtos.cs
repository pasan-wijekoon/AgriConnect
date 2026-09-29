namespace AgriConnect.Api.Dtos;

public sealed record AdminUserResponse(
    Guid Id, string FullName, string Email, string Role, string? Phone,
    string? Region, bool IsActive, DateTimeOffset CreatedAt);

public sealed record CreateManagedUserRequest(
    string FullName, string Email, string Password, string Role, string? Phone, string? Region);

public sealed record ChangeUserRoleRequest(string Role);
public sealed record ChangeUserStatusRequest(bool IsActive);
public sealed record ResetUserCredentialsRequest(string NewPassword);
