using System.Security.Claims;
using AgriConnect.Api.Dtos.Reports;
using AgriConnect.Api.Services;
using AgriConnect.Api.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>Component D — exportable summary reports (FR18).</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Administrator")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ReportsController(ReportExportService reports) : ControllerBase
{
    /// <summary>Generates a CSV report. Type is PriceTrends, Listings or Orders.</summary>
    [HttpPost("export")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ReportExportResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportExportResponseDto>> Export([FromBody] ReportExportRequestDto request)
    {
        if (request.DateRangeEnd < request.DateRangeStart)
        {
            ModelState.AddModelError("dateRangeEnd", "'dateRangeEnd' must be on or after 'dateRangeStart'.");
            return ValidationProblem();
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var requestedBy))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unusable identity",
                detail: "The token's user id is missing or is not a GUID, so the report cannot be attributed.");
        }

        var report = await reports.GenerateAsync(
            request.Type, request.DateRangeStart!.Value, request.DateRangeEnd!.Value, requestedBy);

        return AcceptedAtAction(nameof(GetById), new { id = report.Id }, ReportExportResponseDto.From(report));
    }

    /// <summary>Retrieves a previously generated report.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportExportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportExportResponseDto>> GetById(Guid id)
    {
        var report = await reports.GetByIdAsync(id)
            ?? throw new NotFoundException($"Report {id} was not found.");
        return ReportExportResponseDto.From(report);
    }
}
