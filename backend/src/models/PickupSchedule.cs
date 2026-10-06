namespace AgriConnect.Api.Models;

/// <summary>
/// A proposed or confirmed pickup/delivery slot for an order at a collection centre
/// (FR10). AI-proposed slots start life as <see cref="ScheduleStatus.Proposed"/> and
/// only become <see cref="ScheduleStatus.Confirmed"/> after explicit Officer approval
/// (FR19) — nothing in this component auto-confirms a proposal.
///
/// Component B — Order &amp; Collection-Centre Logistics.
/// </summary>
public class PickupSchedule
{
    public Guid Id { get; set; }

    /// <summary>1-1 with the order this slot is for (real FK — same component).</summary>
    public Guid OrderId { get; set; }

    public Order? Order { get; set; }

    /// <summary>FK to the collection centre the slot is booked at (real FK — same component).</summary>
    public Guid CollectionCentreId { get; set; }

    public CollectionCentre? CollectionCentre { get; set; }

    public DateTimeOffset SlotStart { get; set; }

    public DateTimeOffset SlotEnd { get; set; }

    public ScheduleStatus Status { get; set; } = ScheduleStatus.Proposed;
}
