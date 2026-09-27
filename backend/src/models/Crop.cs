using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Models;

/// <summary>
/// Shared reference table for crop types used across all AgriConnect components.
///
/// Provides the authoritative list of produce that can be listed, priced, and analysed.
/// Component A (Listings) references this table for its produce catalog; Component D
/// references it via <c>PriceTrendSnapshot.CropId</c> and
/// <c>ShortageOversupplyEvent.CropId</c>.
///
/// Shared — consumed by Component A (Listings), Component D (Analytics), and the Agentic AI service.
/// </summary>
public class Crop
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>E.g. "Vegetables", "Fruits", "Grains" — Component A's own field, used to
    /// group the produce catalog in the marketplace UI.</summary>
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
