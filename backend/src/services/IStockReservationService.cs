using AgriConnect.Api.Models;

namespace AgriConnect.Api.Services;

public record ReservationResult(bool Success, StockReservation? Reservation, string? FailureReason);

/// <summary>
/// FR9 — concurrency-safe stock reservation. Guarantees
/// SUM(active StockReservation.ReservedQuantity) never exceeds the listing's
/// available quantity, even under concurrent requests for the same listing
/// (plan §7.2). This is the critical path CLAUDE.md §15 calls out explicitly:
/// no naive read-check-write sequence.
/// </summary>
public interface IStockReservationService
{
    /// <summary>
    /// Atomically inserts <paramref name="order"/> and a matching
    /// <see cref="StockReservation"/> for it, inside a single serializable
    /// transaction that re-checks the sum of active reservations for
    /// <c>order.ListingId</c> against <paramref name="availableQuantity"/> before
    /// commit. Retries on Postgres serialization failure (SQLSTATE 40001).
    /// </summary>
    Task<ReservationResult> ReserveAndPlaceOrderAsync(
        Order order,
        decimal availableQuantity,
        CancellationToken cancellationToken = default);
}
