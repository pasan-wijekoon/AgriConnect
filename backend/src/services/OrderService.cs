using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    NotificationService notifications,
    SchedulingService? schedulingService = null,
    ILogger<OrderService>? logger = null)
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

        if (request.BuyerLat.HasValue != request.BuyerLng.HasValue
            || request.BuyerLat is < -90 or > 90
            || request.BuyerLng is < -180 or > 180)
        {
            return OrderOperationResult<OrderResponse>.Fail(
                OrderOperationError.InvalidRequest,
                "Buyer location must include both a latitude (-90 to 90) and a longitude (-180 to 180), or neither.");
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
            // About 1 km is plenty to pick the nearest centre and keeps the stored location coarse.
            BuyerLatitude = request.BuyerLat is { } lat ? Math.Round(lat, 2, MidpointRounding.AwayFromZero) : null,
            BuyerLongitude = request.BuyerLng is { } lng ? Math.Round(lng, 2, MidpointRounding.AwayFromZero) : null,
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
            $"Your order for {await DescribeAsync(order, ct)} has been placed and is awaiting approval.");
        await db.SaveChangesAsync(ct);

        return OrderOperationResult<OrderResponse>.Ok(await ToResponseAsync(order, reservation.Reservation, ct));
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

        return OrderOperationResult<OrderResponse>.Ok(await ToResponseAsync(order, order.StockReservation, ct));
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
            // Orders against listings this farmer owns — a single SQL join now that
            // Component A's real Listing table exists (was a per-order port lookup).
            query = query.Where(o => db.Listings.Any(l => l.Id == o.ListingId && l.FarmerId == currentUserId));
        }
        else if (role == Roles.Officer)
        {
            var scope = await OfficerScope.ForAsync(db, currentUserId, ct);
            query = scope.Apply(db, query);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<OrderResponse>(await ToResponsesAsync(items, ct), page, size, total);
    }

    /// <summary>
    /// The order's activity timeline: its audit entries (and its pickup schedule's), oldest
    /// first, with friendly summaries and actor names. Visible to exactly the users who can
    /// view the order itself; anyone else gets the same 404 as for a missing order (IDOR).
    /// </summary>
    public async Task<OrderOperationResult<IReadOnlyList<OrderActivityItem>>> GetActivityAsync(
        Guid orderId, Guid currentUserId, string role, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null || !await CanViewAsync(order, currentUserId, role, ct))
        {
            return OrderOperationResult<IReadOnlyList<OrderActivityItem>>.Fail(
                OrderOperationError.NotFound, "Order not found.");
        }

        var scheduleIds = await db.PickupSchedules.AsNoTracking()
            .Where(p => p.OrderId == orderId).Select(p => p.Id).ToListAsync(ct);

        var entries = await db.AuditLogs.AsNoTracking()
            .Where(a => (a.EntityType == "Order" && a.EntityId == orderId)
                        || (a.EntityType == "PickupSchedule" && scheduleIds.Contains(a.EntityId)))
            .OrderBy(a => a.Timestamp)
            .ToListAsync(ct);

        var actorIds = entries.Select(a => a.ActorId).Where(id => id != AuditLogService.SystemActorId).Distinct().ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        IReadOnlyList<OrderActivityItem> items = entries
            .Select(a => OrderActivityMapper.ToItem(
                a,
                a.ActorId == AuditLogService.SystemActorId
                    ? OrderActivityMapper.SystemActorName
                    : names.GetValueOrDefault(a.ActorId, "Unknown user")))
            .ToList();

        return OrderOperationResult<IReadOnlyList<OrderActivityItem>>.Ok(items);
    }

    public async Task<OrderOperationResult<OrderResponse>> UpdateStatusAsync(
        Guid orderId, OrderStatus newStatus, Guid actorId, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.StockReservation).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return OrderOperationResult<OrderResponse>.Fail(OrderOperationError.NotFound, "Order not found.");
        }

        if (!await (await OfficerScope.ForAsync(db, actorId, ct)).CanAccessAsync(db, orderId, ct))
        {
            // 404, not 403 - another centre's order must not be discoverable.
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
        await NotifyOrderStakeholdersAsync(
            order, $"Your order for {await DescribeAsync(order, ct)} is now {newStatus}.", ct);

        await db.SaveChangesAsync(ct);

        if (newStatus == OrderStatus.Approved)
        {
            await TryAutoProposeScheduleAsync(order.Id, actorId, ct);
        }

        return OrderOperationResult<OrderResponse>.Ok(await ToResponseAsync(order, order.StockReservation, ct));
    }

    /// <summary>
    /// Approval kicks off a schedule *proposal* (never a confirmation - FR19/CLAUDE.md
    /// 17/18: the Officer still has to Approve/Reject/Request Revision it). A failed
    /// proposal (no capacity, agent down, ...) must not undo the approval itself, so
    /// it's logged and the Officer can retry from the order page.
    /// </summary>
    private async Task TryAutoProposeScheduleAsync(Guid orderId, Guid actorId, CancellationToken ct)
    {
        if (schedulingService is null)
        {
            return;
        }

        try
        {
            // The buyer's approximate location (if they shared one when ordering) lets the
            // Matching Agent suggest the nearest centre; without it the region default applies.
            var location = await db.Orders.AsNoTracking()
                .Where(o => o.Id == orderId && o.BuyerLatitude != null && o.BuyerLongitude != null)
                .Select(o => new BuyerLocationDto(o.BuyerLatitude!.Value, o.BuyerLongitude!.Value))
                .FirstOrDefaultAsync(ct);

            var proposal = await schedulingService.ProposeAsync(
                orderId, new CreateScheduleRequest(null, null, location), actorId, ct);
            if (!proposal.Success)
            {
                logger?.LogWarning(
                    "Auto-proposal for order {OrderId} did not produce a schedule: {Reason}",
                    orderId, proposal.ErrorMessage);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogError(ex, "Auto-proposal for order {OrderId} failed.", orderId);
        }
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
            if (!await (await OfficerScope.ForAsync(db, currentUserId, ct)).CanAccessAsync(db, orderId, ct))
            {
                return OrderOperationResult<OrderResponse>.Fail(OrderOperationError.NotFound, "Order not found.");
            }

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
        await NotifyOrderStakeholdersAsync(
            order, $"Your order for {await DescribeAsync(order, ct)} was cancelled.", ct);

        await db.SaveChangesAsync(ct);

        // No separate reservation-release write is needed: the active-reservation
        // sum in StockReservationService already filters out Cancelled orders, so
        // the reservation stops counting the instant this commits (plan §7.2).
        return OrderOperationResult<OrderResponse>.Ok(await ToResponseAsync(order, order.StockReservation, ct));
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
            return await (await OfficerScope.ForAsync(db, currentUserId, ct)).CanAccessAsync(db, order.Id, ct);
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

    /// <summary>"18.75 kg of Carrots" - the human-readable subject of an order for
    /// notification text (falls back to just the quantity if the listing is gone).</summary>
    private async Task<string> DescribeAsync(Order order, CancellationToken ct)
    {
        var listing = await db.Listings.AsNoTracking()
            .Where(l => l.Id == order.ListingId)
            .Select(l => new { l.Unit, CropName = l.Crop.Name })
            .FirstOrDefaultAsync(ct);

        var quantity = order.Quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        return listing is null ? quantity : $"{quantity} {listing.Unit} of {listing.CropName}";
    }

    private async Task<OrderResponse> ToResponseAsync(Order order, StockReservation? reservation, CancellationToken ct)
    {
        order.StockReservation ??= reservation;
        return (await ToResponsesAsync([order], ct))[0];
    }

    /// <summary>Builds responses for a page of orders with a fixed number of batched
    /// lookups (listings, users, schedules, centres) rather than one per order.</summary>
    private async Task<List<OrderResponse>> ToResponsesAsync(IReadOnlyList<Order> orders, CancellationToken ct)
    {
        if (orders.Count == 0)
        {
            return [];
        }

        var listingIds = orders.Select(o => o.ListingId).Distinct().ToList();
        var orderIds = orders.Select(o => o.Id).ToList();

        var listings = await db.Listings.AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .Select(l => new ListingInfo(l.Id, l.FarmerId, l.Unit, l.RegionId, l.Crop.Name, l.Region.Name))
            .ToDictionaryAsync(l => l.Id, ct);

        var userIds = orders.Select(o => o.BuyerId).Concat(listings.Values.Select(l => l.FarmerId)).Distinct().ToList();
        var userNames = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var schedules = await db.PickupSchedules.AsNoTracking()
            .Where(p => orderIds.Contains(p.OrderId))
            .ToDictionaryAsync(p => p.OrderId, ct);

        var regionIds = listings.Values.Select(l => l.RegionId).Distinct().ToList();
        var scheduleCentreIds = schedules.Values.Select(sc => sc.CollectionCentreId).Distinct().ToList();
        var centres = await db.CollectionCentres.AsNoTracking()
            .Where(c => regionIds.Contains(c.RegionId) || scheduleCentreIds.Contains(c.Id))
            .Select(c => new CentreInfo(c.Id, c.Name, c.RegionId))
            .ToListAsync(ct);

        string? NameOf(Guid id) =>
            userNames.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n) ? n : null;

        return orders.Select(order =>
        {
            listings.TryGetValue(order.ListingId, out var listing);
            schedules.TryGetValue(order.Id, out var schedule);

            // The order's centre: wherever its schedule is booked, else the default
            // centre serving the listing's region (what auto-proposal will pick).
            CentreInfo? centre = null;
            if (schedule is not null)
            {
                centre = centres.FirstOrDefault(c => c.Id == schedule.CollectionCentreId);
            }
            else if (listing is not null)
            {
                centre = centres.Where(c => c.RegionId == listing.RegionId).OrderBy(c => c.Name).FirstOrDefault();
            }

            return new OrderResponse(
                order.Id,
                order.ListingId,
                order.BuyerId,
                order.Quantity,
                order.Status,
                order.DeliveryPreference,
                order.CreatedAt,
                order.UpdatedAt,
                // The hold only matters (and only expires) while Pending.
                order.Status == OrderStatus.Pending ? order.StockReservation?.ExpiresAt : null,
                listing?.CropName,
                listing?.Unit,
                listing?.FarmerId,
                listing is null ? null : NameOf(listing.FarmerId),
                NameOf(order.BuyerId),
                listing?.RegionName,
                centre?.Id,
                centre?.Name,
                schedule?.Status,
                schedule?.SlotStart,
                schedule?.SlotEnd);
        }).ToList();
    }

    private sealed record ListingInfo(Guid Id, Guid FarmerId, string Unit, Guid RegionId, string CropName, string RegionName);

    private sealed record CentreInfo(Guid Id, string Name, Guid RegionId);
}
