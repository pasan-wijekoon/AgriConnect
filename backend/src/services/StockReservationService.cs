using System.Data;
using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgriConnect.Api.Services;

/// <summary>
/// Implements the algorithm from plan §7.2 exactly:
///
///   BEGIN TRANSACTION (Serializable)
///     active = SUM(ReservedQuantity) WHERE ListingId = @id AND ExpiresAt > now()
///                                       AND Order.Status != Cancelled
///     IF active + requested > available: ROLLBACK, return failure
///     INSERT StockReservation, INSERT Order
///   COMMIT
///   CATCH serialization_failure (40001): retry up to 3 times, 50/150/400ms backoff
///
/// The Order and its StockReservation are created atomically — either both persist
/// or neither does — so "placing an order" and "reserving stock" can never diverge.
/// </summary>
public class StockReservationService(AgriConnectDbContext db, IConfiguration configuration)
    : IStockReservationService
{
    private static readonly int[] RetryDelaysMs = [50, 150, 400];

    public async Task<ReservationResult> ReserveAndPlaceOrderAsync(
        Order order,
        decimal availableQuantity,
        CancellationToken cancellationToken = default)
    {
        var ttlMinutes = configuration.GetValue("Orders:ReservationTtlMinutes", 30);

        for (var attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                var now = DateTimeOffset.UtcNow;

                var activeReserved = await db.StockReservations
                    .Where(r => r.ListingId == order.ListingId && r.ExpiresAt > now)
                    .Join(db.Orders, r => r.OrderId, o => o.Id, (r, o) => new { r.ReservedQuantity, o.Status })
                    .Where(x => x.Status != OrderStatus.Cancelled)
                    .SumAsync(x => (decimal?)x.ReservedQuantity, cancellationToken) ?? 0m;

                if (activeReserved + order.Quantity > availableQuantity)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new ReservationResult(false, null,
                        $"Insufficient stock: {availableQuantity - activeReserved} available, {order.Quantity} requested.");
                }

                var reservation = new StockReservation
                {
                    Id = Guid.NewGuid(),
                    ListingId = order.ListingId,
                    OrderId = order.Id,
                    ReservedQuantity = order.Quantity,
                    ExpiresAt = now.AddMinutes(ttlMinutes)
                };

                db.Orders.Add(order);
                db.StockReservations.Add(reservation);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new ReservationResult(true, reservation, null);
            }
            catch (Exception ex) when (IsSerializationFailure(ex))
            {
                // When the serialization failure surfaces from CommitAsync itself
                // (rather than an earlier query/SaveChanges), Npgsql has already
                // torn the transaction down server-side — an explicit Rollback in
                // that case throws "transaction has completed", so it's swallowed
                // here rather than treated as a new failure.
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (InvalidOperationException)
                {
                }

                db.ChangeTracker.Clear();

                if (attempt == RetryDelaysMs.Length)
                {
                    return new ReservationResult(false, null,
                        "Could not complete reservation due to concurrent updates; please try again.");
                }

                await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
            }
        }

        // Unreachable: the loop above always returns on its final attempt.
        throw new InvalidOperationException("Reservation retry loop exited without a result.");
    }

    /// <summary>
    /// Walks the full InnerException chain rather than checking one specific
    /// wrapper shape: EF Core's default execution strategy wraps a serialization
    /// failure in an extra InvalidOperationException ("likely due to a transient
    /// failure") when it's detected inside a manually-managed transaction (which
    /// this method always runs in), on top of the DbUpdateException/PostgresException
    /// nesting SaveChanges already produces. Checking only one or two levels deep
    /// missed that case under real concurrent load.
    /// </summary>
    private static bool IsSerializationFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }
        }

        return false;
    }
}
