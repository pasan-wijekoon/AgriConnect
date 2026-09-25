using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// Read/mark-read surface for the shared Notification table (FR22, plan §12).
/// Not role-restricted beyond authentication — every role can receive
/// notifications, and each user only ever sees their own (IDOR-safe: scoped to
/// the caller's own id server-side, not a client-supplied one).
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await notifications.ListForUserAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var ok = await notifications.MarkReadAsync(id, User.GetUserId(), ct);
        return ok ? NoContent() : Problem(detail: "Notification not found.", statusCode: StatusCodes.Status404NotFound);
    }
}
