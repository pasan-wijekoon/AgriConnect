namespace AgriConnect.Api.Models;

/// <summary>
/// Cross-cutting audit trail (FR20, DFD §6.3) — who did what to which entity, and
/// when. Shared across every component; Component B is the first to need it
/// (plan §12), so this table and its DbContext block are the minimal shared shape,
/// not a Component-B-owned entity. Any component may write its own <see cref="Action"/>
/// values here, so there is deliberately no CHECK constraint enumerating them.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>FK to the shared User table (shared auth). Not yet landed —
    /// mapped as an indexed Guid without an FK constraint until it exists.
    /// A well-known all-zero Guid represents a system/automated actor (e.g. the
    /// reservation expiry sweep) that has no human user behind it.</summary>
    public Guid ActorId { get; set; }

    /// <summary>Free-text action name, e.g. "OrderCreated", "OrderStatusChanged".</summary>
    public string Action { get; set; } = "";

    /// <summary>The entity type this action was performed on, e.g. "Order".</summary>
    public string EntityType { get; set; } = "";

    public Guid EntityId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Serialized JSON with action-specific context (e.g. {"from":"Pending","to":"Approved"}).</summary>
    public string? Details { get; set; }
}
