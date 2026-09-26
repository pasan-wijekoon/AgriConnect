using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Listing
{
    public Guid Id { get; set; }

    // FK → User (Farmer). We store the ID directly since the User entity
    // is cross-cutting and will be added when auth is integrated.
    public Guid FarmerId { get; set; }

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;

    public Guid RegionId { get; set; }
    public Region Region { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    [Required, MaxLength(10)]
    public string Unit { get; set; } = "kg";

    [Required, MaxLength(5)]
    public string ClaimedGrade { get; set; } = string.Empty;

    public DateTime PickupWindowStart { get; set; }
    public DateTime PickupWindowEnd { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft";
    // Valid: Draft, PendingApproval, Published, Withdrawn, SoldOut

    [Column(TypeName = "decimal(12,2)")]
    public decimal? MinPrice { get; set; } // farmer's floor price (optional)

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<ListingPhoto> Photos { get; set; } = new();
    public PriceSuggestion? PriceSuggestion { get; set; }
}
