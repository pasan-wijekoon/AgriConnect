using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.src.dtos;
using backend.src.services;
using backend.src.config;

namespace backend.src.controllers;

[ApiController]
[Route("api/listings")]
public class ListingsInspectionController : ControllerBase
{
    private readonly IInspectionService _inspectionService;
    private readonly IAgentClientService _agentService;

    public ListingsInspectionController(IInspectionService inspectionService, IAgentClientService agentService)
    {
        _inspectionService = inspectionService;
        _agentService = agentService;
    }

    /// <summary>
    /// Agentic AI Quality & Compliance Evaluation Gate Check (FR5, FR14, FR19, FR20).
    /// Executes the LangGraph + Gemini 3.8 Flash agent workflow with deterministic rules.
    /// </summary>
    [HttpPost("{id:guid}/evaluate-compliance")]
    public async Task<ActionResult<AgentQualityValidationDto>> EvaluateCompliance(Guid id, [FromHeader(Name = "X-Officer-Id")] Guid? headerOfficerId)
    {
        try
        {
            var officerId = headerOfficerId ?? DbSeeder.DefaultOfficerId;
            var result = await _agentService.EvaluateListingQualityAsync(id, officerId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during agent quality evaluation.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get the latest Agentic AI workflow record and audit details for a listing (FR20).
    /// </summary>
    [HttpGet("{id:guid}/agent-workflow")]
    public async Task<ActionResult<AgentWorkflowResponseDto>> GetListingAgentWorkflow(Guid id)
    {
        var result = await _agentService.GetLatestWorkflowForListingAsync(id);
        if (result == null)
        {
            return NotFound(new { message = "No agent workflow record found for this listing." });
        }
        return Ok(result);
    }


    /// <summary>
    /// Full inspection history for a listing (FR13).
    /// </summary>
    [HttpGet("{id:guid}/inspections")]
    public async Task<ActionResult<List<InspectionResponseDto>>> GetInspectionsForListing(Guid id)
    {
        var result = await _inspectionService.GetInspectionsByListingIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Business-specific gate: Listing becomes Published and buyer-visible ONLY after passing quality inspection AND receiving officer approval (FR5).
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ListingSummaryDto>> PublishListing(Guid id, [FromHeader(Name = "X-Officer-Id")] Guid? headerOfficerId)
    {
        try
        {
            var officerId = headerOfficerId ?? DbSeeder.DefaultOfficerId;
            var result = await _inspectionService.PublishListingWithGateCheckAsync(id, officerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Business rule gate failure
            return StatusCode(422, new { error = ex.Message, isGateBlocked = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while publishing the listing.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get listings awaiting officer quality inspection.
    /// </summary>
    [HttpGet("pending-inspection")]
    public async Task<ActionResult<List<ListingSummaryDto>>> GetPendingListings()
    {
        var result = await _inspectionService.GetPendingListingsForInspectionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get farmer's own listings for mobile inspection status tracking (FR11, FR13).
    /// </summary>
    [HttpGet("my-listings")]
    public async Task<ActionResult<List<ListingSummaryDto>>> GetMyListings([FromHeader(Name = "X-Farmer-Id")] Guid? headerFarmerId)
    {
        var farmerId = headerFarmerId ?? DbSeeder.FarmerKamalId;
        var result = await _inspectionService.GetFarmerListingsAsync(farmerId);
        return Ok(result);
    }
}

