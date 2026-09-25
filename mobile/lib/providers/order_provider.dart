import 'package:flutter/foundation.dart';

import '../models/order.dart';
import '../services/order_service.dart';

/// Holds the current buyer/farmer's order list + place-order state for the
/// tracking and place-order screens (plan §11 — Provider, per ADR §11
/// decision 2). Talks to [OrderService]; screens read the dev identity
/// (role/userId) from [DevIdentityProvider] and pass it in explicitly rather
/// than this provider depending on that one directly.
class OrderProvider extends ChangeNotifier {
  final OrderService _service;

  OrderProvider({OrderService? service}) : _service = service ?? OrderService();

  List<Order> _orders = [];
  bool _loading = false;
  String? _error;
  bool _submitting = false;

  List<Order> get orders => _orders;
  bool get loading => _loading;
  String? get error => _error;
  bool get submitting => _submitting;

  Future<void> loadOrders(String role, String userId) async {
    _loading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _service.listOrders(role, userId, size: 50);
      _orders = result.items;
    } catch (e) {
      _error = e.toString();
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<Order?> placeOrder(
    String role,
    String userId, {
    required String listingId,
    required double quantity,
    required DeliveryPreference deliveryPreference,
  }) async {
    _submitting = true;
    _error = null;
    notifyListeners();

    try {
      final order = await _service.placeOrder(
        role,
        userId,
        listingId: listingId,
        quantity: quantity,
        deliveryPreference: deliveryPreference,
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

  Future<bool> cancelOrder(String role, String userId, String orderId) async {
    try {
      final updated = await _service.cancelOrder(role, userId, orderId);
      _orders = _orders.map((o) => o.id == orderId ? updated : o).toList();
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<PickupSchedule?> loadSchedule(
          String role, String userId, String orderId) =>
      _service.getSchedule(role, userId, orderId);
}
