using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriConnect.Api.Models;

/// <summary>
/// The quantity of a listing held against an order, with an expiry. Concurrency-safe
/// creation (serializable transaction, re-checks the sum of active reservations
/// against listing quantity) is what guarantees FR9's invariant that total reserved
/// quantity never exceeds available quantity — see StockReservationService.
///
/// Component B — Order &amp; Collection-Centre Logistics.
/// </summary>
public class StockReservation
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared Listing table (Component A). Not yet landed —
    /// mapped as an indexed Guid without an FK constraint until it exists.</summary>
    public Guid ListingId { get; set; }

    /// <summary>1-1 with the order that created this reservation (real FK — same component).</summary>
    public Guid OrderId { get; set; }

    public Order? Order { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal ReservedQuantity { get; set; }

    /// <summary>
    /// When this reservation stops counting against available stock. A background
    /// sweep (ReservationExpirySweepService) cancels the parent order once this
    /// passes while the order is still Pending — see plan §7.2 (Assumption: the DFD
    /// defines this column but does not state who acts on expiry).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
