using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriConnect.Api.Models;

/// <summary>
/// A record of a summary report an administrator generated, kept so past exports stay
/// retrievable and the export activity remains auditable.
///
/// Component D — FR18 (administrators export summary reports of listings, orders,
/// and price trends).
/// </summary>
public class ReportExport
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared User table — the administrator who requested it.</summary>
    public Guid RequestedBy { get; set; }

    /// <summary>What the report covers, e.g. "Listings", "Orders", "PriceTrends".</summary>
    [MaxLength(30)]
    public string Type { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateOnly DateRangeStart { get; set; }

    [Column(TypeName = "date")]
    public DateOnly DateRangeEnd { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Where the generated file can be downloaded from.</summary>
    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;
}
