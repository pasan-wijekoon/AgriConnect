namespace AgriConnect.Api.Models;

public static class AgentApprovalStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string RevisionRequested = "RevisionRequested";
}

/// <summary>
/// Component C — records a single Agentic AI quality/compliance evaluation run
/// against a Listing (FR20: agent plan/tool-call/validation audit trail). Mirrors
/// the "AI Proposal -&gt; Validation -&gt; Officer Review -&gt; Approve/Reject" workflow
/// CLAUDE.md §17/§18 require — the agent never commits the outcome itself,
/// <see cref="ApprovedByOfficerId"/> always records the officer whose action
/// triggered/confirmed it.
/// </summary>
public class AgentWorkflow
{
    public Guid Id { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public Guid TriggerEntityId { get; set; }
    public string ObjectiveText { get; set; } = string.Empty;
    public string PlanSteps { get; set; } = "[]";
    public string ToolCallLog { get; set; } = "[]";
    public string? ValidationResult { get; set; }
    public string ApprovalStatus { get; set; } = AgentApprovalStatus.Pending;
    public Guid? ApprovedByOfficerId { get; set; }
    public string? FinalOutcome { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? ApprovedByOfficer { get; set; }
}
