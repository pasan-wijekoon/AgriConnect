import 'package:flutter/foundation.dart';

import '../models/order.dart';
import '../services/order_service.dart';

/// The signed-in buyer's/farmer's orders (role-scoped server-side) plus
/// place/cancel actions. Created per login session (see MainShell), so it
/// never carries one user's orders over to the next.
class OrderProvider extends ChangeNotifier {
  final OrderService _service;

  /// The user this instance belongs to; a different user gets a fresh provider.
  final String? ownerId;

  OrderProvider(this._service, {this.ownerId});

  List<Order> _orders = [];
  bool _loading = false;
  bool _loaded = false;
  String? _error;
  bool _submitting = false;

  List<Order> get orders => _orders;
  bool get loading => _loading;
  bool get loaded => _loaded;
  String? get error => _error;
  bool get submitting => _submitting;

  int get activeCount => _orders
      .where((o) =>
          o.status == OrderStatus.pending ||
          o.status == OrderStatus.approved ||
          o.status == OrderStatus.scheduled)
      .length;

  Future<void> load() async {
    _loading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _service.listOrders(size: 100);
      _orders = result.items;
      _loaded = true;
    } catch (e) {
      _error = e.toString();
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  /// Returns the placed order, or null with [error] set.
  Future<Order?> placeOrder({
    required String listingId,
    required double quantity,
    required DeliveryPreference deliveryPreference,
    double? buyerLat,
    double? buyerLng,
  }) async {
    _submitting = true;
    _error = null;
    notifyListeners();

    try {
      final order = await _service.placeOrder(
        listingId: listingId,
        quantity: quantity,
        deliveryPreference: deliveryPreference,
        buyerLat: buyerLat,
        buyerLng: buyerLng,
      );
      _orders = [order, ..._orders];
      return order;
    } catch (e) {
      _error = e.toString();
      return null;
    } finally {
      _submitting = false;
      notifyListeners();
    }
  }

  Future<Order?> cancel(String orderId, {String? reason}) async {
    _error = null;
    try {
      final updated = await _service.cancelOrder(orderId, reason: reason);
      _orders = _orders.map((o) => o.id == orderId ? updated : o).toList();
      notifyListeners();
      return updated;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return null;
    }
  }

  /// Re-fetches one order (e.g. from the detail page's pull-to-refresh) and
  /// folds it into the list.
  Future<Order?> refreshOne(String orderId) async {
    try {
      final updated = await _service.getOrder(orderId);
      _orders = _orders.map((o) => o.id == orderId ? updated : o).toList();
      notifyListeners();
      return updated;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return null;
    }
  }

  Future<PickupSchedule?> loadSchedule(String orderId) => _service.getSchedule(orderId);
}
