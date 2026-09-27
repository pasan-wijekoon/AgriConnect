using System;
using System.Collections.Generic;

namespace backend.src.dtos;

public class ToolCallLogDto
{
    public string Tool { get; set; } = string.Empty;
    public object? Input { get; set; }
    public object? Output { get; set; }
}

public class AgentQualityValidationDto
{
    public bool Passed { get; set; }
    public List<string> FailedChecks { get; set; } = new();
    public List<string> Flags { get; set; } = new();
    public double GradeConfidence { get; set; }
    public string AssessedGrade { get; set; } = string.Empty;
    public string ReasoningSummary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public List<ToolCallLogDto> ToolCallLog { get; set; } = new();
    public Guid WorkflowId { get; set; }
}

public class AgentWorkflowResponseDto
{
    public Guid Id { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public Guid TriggerEntityId { get; set; }
    public string ObjectiveText { get; set; } = string.Empty;
    public string PlanSteps { get; set; } = string.Empty;
    public string ToolCallLog { get; set; } = string.Empty;
    public string? ValidationResult { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public Guid? ApprovedByOfficerId { get; set; }
    public string? FinalOutcome { get; set; }
    public DateTime CreatedAt { get; set; }
}
