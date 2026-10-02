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
    DateTimeOffset? ReservationExpiresAt,
    // Display fields (added so clients never have to show raw GUIDs) — all
    // optional/nullable: absent when the referenced listing/user/centre can't
    // be resolved.
    string? CropName = null,
    string? Unit = null,
    Guid? FarmerId = null,
    string? FarmerName = null,
    string? BuyerName = null,
    string? RegionName = null,
    Guid? CollectionCentreId = null,
    string? CollectionCentreName = null,
    ScheduleStatus? ScheduleStatus = null,
    DateTimeOffset? SlotStart = null,
    DateTimeOffset? SlotEnd = null);

public record OrderStatusUpdateRequest(OrderStatus Status);

public record OrderCancelRequest(string? Reason);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, int TotalCount);
