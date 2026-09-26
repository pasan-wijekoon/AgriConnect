using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class EstimatePriceRequestDto
{
    public string CropId { get; set; } = string.Empty;
    public string RegionId { get; set; } = string.Empty;
    public string? CropName { get; set; }
    public string? RegionName { get; set; }
    public decimal Quantity { get; set; } = 100m;
    public string ClaimedGrade { get; set; } = "A";
}

public class PriceEstimateResultDto
{
    public string Crop { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Grade { get; set; } = "A";
    public decimal SuggestedPriceMin { get; set; }
    public decimal SuggestedPriceMax { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Confidence { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public decimal BenchmarkWholesale { get; set; }
    public decimal Change24h { get; set; }
    public string Trend { get; set; } = "stable";
}

public class TodayPriceItemDto
{
    public string CropId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public string Region { get; set; } = string.Empty;
    public string Grade { get; set; } = "A";
    public decimal SuggestedPriceMin { get; set; }
    public decimal SuggestedPriceMax { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Confidence { get; set; }
    public decimal Change24h { get; set; }
    public string Trend { get; set; } = "stable"; // rising, falling, stable
    public string ImageUrl { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public decimal BenchmarkWholesale { get; set; }
}

public class TodayPricesResponseDto
{
    public string Date { get; set; } = "Today";
    public int TotalCrops { get; set; }
    public string SelectedGrade { get; set; } = "A";
    public string SelectedRegion { get; set; } = "All Regions";
    public string MarketStatus { get; set; } = "Active Trading";
    public List<TodayPriceItemDto> Items { get; set; } = new();
}

// ── Today's Prices Catalog (Admin-managed) ──────────────────────

public class TodayPriceCatalogItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public string DefaultRegion { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTodayPriceCatalogItemDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Unit { get; set; } = "kg";

    [Required, MaxLength(100)]
    public string DefaultRegion { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }
}

public class UpdateTodayPriceCatalogItemDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(10)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? DefaultRegion { get; set; }

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class OrchestrationRequestDto
{
    [Required]
    public string ObjectiveText { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "NewListingSubmitted";
    public string? TriggerEntityId { get; set; }
    public object ListingContext { get; set; } = new();
}

/// <summary>
/// Parsed result of the LangGraph coordinator's `/api/orchestration/run` call —
/// the outcome of the full planner -> fair-price-agent -> validator ->
/// (optional anomaly investigation) -> human-approval-checkpoint pipeline.
/// </summary>
public class OrchestrationResultDto
{
    public string ApprovalStatus { get; set; } = "PendingOfficerApproval";
    public decimal SuggestedPriceMin { get; set; }
    public decimal SuggestedPriceMax { get; set; }
    public decimal Confidence { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public bool ValidationPassed { get; set; }
    public string? ValidationSummary { get; set; }
    public string? CheckpointName { get; set; }
}
