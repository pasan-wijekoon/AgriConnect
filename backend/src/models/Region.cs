using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Models;

/// <summary>
/// Shared reference table for geographical regions used across all AgriConnect components.
///
/// Represents the collection-centre catchment areas and agricultural districts of Sri Lanka.
/// Component A (Listings) references this table for its produce catalog; Component B
/// (Logistics) references it via <c>CollectionCentre.RegionId</c>; Component D references
/// it via <c>PriceTrendSnapshot.RegionId</c> and <c>ShortageOversupplyEvent.RegionId</c>.
///
/// Shared — consumed by Component A (Listings), Component B (Logistics), Component D (Analytics),
/// and the Agentic AI service.
/// </summary>
public class Region
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional link to a Component B CollectionCentre, when this region has one
    /// (no FK constraint yet — same no-FK-yet pattern as other cross-component references
    /// until the two tables are formally linked).</summary>
    public Guid? CollectionCentreId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
