using System.Text.Json;
using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Models;

namespace AgriConnect.Api.Services;

/// <summary>
/// Writes to the shared AuditLog table (FR20, plan §12). Deliberately does not
/// call SaveChangesAsync itself — callers add an entry via <see cref="Log"/>
/// alongside the entity change it documents and save both together in their own
/// existing unit of work, so the audit trail is always atomic with the change it
/// records rather than a separate, independently-failable write.
/// </summary>
public class AuditLogService(AgriConnectDbContext db)
{
    /// <summary>Well-known actor id for events with no human actor behind them
    /// (e.g. the reservation expiry sweep). No User table exists yet to seed a
    /// real system account against, so this is a documented sentinel — see
    /// PROGRESS.md Decisions.</summary>
    public static readonly Guid SystemActorId = Guid.Empty;

    // Program.cs's global JsonStringEnumConverter only applies to MVC's
    // request/response (de)serialization, not this manual JsonSerializer.Serialize
    // call — without its own copy here, an enum in `details` (e.g. OrderStatus,
    // ScheduleDecision) would silently serialize as a raw integer, defeating the
    // point of a human-readable audit trail. A unit test caught exactly this.
    private static readonly JsonSerializerOptions DetailsJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public void Log(Guid actorId, string action, string entityType, Guid entityId, object? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = DateTimeOffset.UtcNow,
            Details = details is null ? null : JsonSerializer.Serialize(details, DetailsJsonOptions)
        });
    }
}
