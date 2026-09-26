import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../models/auth_user.dart';

/// Handles login/register against the backend's JWT auth endpoints and
/// persists the session locally so the farmer/buyer isn't asked to log in
/// every time the app opens. The backend now requires a valid bearer token
/// on every listing/price endpoint, so this is a hard requirement, not a
/// nice-to-have — see AuthService.cs / [Authorize] on the .NET controllers.
class AuthService {
  static const _storageKey = 'agriconnect_auth_user';

  static String get defaultBaseUrl {
    if (!kIsWeb && Platform.isAndroid) {
      return 'http://10.0.2.2:5000/api';
    }
    return 'http://localhost:5000/api';
  }

  final String baseUrl;
  AuthUser? _current;

  AuthService({String? baseUrl}) : baseUrl = baseUrl ?? defaultBaseUrl;

  AuthUser? get currentUser => _current;
  String? get token => _current?.token;
  bool get isLoggedIn => _current != null;

  /// Loads any previously-saved session from local storage. Call once at
  /// app startup before deciding whether to show the login screen.
  Future<AuthUser?> loadSavedSession() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_storageKey);
    if (raw == null) return null;
    try {
      _current = AuthUser.fromJson(jsonDecode(raw) as Map<String, dynamic>);
      return _current;
    } catch (_) {
      await prefs.remove(_storageKey);
      return null;
    }
  }

  Future<AuthUser> login(String email, String password) async {
    final res = await http.post(
      Uri.parse('$baseUrl/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );

    if (res.statusCode != 200) {
      final body = _tryDecode(res.body);
      throw Exception(body?['error'] ?? 'Login failed (${res.statusCode}).');
    }

    final user = AuthUser.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
    await _persist(user);
    return user;
  }

  Future<AuthUser> register({
    required String fullName,
    required String email,
    required String password,
    required String role,
    String? phone,
    String? region,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/auth/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'fullName': fullName,
        'email': email,
        'password': password,
        'role': role,
        'phone': phone,
        'region': region,
      }),
    );

    if (res.statusCode != 201 && res.statusCode != 200) {
      final body = _tryDecode(res.body);
      throw Exception(body?['error'] ?? 'Registration failed (${res.statusCode}).');
    }

    final user = AuthUser.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
    await _persist(user);
    return user;
  }

  Future<void> logout() async {
    _current = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_storageKey);
  }

  Future<void> _persist(AuthUser user) async {
    _current = user;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_storageKey, jsonEncode(user.toJson()));
  }

  Map<String, dynamic>? _tryDecode(String body) {
    try {
      return jsonDecode(body) as Map<String, dynamic>;
    } catch (_) {
      return null;
    }
  }
}
