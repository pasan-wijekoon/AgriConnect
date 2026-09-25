namespace AgriConnect.Api.Models;

/// <summary>
/// Cross-cutting in-app notification (FR22, DFD §6.3). Shared across every
/// component; Component B is the first to need it (plan §12), so this table and
/// its DbContext block are the minimal shared shape, not a Component-B-owned
/// entity. The dedicated "Notification Service (WS/Push)" the DFD's architecture
/// diagram shows is a separate real-time delivery concern this table does not
/// attempt to build — this is just the persisted record a client polls.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared User table (shared auth). Not yet landed —
    /// mapped as an indexed Guid without an FK constraint until it exists.</summary>
    public Guid UserId { get; set; }

    /// <summary>Free-text notification type, e.g. "OrderPlaced", "ScheduleProposed".</summary>
    public string Type { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
