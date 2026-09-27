using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriConnect.Api.Controllers;

/// <summary>
/// Component C — Quality Grading &amp; Inspection: the FR5 publish gate and its
/// supporting listing views. This is now the *only* way a Listing reaches
/// Status = Published — Component A's original ungated PATCH
/// /api/listings/{id}/approve was removed during integration (see
/// InspectionService.PublishListingWithGateCheckAsync and PROGRESS.md).
/// </summary>
[ApiController]
[Route("api/listings")]
[Authorize]
public class ListingsInspectionController(IInspectionService inspectionService, IAgentClientService agentService) : ControllerBase
{
    /// <summary>
    /// Agentic AI Quality & Compliance Evaluation Gate Check (FR5, FR14, FR19, FR20).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpPost("{id:guid}/evaluate-compliance")]
    public async Task<ActionResult<AgentQualityValidationDto>> EvaluateCompliance(Guid id)
    {
        try
        {
            var result = await agentService.EvaluateListingQualityAsync(id, User.GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Get the latest Agentic AI workflow record and audit details for a listing (FR20).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet("{id:guid}/agent-workflow")]
    public async Task<ActionResult<AgentWorkflowResponseDto>> GetListingAgentWorkflow(Guid id)
    {
        var result = await agentService.GetLatestWorkflowForListingAsync(id);
        if (result == null)
        {
            return Problem(detail: "No agent workflow record found for this listing.", statusCode: StatusCodes.Status404NotFound);
        }
        return Ok(result);
    }

    /// <summary>
    /// Full inspection history for a listing (FR13).
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet("{id:guid}/inspections")]
    public async Task<ActionResult<List<InspectionResponseDto>>> GetInspectionsForListing(Guid id)
    {
        var result = await inspectionService.GetInspectionsByListingIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// FR5 gate: a listing becomes Published ONLY after passing quality inspection
    /// AND receiving officer approval. Administrator is also allowed to trigger
    /// this — Component A's Admin Dashboard used to call the now-removed, ungated
    /// PATCH .../approve for the same "publish this listing" action.
    /// </summary>
    [Authorize(Roles = Roles.OfficerAdmin)]
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ListingSummaryDto>> PublishListing(Guid id)
    {
        try
        {
            var result = await inspectionService.PublishListingWithGateCheckAsync(id, User.GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Business rule gate failure — quality gate blocked, not a server error.
            var problem = Problem(detail: ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
            if (problem.Value is Microsoft.AspNetCore.Mvc.ProblemDetails details)
            {
                details.Extensions["isGateBlocked"] = true;
            }
            return problem;
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Get listings awaiting officer quality inspection.
    /// </summary>
    [Authorize(Roles = Roles.Officer)]
    [HttpGet("pending-inspection")]
    public async Task<ActionResult<List<ListingSummaryDto>>> GetPendingListings()
    {
        var result = await inspectionService.GetPendingListingsForInspectionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get the authenticated farmer's own listings for mobile inspection status
    /// tracking (FR11, FR13).
    /// </summary>
    [Authorize(Roles = Roles.Farmer)]
    [HttpGet("my-listings")]
    public async Task<ActionResult<List<ListingSummaryDto>>> GetMyListings()
    {
        var result = await inspectionService.GetFarmerListingsAsync(User.GetUserId());
        return Ok(result);
    }
}
