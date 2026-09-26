using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

// ── Request DTOs ──────────────────────────────────────────────

public class CreateListingDto
{
    [Required]
    public Guid CropId { get; set; }

    [Required]
    public Guid RegionId { get; set; }

    [Required, Range(0.01, 999999.99)]
    public decimal Quantity { get; set; }

    [MaxLength(10)]
    public string Unit { get; set; } = "kg";

    [Required, MaxLength(5)]
    public string ClaimedGrade { get; set; } = string.Empty;

    [Required]
    public DateTime PickupWindowStart { get; set; }

    [Required]
    public DateTime PickupWindowEnd { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? MinPrice { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Photo URLs (at least one required per FR3)
    [Required, MinLength(1)]
    public List<string> PhotoUrls { get; set; } = new();
}

public class UpdateListingDto
{
    public Guid? CropId { get; set; }
    public Guid? RegionId { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? Quantity { get; set; }

    [MaxLength(10)]
    public string? Unit { get; set; }

    [MaxLength(5)]
    public string? ClaimedGrade { get; set; }

    public DateTime? PickupWindowStart { get; set; }
    public DateTime? PickupWindowEnd { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? MinPrice { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Null = leave photos unchanged. Non-null = replace the listing's full
    // photo set with exactly this list (the edit form always sends the
    // farmer's current photo set, whether they changed it or not).
    public List<string>? PhotoUrls { get; set; }
}

public class AddPhotosDto
{
    [Required, MinLength(1)]
    public List<string> PhotoUrls { get; set; } = new();
}

public class DecidePriceSuggestionDto
{
    [MaxLength(1000)]
    public string? OfficerNote { get; set; }
}

public class RevisePriceSuggestionDto
{
    [Required, Range(0.01, 999999.99)]
    public decimal RevisedPriceMin { get; set; }

    [Required, Range(0.01, 999999.99)]
    public decimal RevisedPriceMax { get; set; }

    [MaxLength(1000)]
    public string? OfficerNote { get; set; }
}

// ── Query DTO ─────────────────────────────────────────────────

public class ListingSearchQuery
{
    public Guid? CropId { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ExcludeFarmerId { get; set; }   // Exclude this farmer's own listings from results
    public string? Status { get; set; }          // Published, Withdrawn, SoldOut
    public string? Grade { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Search { get; set; }          // keyword search on crop name
    public string SortBy { get; set; } = "date"; // date, price, quantity
    public string SortDir { get; set; } = "desc"; // asc, desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ── Response DTOs ─────────────────────────────────────────────

public class ListingResponseDto
{
    public Guid Id { get; set; }
    public Guid FarmerId { get; set; }
    public string CropName { get; set; } = string.Empty;
    public string CropCategory { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ClaimedGrade { get; set; } = string.Empty;
    public DateTime PickupWindowStart { get; set; }
    public DateTime PickupWindowEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PhotoDto> Photos { get; set; } = new();
    public PriceSuggestionDto? PriceSuggestion { get; set; }
}

public class PhotoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class PriceSuggestionDto
{
    public Guid Id { get; set; }
    public decimal SuggestedPriceMin { get; set; }
    public decimal SuggestedPriceMax { get; set; }
    public decimal Confidence { get; set; }
    public string ReasoningSummary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CheckpointName { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? OfficerNote { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class CropDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class RegionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
