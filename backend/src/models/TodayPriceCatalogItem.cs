using System.ComponentModel.DataAnnotations;

namespace backend.Models;

/// <summary>
/// One row on the "Today's Prices" marketplace discovery page. Admin-managed
/// (add/edit/remove) — this table is the source of truth for which crops
/// appear there; the actual price shown per item is still computed live by
/// the Fair-Price Estimation Agent, never stored here.
/// </summary>
public class TodayPriceCatalogItem
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string Unit { get; set; } = "kg";

    [Required, MaxLength(100)]
    public string DefaultRegion { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
