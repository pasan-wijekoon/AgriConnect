using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>FR21 — nearest suitable collection centre + distance estimate.</summary>
[ApiController]
[Route("api/collection-centres")]
[Authorize]
public class CollectionCentresController(
    CollectionCentreService centreService, SchedulingService schedulingService, AgriConnect.Api.Config.AgriConnectDbContext db) : ControllerBase
{
    [HttpGet("nearest")]
    [Authorize(Roles = Roles.BuyerFarmer)]
    public async Task<IActionResult> Nearest(
        [FromQuery] decimal lat, [FromQuery] decimal lng, [FromQuery] Guid? regionId, CancellationToken ct)
    {
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            return Problem(detail: "Invalid coordinates.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await centreService.FindNearestAsync(lat, lng, regionId, ct);
        return Ok(result);
    }

    /// <summary>Not in the plan's original endpoint table — added to back the
    /// Officer scheduling calendar's centre selector (Design.md §33).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.OfficerAdmin)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await centreService.ListAllAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Not in the plan's original endpoint table — added to back the
    /// Officer scheduling calendar (Design.md §33: "centre bookings calendar
    /// (Proposed/Confirmed/Cancelled)").</summary>
    [HttpGet("{centreId:guid}/schedules")]
    [Authorize(Roles = Roles.OfficerAdmin)]
    public async Task<IActionResult> Schedules(Guid centreId, CancellationToken ct)
    {
        var scope = await OfficerScope.ForAsync(db, User.GetUserId(), ct);
        if (scope.IsScoped && scope.CentreId != centreId)
        {
            // Another centre's bookings - 404, same as other cross-centre access.
            return Problem(detail: "Collection centre not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var result = await schedulingService.ListByCentreAsync(centreId, ct);
        return Ok(result);
    }
}
