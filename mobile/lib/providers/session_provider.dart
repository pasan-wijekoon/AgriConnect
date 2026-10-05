import 'package:flutter/foundation.dart';

import '../models/auth_user.dart';
import '../services/auth_service.dart';
import '../services/backend_client.dart';

/// The one source of truth for "who is signed in" — replaces the old
/// dev-identity picker. Wraps [AuthService] (real JWT login, persisted
/// locally) and exposes an authenticated [BackendClient] for the order,
/// centre and notification endpoints.
class SessionProvider extends ChangeNotifier {
  final AuthService auth;
  late final BackendClient client;

  AuthUser? _user;
  bool _restoring = true;

  SessionProvider({AuthService? auth}) : auth = auth ?? AuthService() {
    // A 401 from any call means the saved token is no longer valid.
    client = BackendClient(getToken: () => this.auth.token, onUnauthorized: () {
      if (_user != null) logout();
    });
  }

  AuthUser? get user => _user;
  bool get isRestoring => _restoring;
  bool get isSignedIn => _user != null;
  String? get token => auth.token;

  Future<void> restore() async {
    _user = await auth.loadSavedSession();
    _restoring = false;
    notifyListeners();
  }

  Future<AuthUser> login(String email, String password) async {
    final user = await auth.login(email, password);
    _user = user;
    notifyListeners();
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
    final user = await auth.register(
      fullName: fullName,
      email: email,
      password: password,
      role: role,
      phone: phone,
      region: region,
    );
    _user = user;
    notifyListeners();
    return user;
  }

  Future<void> logout() async {
    await auth.logout();
    _user = null;
    notifyListeners();
  }
}
