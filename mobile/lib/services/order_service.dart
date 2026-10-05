import '../models/collection_centre.dart';
import '../models/order.dart';
import 'backend_client.dart';

export 'backend_client.dart' show ApiException;

/// Typed client for Component B's Order/Scheduling/CollectionCentre endpoints
/// — the Flutter counterpart to `web/src/utils/ordersApi.ts`. Same API, same
/// contract, one client per platform.
class OrderService {
  final BackendClient _api;

  OrderService(this._api);

  Future<Order> placeOrder({
    required String listingId,
    required double quantity,
    required DeliveryPreference deliveryPreference,
    double? buyerLat,
    double? buyerLng,
  }) =>
      _api.request(
        'POST',
        '/api/orders',
        body: {
          'listingId': listingId,
          'quantity': quantity,
          'deliveryPreference': deliveryPreferenceToJson(deliveryPreference),
          // Optional approximate location (both or neither) for the nearest-centre suggestion.
          if (buyerLat != null && buyerLng != null) ...{
            'buyerLat': buyerLat,
            'buyerLng': buyerLng,
          },
        },
        decode: (json) => Order.fromJson(json as Map<String, dynamic>),
      );

  Future<PagedResult<Order>> listOrders({
    OrderStatus? status,
    int page = 1,
    int size = 50,
  }) {
    final query = {
      'page': '$page',
      'size': '$size',
      if (status != null) 'status': orderStatusToJson(status),
    };
    return _api.request(
      'GET',
      '/api/orders?${Uri(queryParameters: query).query}',
      decode: (json) => PagedResult.fromJson(
        json as Map<String, dynamic>,
        (item) => Order.fromJson(item),
      ),
    );
  }

  Future<Order> getOrder(String id) => _api.request(
        'GET',
        '/api/orders/$id',
        decode: (json) => Order.fromJson(json as Map<String, dynamic>),
      );

  Future<Order> cancelOrder(String id, {String? reason}) => _api.request(
        'POST',
        '/api/orders/$id/cancel',
        body: {'reason': reason},
        decode: (json) => Order.fromJson(json as Map<String, dynamic>),
      );

  Future<PickupSchedule?> getSchedule(String orderId) async {
    try {
      return await _api.request(
        'GET',
        '/api/orders/$orderId/schedule',
        decode: (json) => PickupSchedule.fromJson(json as Map<String, dynamic>),
      );
    } on ApiException catch (e) {
      if (e.status == 404) return null;
      rethrow;
    }
  }

  Future<List<NearestCentre>> nearestCentres({
    required double lat,
    required double lng,
    String? regionId,
  }) {
    final query = {
      'lat': '$lat',
      'lng': '$lng',
      'regionId': ?regionId,
    };
    return _api.request(
      'GET',
      '/api/collection-centres/nearest?${Uri(queryParameters: query).query}',
      decode: (json) => (json as List)
          .map((e) => NearestCentre.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
