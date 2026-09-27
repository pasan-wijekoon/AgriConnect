using System;

namespace backend.src.models;

public static class AgentApprovalStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string RevisionRequested = "RevisionRequested";
}

public class AgentWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TriggerType { get; set; } = string.Empty; // e.g., "QualityComplianceGate", "ListingSubmitted"
    public Guid TriggerEntityId { get; set; }
    public string ObjectiveText { get; set; } = string.Empty;
    public string PlanSteps { get; set; } = "[]"; // JSON array of step details
    public string ToolCallLog { get; set; } = "[]"; // JSON array of tool execution records
    public string? ValidationResult { get; set; } // JSON structured validation output
    public string ApprovalStatus { get; set; } = AgentApprovalStatus.Pending;
    public Guid? ApprovedByOfficerId { get; set; }
    public string? FinalOutcome { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? ApprovedByOfficer { get; set; }
}
