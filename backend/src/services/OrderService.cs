using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

public enum OrderOperationError
{
    None,
    NotFound,
    Forbidden,
    InvalidRequest,
    Conflict
}

public class OrderOperationResult<T>
{
    public bool Success { get; private init; }
    public T? Value { get; private init; }
    public OrderOperationError Error { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static OrderOperationResult<T> Ok(T value) => new() { Success = true, Value = value };

    public static OrderOperationResult<T> Fail(OrderOperationError error, string message) =>
        new() { Success = false, Error = error, ErrorMessage = message };
}

/// <summary>
/// Order lifecycle (FR8, FR11): creation (delegating the concurrency-critical
/// reservation step to <see cref="IStockReservationService"/>), retrieval/listing
/// with role-scoped visibility, controlled status transitions, and cancellation.
///
/// Component B — Order &amp; Collection-Centre Logistics.
/// </summary>
public class OrderService(
    AgriConnectDbContext db,
    IListingAvailabilityPort listingPort,
    IStockReservationService reservationService,
    AuditLogService auditLog,
    NotificationService notifications)
{
    // Explicit allow-list (plan §5.3/CLAUDE.md §16) — anything not listed here is
    // rejected with 400, never silently accepted.
    //
    // Approved -> Scheduled is intentionally NOT here: that transition happens when
    // a PickupSchedule is Confirmed (SchedulingService, Phase 6/plan §5.3), not via
    // an officer's direct status PUT.
    private static readonly HashSet<(OrderStatus From, OrderStatus To)> AllowedTransitions =
    [
        (OrderStatus.Pending, OrderStatus.Approved),
        (OrderStatus.Pending, OrderStatus.Cancelled),
        (OrderStatus.Approved, OrderStatus.Cancelled),
        (OrderStatus.Scheduled, OrderStatus.Completed),
        (OrderStatus.Scheduled, OrderStatus.Cancelled)
    ];

    public async Task<OrderOperationResult<OrderResponse>> PlaceOrderAsync(
        Guid buyerId, CreateOrderRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.InvalidRequest, "Quantity must be greater than zero.");
        }

        var availability = await listingPort.GetAvailabilityAsync(request.ListingId, ct);
        if (availability is null)
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.NotFound, "Listing not found.");
        }

        if (availability.Status != "Published")
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.InvalidRequest, "Listing is not published.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ListingId = request.ListingId,
            BuyerId = buyerId,
            Quantity = request.Quantity,
            Status = OrderStatus.Pending,
            DeliveryPreference = request.DeliveryPreference,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var reservation = await reservationService.ReserveAndPlaceOrderAsync(
            order, availability.AvailableQuantity, ct);

        if (!reservation.Success)
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.Conflict, reservation.FailureReason ?? "Insufficient stock.");
        }

        // A separate SaveChangesAsync from StockReservationService's own commit
        // (plan §12/CLAUDE.md §28: deliberately not touching that service's
        // serializable-transaction code, which has hard-won concurrency-bug
        // fixes and load tests behind it, just to make this write atomic with
        // it — see PROGRESS.md Decisions).
        auditLog.Log(buyerId, "OrderCreated", "Order", order.Id,
            new { order.ListingId, order.Quantity, order.DeliveryPreference });
        notifications.Notify(buyerId, "OrderPlaced",
            $"Your order for {order.Quantity} has been placed and is awaiting approval.");
        await db.SaveChangesAsync(ct);

        return OrderOperationResult<OrderResponse>.Ok(ToResponse(order, reservation.Reservation));
    }

    public async Task<OrderOperationResult<OrderResponse>> GetByIdAsync(
        Guid orderId, Guid currentUserId, string role, CancellationToken ct = default)
    {
        var order = await db.Orders
            .Include(o => o.StockReservation)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null || !await CanViewAsync(order, currentUserId, role, ct))
        {
            // 404, not 403 — do not reveal that another user's order exists (IDOR
            // prevention, plan §6/CLAUDE.md §20).
            return OrderOperationResult<OrderResponse>.Fail(OrderOperationError.NotFound, "Order not found.");
        }

        return OrderOperationResult<OrderResponse>.Ok(ToResponse(order, order.StockReservation));
    }

    public async Task<PagedResult<OrderResponse>> ListAsync(
        Guid currentUserId, string role, OrderStatus? status, int page, int size, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100); // capped at 100 — Component D's convention

        var query = db.Orders.Include(o => o.StockReservation).AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (role == Roles.Buyer)
        {
            query = query.Where(o => o.BuyerId == currentUserId);
        }
        else if (role == Roles.Farmer)
        {
            // Fixture-scale filter: resolves ownership per order via the availability
            // port since it doesn't yet support "listings by farmer" as a query.
            // Component A's real Listing table will support a direct SQL join —
            // replace this in-memory filter once that lands (plan §3).
            var candidates = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
            var owned = new List<Order>();
            foreach (var order in candidates)
            {
                var availability = await listingPort.GetAvailabilityAsync(order.ListingId, ct);
                if (availability is not null && availability.FarmerId == currentUserId)
                {
                    owned.Add(order);
                }
            }

            var pageItems = owned.Skip((page - 1) * size).Take(size).Select(o => ToResponse(o, o.StockReservation)).ToList();
            return new PagedResult<OrderResponse>(pageItems, page, size, owned.Count);
        }
        // Officer: no additional filter — centre-scoping is a documented open
        // question (no Officer<->CollectionCentre column exists yet; plan §16 #3).

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<OrderResponse>(
            items.Select(o => ToResponse(o, o.StockReservation)).ToList(), page, size, total);
    }

    public async Task<OrderOperationResult<OrderResponse>> UpdateStatusAsync(
        Guid orderId, OrderStatus newStatus, Guid actorId, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.StockReservation).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return OrderOperationResult<OrderResponse>.Fail(OrderOperationError.NotFound, "Order not found.");
        }

        var previousStatus = order.Status;
        if (!AllowedTransitions.Contains((order.Status, newStatus)))
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.InvalidRequest,
                $"Cannot transition an order from {order.Status} to {newStatus}.");
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        auditLog.Log(actorId, "OrderStatusChanged", "Order", order.Id,
            new { From = previousStatus, To = newStatus });
        await NotifyOrderStakeholdersAsync(order, $"Your order status changed to {newStatus}.", ct);

        await db.SaveChangesAsync(ct);

        return OrderOperationResult<OrderResponse>.Ok(ToResponse(order, order.StockReservation));
    }

    public async Task<OrderOperationResult<OrderResponse>> CancelAsync(
        Guid orderId, Guid currentUserId, string role, string? reason, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.StockReservation).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return OrderOperationResult<OrderResponse>.Fail(OrderOperationError.NotFound, "Order not found.");
        }

        if (role == Roles.Buyer)
        {
            if (order.BuyerId != currentUserId)
            {
                // 404, not 403 — IDOR prevention (plan §6).
                return OrderOperationResult<OrderResponse>.Fail(OrderOperationError.NotFound, "Order not found.");
            }

            // Assumption (plan §5.3/§12, open question #6): a buyer may cancel only
            // before the order is Scheduled.
            if (order.Status is OrderStatus.Scheduled or OrderStatus.Completed or OrderStatus.Cancelled)
            {
                return OrderOperationResult<OrderResponse>.Fail(
                    OrderOperationError.Conflict,
                    $"Buyers cannot cancel an order once it is {order.Status}.");
            }
        }
        else if (role == Roles.Officer)
        {
            if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
            {
                return OrderOperationResult<OrderResponse>.Fail(
                    OrderOperationError.Conflict, $"Order is already {order.Status}.");
            }
        }
        else
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.Forbidden, "Only the order's buyer or an officer can cancel it.");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        // Reason is now actually persisted (was accepted-but-dropped before audit
        // logging existed — see PROGRESS.md Known Issues/Decisions).
        auditLog.Log(currentUserId, "OrderCancelled", "Order", order.Id, new { Role = role, Reason = reason });
        await NotifyOrderStakeholdersAsync(order, "Your order was cancelled.", ct);

        await db.SaveChangesAsync(ct);

        // No separate reservation-release write is needed: the active-reservation
        // sum in StockReservationService already filters out Cancelled orders, so
        // the reservation stops counting the instant this commits (plan §7.2).
        return OrderOperationResult<OrderResponse>.Ok(ToResponse(order, order.StockReservation));
    }

    /// <summary>
    /// Notifies the buyer, plus the farmer if the listing's ownership is known
    /// (plan §12 — both track order status per FR11). The listing lookup is
    /// best-effort: a notification not being sent should never block the order
    /// operation it accompanies.
    /// </summary>
    private async Task NotifyOrderStakeholdersAsync(Order order, string message, CancellationToken ct)
    {
        notifications.Notify(order.BuyerId, "OrderStatusChanged", message);

        var listing = await listingPort.GetAvailabilityAsync(order.ListingId, ct);
        if (listing is not null)
        {
            notifications.Notify(listing.FarmerId, "OrderStatusChanged", message);
        }
    }

    private async Task<bool> CanViewAsync(Order order, Guid currentUserId, string role, CancellationToken ct)
    {
        if (role == Roles.Officer)
        {
            // No Officer<->CollectionCentre binding exists yet — open question,
            // plan §16 #3. Until it lands, any Officer may view any order.
            return true;
        }

        if (role == Roles.Buyer)
        {
            return order.BuyerId == currentUserId;
        }

        if (role == Roles.Farmer)
        {
            var availability = await listingPort.GetAvailabilityAsync(order.ListingId, ct);
            return availability is not null && availability.FarmerId == currentUserId;
        }

        return false;
    }

    private static OrderResponse ToResponse(Order order, StockReservation? reservation) => new(
        order.Id,
        order.ListingId,
        order.BuyerId,
        order.Quantity,
        order.Status,
        order.DeliveryPreference,
        order.CreatedAt,
        order.UpdatedAt,
        reservation?.ExpiresAt);
}
