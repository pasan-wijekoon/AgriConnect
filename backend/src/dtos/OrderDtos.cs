using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos;

/// <param name="BuyerLat">Optional approximate buyer location; both values or neither.</param>
public record CreateOrderRequest(
    Guid ListingId, decimal Quantity, DeliveryPreference DeliveryPreference,
    decimal? BuyerLat = null, decimal? BuyerLng = null);

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

/// <summary>One entry of an order's activity timeline (FR11), built from the audit log (FR20).</summary>
/// <param name="Explanation">Why the nearest centre was suggested (only on that entry).</param>
public record OrderActivityItem(
    DateTimeOffset Timestamp, string Action, string Summary, string ActorName, string? Explanation = null);

public record OrderCancelRequest(string? Reason);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int Size, int TotalCount);
