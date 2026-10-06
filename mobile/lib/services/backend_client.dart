import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';

/// Error from the backend, carrying the HTTP status and the RFC 7807
/// `detail`/`title` (or a generic fallback) as a user-presentable message.
class ApiException implements Exception {
  final int status;
  final String message;

  ApiException(this.status, this.message);

  @override
  String toString() => message;
}

/// Single authenticated HTTP client for the Order/Scheduling/Centre/Notification
/// endpoints — every request carries the real login's bearer token (the dev
/// `X-Dev-*` header bypass is gone from the mobile app). A 401 means the saved
/// session is no longer valid, so [onUnauthorized] signs the user out.
class BackendClient {
  final http.Client _client;
  final String? Function() getToken;
  final void Function()? onUnauthorized;

  BackendClient({http.Client? client, required this.getToken, this.onUnauthorized})
      : _client = client ?? http.Client();

  Future<T> request<T>(
    String method,
    String path, {
    Map<String, dynamic>? body,
    required T Function(dynamic json) decode,
  }) async {
    final uri = Uri.parse('$apiBaseUrl$path');
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (getToken() case final token? when token.isNotEmpty)
        'Authorization': 'Bearer $token',
    };

    final http.Response response;
    try {
      response = switch (method) {
        'GET' => await _client.get(uri, headers: headers),
        'POST' => await _client.post(uri,
            headers: headers, body: body == null ? null : jsonEncode(body)),
        'PUT' => await _client.put(uri,
            headers: headers, body: body == null ? null : jsonEncode(body)),
        _ => throw ArgumentError('Unsupported method: $method'),
      };
    } on ArgumentError {
      rethrow;
    } catch (_) {
      throw ApiException(0, 'Cannot reach the server. Check your connection and try again.');
    }

    if (response.statusCode == 401) {
      onUnauthorized?.call();
      throw ApiException(401, 'Your session has expired. Please sign in again.');
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      var message = 'Request failed (${response.statusCode}).';
      try {
        final problem = jsonDecode(response.body) as Map<String, dynamic>;
        message = (problem['detail'] ?? problem['title'] ?? problem['error'] ?? message) as String;
      } catch (_) {
        // Non-JSON error body — keep the generic message above.
      }
      throw ApiException(response.statusCode, message);
    }

    if (response.body.isEmpty) {
      return decode(null);
    }
    return decode(jsonDecode(response.body));
  }
}
