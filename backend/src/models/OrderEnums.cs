namespace AgriConnect.Api.Models;

/// <summary>
/// Lifecycle status of an <see cref="Order"/>. Persisted as a string to match the
/// DB CHECK constraint on <c>Order.Status</c> (DFD 6.3, FR11).
/// </summary>
public enum OrderStatus
{
    Pending,
    Approved,
    Scheduled,
    Completed,
    Cancelled
}

/// <summary>
/// How the buyer wants to receive the produce. Persisted as a string to match the
/// DB CHECK constraint on <c>Order.DeliveryPreference</c>.
/// </summary>
public enum DeliveryPreference
{
    Pickup,
    Delivery
}

/// <summary>
/// Review state of a proposed <see cref="PickupSchedule"/> slot (FR10/FR19 — an AI
/// proposal is never final until an Officer confirms it). Persisted as a string to
/// match the DB CHECK constraint on <c>PickupSchedule.Status</c>.
/// </summary>
public enum ScheduleStatus
{
    Proposed,
    Confirmed,
    Cancelled
}
