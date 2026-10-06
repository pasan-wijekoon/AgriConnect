using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriConnect.Api.Models;

/// <summary>
/// A physical collection/drop-off point produce can be scheduled through (FR10, FR21).
///
/// Component B — Order &amp; Collection-Centre Logistics.
/// </summary>
public class CollectionCentre
{
    public Guid Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "numeric(9,6)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "numeric(9,6)")]
    public decimal Longitude { get; set; }

    /// <summary>
    /// Max concurrent overlapping Confirmed bookings (plan §8.3 default — the DFD
    /// does not state a time unit for this column; Needs confirmation).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    /// <summary>FK to the shared Region table (Component A). Not yet landed —
    /// mapped as an indexed Guid without an FK constraint until it exists.</summary>
    public Guid RegionId { get; set; }

    public ICollection<PickupSchedule> PickupSchedules { get; set; } = new List<PickupSchedule>();
}
