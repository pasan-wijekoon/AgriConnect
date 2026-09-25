using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// Writes to and reads from the shared Notification table (FR22, plan §12).
/// <see cref="Notify"/> mirrors <see cref="AuditLogService.Log"/>'s convention —
/// no SaveChangesAsync of its own, so a notification is always persisted
/// atomically with the state change that triggered it, in the caller's existing
/// unit of work.
/// </summary>
public class NotificationService(AgriConnectDbContext db)
{
    public void Notify(Guid userId, string type, string message)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task<IReadOnlyList<NotificationResponse>> ListForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100) // capped, matching the rest of this API's list-size convention
            .AsNoTracking()
            .ToListAsync(ct);

        return notifications.Select(ToResponse).ToList();
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, ct);

        // Ownership check, not just existence — a user must not be able to mark
        // (or discover the existence of) another user's notification (IDOR
        // prevention, plan §6/CLAUDE.md §20).
        if (notification is null || notification.UserId != userId)
        {
            return false;
        }

        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static NotificationResponse ToResponse(Notification n) =>
        new(n.Id, n.Type, n.Message, n.ReadAt, n.CreatedAt);
}
