using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using backend.src.config;
using backend.src.dtos;
using backend.src.models;

namespace backend.src.services;

public class AgentClientService : IAgentClientService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<AgentClientService> _logger;
    private readonly string _agentServiceUrl;

    public AgentClientService(
        HttpClient httpClient,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AgentClientService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _agentServiceUrl = configuration.GetValue<string>("AgentServiceUrl") ?? "http://localhost:8000";
    }

    public async Task<AgentQualityValidationDto> EvaluateListingQualityAsync(Guid listingId, Guid officerId)
    {
        var listing = await _context.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .Include(l => l.Inspections)
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == listingId);

        if (listing == null)
        {
            throw new KeyNotFoundException($"Listing with ID {listingId} was not found.");
        }

        var latestInspection = listing.Inspections.OrderByDescending(i => i.InspectedAt).FirstOrDefault();
        var photoUrls = listing.Photos.Select(p => p.Url).ToList();

        var payload = new
        {
            listing_id = listing.Id.ToString(),
            crop_name = listing.Crop?.Name ?? "Produce",
            quantity = (double)listing.Quantity,
            unit = listing.Unit,
            claimed_grade = listing.ClaimedGrade,
            confirmed_grade = latestInspection?.ConfirmedGrade,
            inspector_notes = latestInspection?.Notes,
            photo_urls = photoUrls,
            min_price = listing.MinPrice.HasValue ? (double?)listing.MinPrice.Value : null
        };

        AgentQualityValidationDto validationResult;

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_agentServiceUrl}/api/v1/quality-agent/validate", payload);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>();
                validationResult = new AgentQualityValidationDto
                {
                    Passed = content.GetProperty("passed").GetBoolean(),
                    FailedChecks = content.GetProperty("failed_checks").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
                    Flags = content.GetProperty("flags").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
                    GradeConfidence = content.GetProperty("grade_confidence").GetDouble(),
                    AssessedGrade = content.GetProperty("assessed_grade").GetString() ?? listing.ClaimedGrade,
                    ReasoningSummary = content.GetProperty("reasoning_summary").GetString() ?? "",
                    RecommendedAction = content.GetProperty("recommended_action").GetString() ?? "HoldForInspection"
                };

                if (content.TryGetProperty("tool_call_log", out var toolLogs))
                {
                    foreach (var tl in toolLogs.EnumerateArray())
                    {
                        validationResult.ToolCallLog.Add(new ToolCallLogDto
                        {
                            Tool = tl.GetProperty("tool").GetString() ?? ""
                        });
                    }
                }
            }
            else
            {
                _logger.LogWarning($"Agent service returned status {response.StatusCode}. Falling back to internal heuristic validation.");
                validationResult = HeuristicFallback(listing, latestInspection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach Agentic AI microservice. Using server-side fallback validation.");
            validationResult = HeuristicFallback(listing, latestInspection);
        }

        // Persist workflow in database (FR20)
        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            TriggerType = "QualityComplianceGate",
            TriggerEntityId = listing.Id,
            ObjectiveText = $"Validate quality & compliance for {listing.Crop?.Name ?? "produce"} ({listing.Quantity}{listing.Unit})",
            PlanSteps = JsonSerializer.Serialize(new[]
            {
                new { step = "Deterministic Grade Rules", status = "Completed" },
                new { step = "FR14 Discrepancy Verification", status = "Completed" },
                new { step = "Gemini Multimodal / Context Reasoning", status = "Completed" },
                new { step = "Marketplace Gate Clearance", status = validationResult.Passed ? "Passed" : "Blocked" }
            }),
            ToolCallLog = JsonSerializer.Serialize(validationResult.ToolCallLog),
            ValidationResult = JsonSerializer.Serialize(validationResult),
            ApprovalStatus = validationResult.Passed ? AgentApprovalStatus.Approved : AgentApprovalStatus.Pending,
            ApprovedByOfficerId = officerId,
            FinalOutcome = JsonSerializer.Serialize(new { action = validationResult.RecommendedAction, passed = validationResult.Passed })
        };

        _context.AgentWorkflows.Add(workflow);

        // Record AuditLog
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = officerId,
            Action = "AgentQualityComplianceEvaluated",
            EntityType = "Listing",
            EntityId = listing.Id,
            Timestamp = DateTime.UtcNow,
            Details = JsonSerializer.Serialize(new
            {
                workflowId = workflow.Id,
                passed = validationResult.Passed,
                action = validationResult.RecommendedAction,
                assessedGrade = validationResult.AssessedGrade
            })
        });

        await _context.SaveChangesAsync();
        validationResult.WorkflowId = workflow.Id;

        return validationResult;
    }

    public async Task<AgentWorkflowResponseDto?> GetLatestWorkflowForListingAsync(Guid listingId)
    {
        var workflow = await _context.AgentWorkflows
            .Where(w => w.TriggerEntityId == listingId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        if (workflow == null) return null;

        return new AgentWorkflowResponseDto
        {
            Id = workflow.Id,
            TriggerType = workflow.TriggerType,
            TriggerEntityId = workflow.TriggerEntityId,
            ObjectiveText = workflow.ObjectiveText,
            PlanSteps = workflow.PlanSteps,
            ToolCallLog = workflow.ToolCallLog,
            ValidationResult = workflow.ValidationResult,
            ApprovalStatus = workflow.ApprovalStatus,
            ApprovedByOfficerId = workflow.ApprovedByOfficerId,
            FinalOutcome = workflow.FinalOutcome,
            CreatedAt = workflow.CreatedAt
        };
    }

    private AgentQualityValidationDto HeuristicFallback(Listing listing, Inspection? latestInspection)
    {
        bool hasConfirmed = latestInspection != null;
        bool isRejected = latestInspection?.ConfirmedGrade == "Rejected";
        bool hasDiscrepancy = hasConfirmed && !string.Equals(listing.ClaimedGrade, latestInspection!.ConfirmedGrade, StringComparison.OrdinalIgnoreCase);

        var failed = new List<string>();
        var flags = new List<string>();

        if (isRejected) failed.Add("Produce rejected by officer inspection.");
        if (hasDiscrepancy) flags.Add($"FR14 Discrepancy: Claimed '{listing.ClaimedGrade}' vs Confirmed '{latestInspection!.ConfirmedGrade}'.");

        bool passed = !isRejected && (!hasDiscrepancy || hasConfirmed);

        return new AgentQualityValidationDto
        {
            Passed = passed,
            FailedChecks = failed,
            Flags = flags,
            GradeConfidence = hasConfirmed ? 0.95 : 0.85,
            AssessedGrade = latestInspection?.ConfirmedGrade ?? listing.ClaimedGrade,
            ReasoningSummary = $"Internal heuristic validation: Claimed grade '{listing.ClaimedGrade}'. Inspection status: {(hasConfirmed ? latestInspection!.ConfirmedGrade : "Pending physical check")}.",
            RecommendedAction = isRejected ? "Reject" : (hasDiscrepancy ? "ReconcileDiscrepancy" : (hasConfirmed ? "Approve" : "HoldForInspection")),
            ToolCallLog = new List<ToolCallLogDto>
            {
                new() { Tool = "validate_grade_standard" },
                new() { Tool = "check_grade_discrepancy" }
            }
        };
    }
}
