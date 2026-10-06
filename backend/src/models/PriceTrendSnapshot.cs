using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriConnect.Api.Models;

/// <summary>
/// A materialised price aggregate for one crop, in one region, for one period bucket.
/// Intentionally denormalised: it exists purely for read-heavy analytics and is
/// rebuilt on a schedule rather than written to transactionally (DFD 6.1).
///
/// Component D — FR15 (historical price trends per crop and region).
/// </summary>
public class PriceTrendSnapshot
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared Crop reference table (Component A / shared).</summary>
    public Guid CropId { get; set; }

    /// <summary>FK to the shared Region reference table (Component A / shared).</summary>
    public Guid RegionId { get; set; }

    /// <summary>
    /// Start of the aggregation bucket, e.g. the Monday of the week being summarised.
    /// Unique together with <see cref="CropId"/> and <see cref="RegionId"/>.
    /// </summary>
    [Column(TypeName = "date")]
    public DateOnly Period { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal AvgPrice { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal MinPrice { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal MaxPrice { get; set; }

    /// <summary>
    /// How many listings fed this bucket. A low sample count means the average is
    /// weakly supported, so the API surfaces it rather than hiding it.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int SampleCount { get; set; }
}
