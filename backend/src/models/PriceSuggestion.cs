using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class PriceSuggestion
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    [Column(TypeName = "decimal(12,2)")]
    public decimal SuggestedPriceMin { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal SuggestedPriceMax { get; set; }

    [Column(TypeName = "decimal(4,3)")]
    public decimal Confidence { get; set; } // 0.000 to 1.000

    [Required]
    public string ReasoningSummary { get; set; } = string.Empty;

    // FK → AgentWorkflow — will be wired when the orchestration layer is built
    public Guid? AgentWorkflowId { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Proposed";
    // Valid: Proposed, Approved, Rejected, Revised

    // Set when a raw orchestrated proposal fails deterministic validation and is
    // never shown to an officer at all — distinct from an officer choosing Reject.
    public bool AutoRejectedByValidation { get; set; }

    [MaxLength(2000)]
    public string? ValidationSummary { get; set; }

    // Populated by whichever LangGraph checkpoint routed this proposal
    // ("AgriculturalOfficerReview" or "SeniorOfficerReview" when escalated).
    [MaxLength(60)]
    public string? CheckpointName { get; set; }

    // ── Officer decision audit trail (FR: Approve / Reject / Request Revision) ──
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }

    [MaxLength(1000)]
    public string? OfficerNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
