using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.src.config;
using backend.src.dtos;
using backend.src.services;

namespace backend.src.controllers;

[ApiController]
[Route("api/[controller]")]
public class InspectionsController : ControllerBase
{
    private readonly IInspectionService _inspectionService;

    public InspectionsController(IInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    /// <summary>
    /// Record an inspection outcome (confirmed grade, notes, optional photos).
    /// Automatically checks for claimed-vs-confirmed grade discrepancies (FR12, FR14).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InspectionResponseDto>> RecordInspection([FromBody] CreateInspectionRequest request, [FromHeader(Name = "X-Officer-Id")] Guid? headerOfficerId)
    {
        try
        {
            // If officer ID is not provided in header, use default test officer
            var officerId = headerOfficerId ?? DbSeeder.DefaultOfficerId;
            var result = await _inspectionService.RecordInspectionAsync(request, officerId);
            return CreatedAtAction(nameof(GetInspectionById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while recording the inspection.", details = ex.Message });
        }
    }

    /// <summary>
    /// List/filter inspection history (FR13).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<InspectionResponseDto>>> GetInspections([FromQuery] InspectionFilterParams filter)
    {
        var result = await _inspectionService.GetInspectionsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get specific inspection details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InspectionResponseDto>> GetInspectionById(Guid id)
    {
        var result = await _inspectionService.GetInspectionByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { error = $"Inspection with ID {id} was not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Amend an inspection record with required reason and immutable audit trail (FR13).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InspectionResponseDto>> AmendInspection(Guid id, [FromBody] UpdateInspectionRequest request, [FromHeader(Name = "X-Officer-Id")] Guid? headerOfficerId)
    {
        try
        {
            var officerId = headerOfficerId ?? DbSeeder.DefaultOfficerId;
            var result = await _inspectionService.AmendInspectionAsync(id, request, officerId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while amending the inspection.", details = ex.Message });
        }
    }

    /// <summary>
    /// List listings flagged for claimed-vs-confirmed grade mismatch (FR14).
    /// </summary>
    [HttpGet("discrepancies")]
    public async Task<ActionResult<List<GradeDiscrepancyDto>>> GetDiscrepancies([FromQuery] bool? onlyUnresolved = true)
    {
        var result = await _inspectionService.GetDiscrepanciesAsync(onlyUnresolved);
        return Ok(result);
    }

    /// <summary>
    /// Resolve a grade discrepancy flag with resolution notes (FR14).
    /// </summary>
    [HttpPost("discrepancies/{id:guid}/resolve")]
    public async Task<ActionResult<GradeDiscrepancyDto>> ResolveDiscrepancy(Guid id, [FromBody] ResolveDiscrepancyRequest request, [FromHeader(Name = "X-Officer-Id")] Guid? headerOfficerId)
    {
        try
        {
            var officerId = headerOfficerId ?? DbSeeder.DefaultOfficerId;
            var result = await _inspectionService.ResolveDiscrepancyAsync(id, request, officerId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while resolving the discrepancy.", details = ex.Message });
        }
    }

    /// <summary>
    /// Get high-level Quality & Inspection summary dashboard statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<QualityDashboardStatsDto>> GetDashboardStats()
    {
        var stats = await _inspectionService.GetDashboardStatsAsync();
        return Ok(stats);
    }
}
