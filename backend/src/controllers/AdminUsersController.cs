using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public class AdminUsersController(AuthService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await auth.ListUsersAsync(search, role, page, size);
        return Ok(new { items = result.Items, page = Math.Max(1, page), size = Math.Clamp(size, 1, 100), total = result.Total });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateManagedUserRequest request) =>
        Created(string.Empty, await auth.CreateManagedUserAsync(request));

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeUserRoleRequest request) =>
        await Update(() => auth.ChangeRoleAsync(id, request.Role));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeUserStatusRequest request) =>
        await Update(() => auth.ChangeStatusAsync(id, request.IsActive));

    [HttpPost("{id:guid}/reset-credentials")]
    public async Task<IActionResult> ResetCredentials(Guid id, ResetUserCredentialsRequest request) =>
        await auth.ResetCredentialsAsync(id, request.NewPassword) ? NoContent() : NotFound();

    private static async Task<IActionResult> Update(Func<Task<AdminUserResponse?>> operation)
    {
        var result = await operation(); return result == null ? new NotFoundResult() : new OkObjectResult(result);
    }
}
