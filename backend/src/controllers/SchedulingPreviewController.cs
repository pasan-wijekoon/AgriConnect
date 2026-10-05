using AgriConnect.Api.Dtos.Agents;
using AgriConnect.Api.Services.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// Component D — lets officers try the Logistics Scheduling Agent directly. Nothing is
/// saved: real pickup slots are proposed through Component B's order scheduling and
/// still need an Officer's approval.
/// </summary>
[ApiController]
[Route("api/analytics/scheduling-preview")]
[Authorize(Roles = "Officer,Administrator")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class SchedulingPreviewController(LogisticsAgentClient agent) : ControllerBase
{
    /// <summary>Ask the agent for the earliest conflict-free slot. Preview only; nothing is stored.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SchedulingPreviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SchedulingPreviewResponseDto>> Preview(
        [FromBody] SchedulingPreviewRequestDto request, CancellationToken cancellationToken)
    {
        var window = request.PreferredWindow!;
        if (window.End <= window.Start)
        {
            ModelState.AddModelError("preferredWindow.end", "The window must end after it starts.");
        }
        for (var i = 0; i < request.ExistingBookings.Count; i++)
        {
            var b = request.ExistingBookings[i];
            if (b.SlotEnd <= b.SlotStart)
            {
                ModelState.AddModelError($"existingBookings[{i}].slotEnd", "A booking must end after it starts.");
            }
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        try
        {
            return await agent.PreviewAsync(
                request.CentreId.Trim(),
                window.Start!.Value,
                window.End!.Value,
                request.ExistingBookings.Select(b => (b.SlotStart!.Value, b.SlotEnd!.Value)).ToList(),
                cancellationToken);
        }
        catch (NoSlotAvailableException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "No free slot", detail: ex.Message);
        }
        catch (AgentUnavailableException ex)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI scheduling unavailable", detail: ex.Message);
        }
    }
}
