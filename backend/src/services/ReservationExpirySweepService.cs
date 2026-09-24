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
                var cancelledCount = await SweepExpiredReservationsAsync(db, stoppingToken);
                if (cancelledCount > 0)
                {
                    logger.LogInformation(
                        "[ReservationExpirySweep] Cancelled {Count} orders with expired reservations.",
                        cancelledCount);
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
        AgriConnectDbContext db, CancellationToken cancellationToken = default)
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
        }

        if (expiredOrders.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return expiredOrders.Count;
    }
}
