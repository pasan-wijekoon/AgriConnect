import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../models/price_trend.dart';

class PriceTrendException implements Exception {
  const PriceTrendException(this.message);

  final String message;

  @override
  String toString() => message;
}

/// Reads market price trends from the AgriConnect API (Component D).
class PriceTrendService {
  PriceTrendService({http.Client? client, String? baseUrl})
      : _client = client ?? http.Client(),
        _baseUrl = baseUrl ?? defaultBaseUrl();

  final http.Client _client;
  final String _baseUrl;

  static const _timeout = Duration(seconds: 15);

  /// Override with --dart-define=API_BASE_URL=https://... ; otherwise the local dev API.
  static String defaultBaseUrl() {
    const configured = String.fromEnvironment('API_BASE_URL');
    if (configured.isNotEmpty) return configured;
    // The Android emulator reaches the host machine through 10.0.2.2, not localhost.
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) return 'http://10.0.2.2:5000';
    return 'http://localhost:5000';
  }

  // TODO: send the signed-in farmer's JWT once login exists. Until then the API's
  // Development-only fake sign-in accepts this header.
  static const _headers = {'Accept': 'application/json', 'X-Dev-Role': 'Farmer'};

  Future<List<NamedItem>> fetchCrops() async {
    final json = await _get(Uri.parse('$_baseUrl/api/analytics/filters'));
    return (json['crops'] as List<dynamic>)
        .map((c) => NamedItem.fromJson(c as Map<String, dynamic>))
        .toList();
  }

  /// Weekly prices for [cropId] across all regions combined.
  Future<PriceTrend> fetchTrend({required String cropId, required DateTime from, required DateTime to}) async {
    final uri = Uri.parse('$_baseUrl/api/analytics/price-trends').replace(queryParameters: {
      'cropId': cropId,
      'from': _date(from),
      'to': _date(to),
      'bucket': 'week',
    });
    return PriceTrend.fromJson(await _get(uri));
  }

  Future<Map<String, dynamic>> _get(Uri uri) async {
    final http.Response response;
    try {
      response = await _client.get(uri, headers: _headers).timeout(_timeout);
    } on TimeoutException {
      throw const PriceTrendException('The server took too long to answer. Please try again.');
    } on http.ClientException {
      throw const PriceTrendException("Can't reach the AgriConnect server. Check your connection.");
    }

    if (response.statusCode != 200) {
      throw PriceTrendException(_problemMessage(response));
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  /// The API returns RFC 7807 ProblemDetails; show its detail when there is one.
  static String _problemMessage(http.Response response) {
    try {
      final body = jsonDecode(response.body) as Map<String, dynamic>;
      final detail = body['detail'] ?? body['title'];
      if (detail is String && detail.isNotEmpty) return detail;
    } catch (_) {
      // Not JSON; fall through to the generic message.
    }
    return 'Prices are unavailable right now (error ${response.statusCode}).';
  }

  static String _date(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}
