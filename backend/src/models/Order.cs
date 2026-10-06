using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgriConnect.Api.Models;

/// <summary>
/// A buyer's order against a published listing: quantity requested, delivery
/// preference, and lifecycle status (FR8, FR11).
///
/// Component B — Order &amp; Collection-Centre Logistics.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared Listing table (Component A). Not yet landed —
    /// mapped as an indexed Guid without an FK constraint until it exists.</summary>
    public Guid ListingId { get; set; }

    /// <summary>FK to the shared User table (shared auth). Not yet landed —
    /// mapped as an indexed Guid without an FK constraint until it exists.</summary>
    public Guid BuyerId { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal Quantity { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DeliveryPreference DeliveryPreference { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Approximate buyer location (rounded to 2 decimal places, about 1 km), optionally
    /// supplied when ordering so the Buyer-Farmer Matching Agent can suggest the nearest
    /// collection centre when the order is approved. Never returned by the API.</summary>
    [Column(TypeName = "numeric(5,2)")]
    public decimal? BuyerLatitude { get; set; }

    [Column(TypeName = "numeric(5,2)")]
    public decimal? BuyerLongitude { get; set; }

    /// <summary>1-1 with the reservation created when the order is placed (FR9).</summary>
    public StockReservation? StockReservation { get; set; }

    /// <summary>1-1 with the pickup/delivery slot once one has been proposed (FR10).</summary>
    public PickupSchedule? PickupSchedule { get; set; }
}
