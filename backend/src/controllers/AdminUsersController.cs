using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// Administrator-only staff management: list accounts, create Officers (bound to a
/// collection centre) and Administrators, activate/deactivate, reset a password.
/// Every change is written to the audit log.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController(AuthService auth, AuditLogService audit, AgriConnectDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await auth.ListUsersAsync(search, role, page, size);
        return Ok(new { items = result.Items, page = Math.Max(1, page), size = Math.Clamp(size, 1, 100), total = result.Total });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateManagedUserRequest request)
    {
        try
        {
            var created = await auth.CreateManagedUserAsync(request);
            await LogAsync("AdminCreateUser", created.Id, new { created.Role, created.Email, created.CollectionCentreId });
            return Created(string.Empty, created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpPatch("{id:guid}/role")]
    public Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeUserRoleRequest request) =>
        Update("AdminChangeUserRole", id, () => auth.ChangeRoleAsync(id, request.Role, request.CollectionCentreId), new { request.Role, request.CollectionCentreId });

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeUserStatusRequest request) =>
        Update("AdminChangeUserStatus", id, () => auth.ChangeStatusAsync(id, request.IsActive, User.GetUserId()), new { request.IsActive });

    [HttpPost("{id:guid}/reset-credentials")]
    public async Task<IActionResult> ResetCredentials(Guid id, [FromBody] ResetUserCredentialsRequest request)
    {
        try
        {
            if (!await auth.ResetCredentialsAsync(id, request.NewPassword)) return NotFound();
            // The new password is never written to the audit log.
            await LogAsync("AdminResetUserCredentials", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private async Task<IActionResult> Update(string action, Guid id, Func<Task<AdminUserResponse?>> operation, object details)
    {
        try
        {
            var result = await operation();
            if (result is null) return NotFound();
            await LogAsync(action, id, details);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private async Task LogAsync(string action, Guid userId, object? details = null)
    {
        audit.Log(User.GetUserId(), action, "User", userId, details);
        await db.SaveChangesAsync();
    }
}
