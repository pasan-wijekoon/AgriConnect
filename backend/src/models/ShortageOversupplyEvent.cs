
namespace AgriConnect.Api.Models;

/// <summary>
/// A recurring supply imbalance detected for a crop in a region — either not enough
/// produce reaching the market, or a glut of it.
///
/// Component D — FR17 (detect and surface recurring shortage or oversupply patterns).
/// </summary>
public class ShortageOversupplyEvent
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared Crop reference table.</summary>
    public Guid CropId { get; set; }

    /// <summary>FK to the shared Region reference table.</summary>
    public Guid RegionId { get; set; }

    public SupplyEventType Type { get; set; }

    public DateTimeOffset DetectedAt { get; set; }

    public SupplyEventSeverity Severity { get; set; }

    /// <summary>
    /// Free-text context for the officer reading the dashboard — e.g. which periods
    /// were below baseline and by how much.
    /// </summary>
    public string? Notes { get; set; }
}
