using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// FR9 — background sweep that cancels Pending orders whose StockReservation has
/// expired, so an abandoned reservation stops occupying a slot in the queue.
///
/// The DFD defines StockReservation.ExpiresAt but never states who acts on it once
/// it passes (plan §7.2 / PROGRESS.md open question #6); this is the documented
/// assumption made in the plan. Stock itself is already freed the instant a
/// reservation's ExpiresAt lapses — StockReservationService's active-reservation
/// sum filters on `ExpiresAt > now`, same as it filters out Cancelled orders — so
/// this sweep doesn't "release" anything; it just moves the abandoned Order into
/// the correct terminal status instead of leaving it stuck in Pending forever.
/// </summary>
public class ReservationExpirySweepService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ReservationExpirySweepService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue("Orders:ExpirySweepIntervalMinutes", 5);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
                var auditLog = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var cancelledCount = await SweepExpiredReservationsAsync(db, auditLog, notifications, stoppingToken);
                if (cancelledCount > 0)
                {
                    logger.LogInformation(
                        "[ReservationExpirySweep] Cancelled {Count} orders with expired reservations.",
                        cancelledCount);
                }

                var reminders = await SendRemindersAsync(
                    db, notifications,
                    buyerLeadTime: TimeSpan.FromHours(configuration.GetValue("Orders:ReservationReminderHours", 6d)),
                    officerPendingAfter: TimeSpan.FromHours(configuration.GetValue("Orders:OfficerPendingReminderHours", 24d)),
                    stoppingToken);
                if (reminders > 0)
                {
                    logger.LogInformation("[ReservationExpirySweep] Sent {Count} reminders.", reminders);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[ReservationExpirySweep] Sweep iteration failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    /// <summary>
    /// Cancels every Pending order whose reservation has expired. Extracted as a
    /// standalone static method — taking the DbContext directly rather than going
    /// through the hosted-service machinery — so it's testable without waiting on
    /// a background timer or standing up a scope factory.
    /// </summary>
    public static async Task<int> SweepExpiredReservationsAsync(
        AgriConnectDbContext db,
        AuditLogService auditLog,
        NotificationService notifications,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var expiredOrders = await db.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .Join(db.StockReservations, o => o.Id, r => r.OrderId, (o, r) => new { Order = o, r.ExpiresAt })
            .Where(x => x.ExpiresAt <= now)
            .Select(x => x.Order)
            .ToListAsync(cancellationToken);

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = now;

            // FR20/plan §12 explicitly calls out expiry as "exactly the kind of
            // event an audit trail exists for" — no human actor triggered this,
            // so it's logged against AuditLogService.SystemActorId. Only the
            // buyer is notified (not the farmer, unlike the manual-cancel path)
            // to keep this background process from taking on a second lookup
            // dependency (IListingAvailabilityPort) just for this one case.
            auditLog.Log(AuditLogService.SystemActorId, "OrderCancelled", "Order", order.Id,
                new { Role = "System", Reason = "Reservation expired" });
            notifications.Notify(order.BuyerId, "OrderStatusChanged",
                "Your order was cancelled because its stock reservation expired.");
        }

        if (expiredOrders.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return expiredOrders.Count;
    }

    /// <summary>
    /// Reminders for orders still waiting for approval (FR22), each sent at most once:
    /// the Buyer is warned when the reservation is about to expire, and the centre's
    /// Officers are reminded when an order has been Pending for a long time. Sent-at
    /// timestamps on the reservation prevent repeats on the next sweep. Static and
    /// taking the DbContext, like <see cref="SweepExpiredReservationsAsync"/>, so it is
    /// testable without the background timer. Returns the number of notifications queued.
    /// </summary>
    public static async Task<int> SendRemindersAsync(
        AgriConnectDbContext db,
        NotificationService notifications,
        TimeSpan buyerLeadTime,
        TimeSpan officerPendingAfter,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var sent = 0;

        // 1) Buyer: the reservation is still live but ends soon.
        var expiringSoon = await db.StockReservations
            .Include(r => r.Order)
            .Where(r => r.Order!.Status == OrderStatus.Pending
                        && r.ExpiresAt > now
                        && r.ExpiresAt <= now + buyerLeadTime
                        && r.BuyerReminderSentAt == null)
            .ToListAsync(cancellationToken);

        // 2) Officers: Pending for a long time, reservation still live.
        var waitingLong = await db.StockReservations
            .Include(r => r.Order)
            .Where(r => r.Order!.Status == OrderStatus.Pending
                        && r.ExpiresAt > now
                        && r.Order.CreatedAt <= now - officerPendingAfter
                        && r.OfficerReminderSentAt == null)
            .ToListAsync(cancellationToken);

        if (expiringSoon.Count == 0 && waitingLong.Count == 0)
        {
            return 0;
        }

        var listingIds = expiringSoon.Concat(waitingLong).Select(r => r.ListingId).Distinct().ToList();
        var listings = await db.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .Select(l => new { l.Id, l.RegionId, l.Unit, CropName = l.Crop.Name })
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        string Describe(StockReservation r)
        {
            var quantity = r.Order!.Quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return listings.TryGetValue(r.ListingId, out var l)
                ? $"{quantity} {l.Unit} of {l.CropName}"
                : $"{quantity} of produce";
        }

        foreach (var reservation in expiringSoon)
        {
            notifications.Notify(
                reservation.Order!.BuyerId,
                "ReservationExpiring",
                $"Your order for {Describe(reservation)} has not been approved yet. Its stock reservation ends at {reservation.ExpiresAt.UtcDateTime:d MMM, HH:mm} UTC, after which the order is cancelled.",
                title: "Reservation ending soon");
            reservation.BuyerReminderSentAt = now;
            sent++;
        }

        foreach (var reservation in waitingLong)
        {
            var regionId = listings.TryGetValue(reservation.ListingId, out var l) ? l.RegionId : (Guid?)null;
            var centreIds = regionId is null
                ? new List<Guid>()
                : await db.CollectionCentres.AsNoTracking()
                    .Where(c => c.RegionId == regionId.Value).Select(c => c.Id).ToListAsync(cancellationToken);

            var officerIds = await db.Users.AsNoTracking()
                .Where(u => u.Role == Roles.Officer && u.IsActive
                            && u.CollectionCentreId != null && centreIds.Contains(u.CollectionCentreId.Value))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var officerId in officerIds)
            {
                notifications.Notify(
                    officerId,
                    "OrderAwaitingApproval",
                    $"Order #{reservation.OrderId.ToString()[..8]} ({Describe(reservation)}) has been waiting for approval for over {officerPendingAfter.TotalHours:0.#} hours.",
                    title: "Order waiting for approval");
                sent++;
            }

            // Marked even when no officer is bound to the centre, so it is not re-evaluated every sweep.
            reservation.OfficerReminderSentAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return sent;
    }
}
