import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/collection_centre.dart';
import '../models/order.dart';

/// Typed client for Component B's Order/Scheduling/CollectionCentre endpoints
/// — the Flutter counterpart to `web/src/utils/ordersApi.ts`. Same API, same
/// contract, one client per platform (CLAUDE.md §22 — no duplicated API
/// clients; plan §11 explicitly calls for this as a second client, not a
/// second implementation of the contract).
class ApiException implements Exception {
  final int status;
  final String message;

  ApiException(this.status, this.message);

  @override
  String toString() => message;
}

class OrderService {
  final http.Client _client;

  OrderService({http.Client? client}) : _client = client ?? http.Client();

  Future<T> _request<T>(
    String role,
    String userId,
    String method,
    String path, {
    Map<String, dynamic>? body,
    required T Function(dynamic) decode,
  }) async {
    final uri = Uri.parse('$apiBaseUrl$path');
    final headers = {
      'Content-Type': 'application/json',
      'X-Dev-Role': role,
      'X-Dev-UserId': userId,
    };

    final response = switch (method) {
      'GET' => await _client.get(uri, headers: headers),
      'POST' => await _client.post(uri,
          headers: headers, body: body == null ? null : jsonEncode(body)),
      'PUT' => await _client.put(uri,
          headers: headers, body: body == null ? null : jsonEncode(body)),
      _ => throw ArgumentError('Unsupported method: $method'),
    };

    if (response.statusCode < 200 || response.statusCode >= 300) {
      String message = 'Request failed with status ${response.statusCode}.';
      try {
        final problem = jsonDecode(response.body) as Map<String, dynamic>;
        message = (problem['detail'] ?? problem['title'] ?? message) as String;
      } catch (_) {
        // Response body wasn't JSON (or was empty) — fall back to the
        // generic message above.
      }
      throw ApiException(response.statusCode, message);
    }

    if (response.body.isEmpty) {
      return decode(null);
    }
    return decode(jsonDecode(response.body));
  }

  Future<Order> placeOrder(
    String role,
    String userId, {
    required String listingId,
    required double quantity,
    required DeliveryPreference deliveryPreference,
  }) =>
      _request(
        role,
        userId,
        'POST',
        '/api/orders',
        body: {
          'listingId': listingId,
          'quantity': quantity,
          'deliveryPreference': deliveryPreferenceToJson(deliveryPreference),
        },
        decode: (json) => Order.fromJson(json as Map<String, dynamic>),
      );

  Future<PagedResult<Order>> listOrders(
    String role,
    String userId, {
    OrderStatus? status,
    int page = 1,
    int size = 20,
  }) {
    final query = {
      'page': '$page',
      'size': '$size',
      if (status != null) 'status': orderStatusToJson(status),
    };
    final path = '/api/orders?${Uri(queryParameters: query).query}';
    return _request(
      role,
      userId,
      'GET',
      path,
      decode: (json) => PagedResult.fromJson(
        json as Map<String, dynamic>,
        (item) => Order.fromJson(item),
      ),
    );
  }

  Future<Order> getOrder(String role, String userId, String id) => _request(
        role,
        userId,
        'GET',
        '/api/orders/$id',
        decode: (json) => Order.fromJson(json as Map<String, dynamic>),
      );

  Future<Order> cancelOrder(String role, String userId, String id,
          {String? reason}) =>
      _request(
        role,
        userId,
        'POST',
        '/api/orders/$id/cancel',
        body: {'reason': reason},
        decode: (json) => Order.fromJson(json as Map<String, dynamic>),
      );

  Future<PickupSchedule?> getSchedule(
      String role, String userId, String orderId) async {
    try {
      return await _request(
        role,
        userId,
        'GET',
        '/api/orders/$orderId/schedule',
        decode: (json) => PickupSchedule.fromJson(json as Map<String, dynamic>),
      );
    } on ApiException catch (e) {
      if (e.status == 404) return null;
      rethrow;
    }
  }

  Future<List<NearestCentre>> nearestCentres(
    String role,
    String userId, {
    required double lat,
    required double lng,
    String? regionId,
  }) {
    final query = {
      'lat': '$lat',
      'lng': '$lng',
      'regionId': ?regionId,
    };
    final path = '/api/collection-centres/nearest?${Uri(queryParameters: query).query}';
    return _request(
      role,
      userId,
      'GET',
      path,
      decode: (json) => (json as List)
          .map((e) => NearestCentre.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
