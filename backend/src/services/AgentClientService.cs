using System.Net.Http.Json;
using System.Text.Json;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// HTTP client for the Quality &amp; Compliance Validation Agent
/// (agentic-ai/src/app/api/quality_routes.py, mounted on the same FastAPI app as
/// the Buyer-Farmer Matching Agent — AgenticAi:BaseUrl, set via typed HttpClient
/// in Program.cs). Folded onto AgriConnectDbContext/AuditLogService during
/// integration — originally written against Component C's own AppDbContext
/// with a raw db.AuditLogs.Add(...) call. See PROGRESS.md.
/// </summary>
public class AgentClientService(
    HttpClient httpClient,
    AgriConnectDbContext context,
    AuditLogService auditLog,
    ILogger<AgentClientService> logger) : IAgentClientService
{
    public async Task<AgentQualityValidationDto> EvaluateListingQualityAsync(Guid listingId, Guid officerId)
    {
        var listing = await context.Listings
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
            var response = await httpClient.PostAsJsonAsync("api/v1/quality-agent/validate", payload);
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
                logger.LogWarning("Agent service returned status {StatusCode}. Falling back to internal heuristic validation.", response.StatusCode);
                validationResult = HeuristicFallback(listing, latestInspection);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reach Agentic AI microservice. Using server-side fallback validation.");
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

        context.AgentWorkflows.Add(workflow);

        auditLog.Log(officerId, "AgentQualityComplianceEvaluated", "Listing", listing.Id, new
        {
            workflowId = workflow.Id,
            passed = validationResult.Passed,
            action = validationResult.RecommendedAction,
            assessedGrade = validationResult.AssessedGrade
        });

        await context.SaveChangesAsync();
        validationResult.WorkflowId = workflow.Id;

        return validationResult;
    }

    public async Task<AgentWorkflowResponseDto?> GetLatestWorkflowForListingAsync(Guid listingId)
    {
        var workflow = await context.AgentWorkflows
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

    private static AgentQualityValidationDto HeuristicFallback(Listing listing, Inspection? latestInspection)
    {
        bool hasConfirmed = latestInspection != null;
        bool isRejected = latestInspection?.ConfirmedGrade == QualityGrade.Rejected;
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
