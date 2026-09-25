/// Mirrors `backend/src/dtos/OrderDtos.cs` / `web/src/utils/ordersApi.ts`'s
/// `OrderResponse` exactly — same API, same contract, second client (plan §11).
library;

enum OrderStatus { pending, approved, scheduled, completed, cancelled }

enum DeliveryPreference { pickup, delivery }

enum ScheduleStatus { proposed, confirmed, cancelled }

OrderStatus orderStatusFromJson(String value) => switch (value) {
      'Pending' => OrderStatus.pending,
      'Approved' => OrderStatus.approved,
      'Scheduled' => OrderStatus.scheduled,
      'Completed' => OrderStatus.completed,
      'Cancelled' => OrderStatus.cancelled,
      _ => throw ArgumentError('Unknown OrderStatus: $value'),
    };

String orderStatusToJson(OrderStatus status) => switch (status) {
      OrderStatus.pending => 'Pending',
      OrderStatus.approved => 'Approved',
      OrderStatus.scheduled => 'Scheduled',
      OrderStatus.completed => 'Completed',
      OrderStatus.cancelled => 'Cancelled',
    };

DeliveryPreference deliveryPreferenceFromJson(String value) => switch (value) {
      'Pickup' => DeliveryPreference.pickup,
      'Delivery' => DeliveryPreference.delivery,
      _ => throw ArgumentError('Unknown DeliveryPreference: $value'),
    };

String deliveryPreferenceToJson(DeliveryPreference value) => switch (value) {
      DeliveryPreference.pickup => 'Pickup',
      DeliveryPreference.delivery => 'Delivery',
    };

ScheduleStatus scheduleStatusFromJson(String value) => switch (value) {
      'Proposed' => ScheduleStatus.proposed,
      'Confirmed' => ScheduleStatus.confirmed,
      'Cancelled' => ScheduleStatus.cancelled,
      _ => throw ArgumentError('Unknown ScheduleStatus: $value'),
    };

class Order {
  final String id;
  final String listingId;
  final String buyerId;
  final double quantity;
  final OrderStatus status;
  final DeliveryPreference deliveryPreference;
  final DateTime createdAt;
  final DateTime updatedAt;
  final DateTime? reservationExpiresAt;

  Order({
    required this.id,
    required this.listingId,
    required this.buyerId,
    required this.quantity,
    required this.status,
    required this.deliveryPreference,
    required this.createdAt,
    required this.updatedAt,
    this.reservationExpiresAt,
  });

  factory Order.fromJson(Map<String, dynamic> json) => Order(
        id: json['id'] as String,
        listingId: json['listingId'] as String,
        buyerId: json['buyerId'] as String,
        quantity: (json['quantity'] as num).toDouble(),
        status: orderStatusFromJson(json['status'] as String),
        deliveryPreference:
            deliveryPreferenceFromJson(json['deliveryPreference'] as String),
        createdAt: DateTime.parse(json['createdAt'] as String),
        updatedAt: DateTime.parse(json['updatedAt'] as String),
        reservationExpiresAt: json['reservationExpiresAt'] == null
            ? null
            : DateTime.parse(json['reservationExpiresAt'] as String),
      );
}

class PagedResult<T> {
  final List<T> items;
  final int page;
  final int size;
  final int totalCount;

  PagedResult({
    required this.items,
    required this.page,
    required this.size,
    required this.totalCount,
  });

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) itemFromJson,
  ) =>
      PagedResult(
        items: (json['items'] as List)
            .map((e) => itemFromJson(e as Map<String, dynamic>))
            .toList(),
        page: json['page'] as int,
        size: json['size'] as int,
        totalCount: json['totalCount'] as int,
      );
}

class PickupSchedule {
  final String id;
  final String orderId;
  final String collectionCentreId;
  final DateTime slotStart;
  final DateTime slotEnd;
  final ScheduleStatus status;
  final bool conflictChecked;

  PickupSchedule({
    required this.id,
    required this.orderId,
    required this.collectionCentreId,
    required this.slotStart,
    required this.slotEnd,
    required this.status,
    required this.conflictChecked,
  });

  factory PickupSchedule.fromJson(Map<String, dynamic> json) => PickupSchedule(
        id: json['id'] as String,
        orderId: json['orderId'] as String,
        collectionCentreId: json['collectionCentreId'] as String,
        slotStart: DateTime.parse(json['slotStart'] as String),
        slotEnd: DateTime.parse(json['slotEnd'] as String),
        status: scheduleStatusFromJson(json['status'] as String),
        conflictChecked: json['conflictChecked'] as bool,
      );
}
