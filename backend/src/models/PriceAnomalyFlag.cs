using System.ComponentModel.DataAnnotations.Schema;

namespace AgriConnect.Api.Models;

/// <summary>
/// Raised when a listing's asking price sits significantly outside the AI-suggested
/// fair-price range produced by Component A. Officers triage these from the review
/// queue; the investigate endpoint explains the likely cause.
///
/// Component D — FR16 (flag listings deviating from the AI-suggested fair price).
/// </summary>
public class PriceAnomalyFlag
{
    public Guid Id { get; set; }

    /// <summary>FK to Component A's Listing.</summary>
    public Guid ListingId { get; set; }

    /// <summary>
    /// Signed percentage distance from the midpoint of the AI-suggested range.
    /// Positive means the listing is priced above the suggestion, negative below.
    /// </summary>
    [Column(TypeName = "numeric(5,2)")]
    public decimal DeviationPercent { get; set; }

    public DateTimeOffset FlaggedAt { get; set; }

    public AnomalyStatus Status { get; set; } = AnomalyStatus.Open;
}
