using System;
using System.Collections.Generic;

namespace backend.src.models;

public static class ListingStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Published = "Published";
    public const string Withdrawn = "Withdrawn";
    public const string SoldOut = "SoldOut";
}

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FarmerId { get; set; }
    public Guid CropId { get; set; }
    public Guid RegionId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string ClaimedGrade { get; set; } = "Grade A";
    public DateTime PickupWindowStart { get; set; }
    public DateTime PickupWindowEnd { get; set; }
    public string Status { get; set; } = ListingStatus.PendingApproval;
    public decimal? MinPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Farmer { get; set; }
    public Crop? Crop { get; set; }
    public Region? Region { get; set; }
    public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    public ICollection<GradeDiscrepancyFlag> GradeDiscrepancyFlags { get; set; } = new List<GradeDiscrepancyFlag>();
}

public class ListingPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Listing? Listing { get; set; }
}
