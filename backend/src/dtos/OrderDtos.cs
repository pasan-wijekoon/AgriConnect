using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos;

public record CreateOrderRequest(Guid ListingId, decimal Quantity, DeliveryPreference DeliveryPreference);

public record OrderResponse(
    Guid Id,
    Guid ListingId,
    Guid BuyerId,
    decimal Quantity,
    OrderStatus Status,
    DeliveryPreference DeliveryPreference,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReservationExpiresAt);

public record OrderStatusUpdateRequest(OrderStatus Status);

public record OrderCancelRequest(string? Reason);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, int TotalCount);
