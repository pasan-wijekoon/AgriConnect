using System;
using System.Collections.Generic;

namespace backend.src.dtos;

public class CreateInspectionRequest
{
    public Guid ListingId { get; set; }
    public string ConfirmedGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}

public class UpdateInspectionRequest
{
    public string ConfirmedGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public string ReasonForAmendment { get; set; } = string.Empty;
}

public class ResolveDiscrepancyRequest
{
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class PublishListingRequest
{
    public string? Notes { get; set; }
}

public class InspectionPhotoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class InspectionResponseDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public string CropName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string ClaimedGrade { get; set; } = string.Empty;
    public string ConfirmedGrade { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime InspectedAt { get; set; }
    public Guid OfficerId { get; set; }
    public string OfficerName { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string ListingStatus { get; set; } = string.Empty;
    public bool HasDiscrepancy { get; set; }
    public List<InspectionPhotoDto> Photos { get; set; } = new();
}

public class ListingSummaryDto
{
    public Guid Id { get; set; }
    public Guid FarmerId { get; set; }
    public string FarmerName { get; set; } = string.Empty;
    public string FarmerPhone { get; set; } = string.Empty;
    public Guid CropId { get; set; }
    public string CropName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string ClaimedGrade { get; set; } = string.Empty;
    public string? LatestConfirmedGrade { get; set; }
    public DateTime PickupWindowStart { get; set; }
    public DateTime PickupWindowEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public int InspectionCount { get; set; }
    public bool HasUnresolvedDiscrepancy { get; set; }
    public List<string> ListingPhotos { get; set; } = new();
}

public class GradeDiscrepancyDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public string CropName { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string ClaimedGrade { get; set; } = string.Empty;
    public string ConfirmedGrade { get; set; } = string.Empty;
    public DateTime FlaggedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ResolvedByOfficerName { get; set; }
    public bool IsResolved => ResolvedAt.HasValue;
}

public class InspectionFilterParams
{
    public string? Search { get; set; }
    public string? Grade { get; set; }
    public Guid? CropId { get; set; }
    public Guid? RegionId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class QualityDashboardStatsDto
{
    public int PendingInspections { get; set; }
    public int CompletedToday { get; set; }
    public int TotalInspections { get; set; }
    public int ActiveDiscrepancies { get; set; }
    public int PublishedListings { get; set; }
    public decimal GradeAComplianceRate { get; set; }
}
