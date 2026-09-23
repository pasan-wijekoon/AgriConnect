using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Models;

/// <summary>
/// Shared reference table for geographical regions used across all AgriConnect components.
///
/// Represents the collection-centre catchment areas and agricultural districts of Sri Lanka.
/// Component D references this table via <c>PriceTrendSnapshot.RegionId</c> and
/// <c>ShortageOversupplyEvent.RegionId</c>.
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

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
