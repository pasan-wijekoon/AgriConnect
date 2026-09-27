using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// Component C — Quality Grading &amp; Inspection (FR12–FR14). Officer id/farmer
/// id now come from the authenticated principal (real JWT or the dev-auth
/// fallback), not the original branch's X-Officer-Id header — matching every
/// other authenticated controller in this codebase (e.g. OrdersController).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InspectionsController(IInspectionService inspectionService) : ControllerBase
{
    /// <summary>
    /// Record an inspection outcome (confirmed grade, notes, optional photos).
    /// Automatically checks for claimed-vs-confirmed grade discrepancies (FR12, FR14).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpPost]
    public async Task<ActionResult<InspectionResponseDto>> RecordInspection([FromBody] CreateInspectionRequest request)
    {
        try
        {
            var result = await inspectionService.RecordInspectionAsync(request, User.GetUserId());
            return CreatedAtAction(nameof(GetInspectionById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// List/filter inspection history (FR13).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet]
    public async Task<ActionResult<InspectionPagedResult<InspectionResponseDto>>> GetInspections([FromQuery] InspectionFilterParams filter)
    {
        var result = await inspectionService.GetInspectionsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get specific inspection details.
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InspectionResponseDto>> GetInspectionById(Guid id)
    {
        var result = await inspectionService.GetInspectionByIdAsync(id);
        if (result == null)
        {
            return Problem(detail: $"Inspection with ID {id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }
        return Ok(result);
    }

    /// <summary>
    /// Amend an inspection record with required reason and immutable audit trail (FR13).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InspectionResponseDto>> AmendInspection(Guid id, [FromBody] UpdateInspectionRequest request)
    {
        try
        {
            var result = await inspectionService.AmendInspectionAsync(id, request, User.GetUserId());
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// List listings flagged for claimed-vs-confirmed grade mismatch (FR14).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet("discrepancies")]
    public async Task<ActionResult<List<GradeDiscrepancyDto>>> GetDiscrepancies([FromQuery] bool? onlyUnresolved = true)
    {
        var result = await inspectionService.GetDiscrepanciesAsync(onlyUnresolved);
        return Ok(result);
    }

    /// <summary>
    /// Resolve a grade discrepancy flag with resolution notes (FR14).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpPost("discrepancies/{id:guid}/resolve")]
    public async Task<ActionResult<GradeDiscrepancyDto>> ResolveDiscrepancy(Guid id, [FromBody] ResolveDiscrepancyRequest request)
    {
        try
        {
            var result = await inspectionService.ResolveDiscrepancyAsync(id, request, User.GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Get high-level Quality & Inspection summary dashboard statistics.
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet("stats")]
    public async Task<ActionResult<QualityDashboardStatsDto>> GetDashboardStats()
    {
        var stats = await inspectionService.GetDashboardStatsAsync();
        return Ok(stats);
    }
}
