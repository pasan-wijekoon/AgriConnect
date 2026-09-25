import 'package:flutter/foundation.dart';

/// Client-side counterpart to the backend's dev-auth seam
/// (`backend/src/config/DevAuthenticationHandler.cs`, plan §6) — the Flutter
/// equivalent of `web/src/context/AuthContext.tsx`. No shared User/Auth/JWT
/// implementation exists anywhere in the repo yet, so every request carries
/// X-Dev-Role/X-Dev-UserId headers built from whatever is picked here.
/// Mobile only ever acts as Buyer or Farmer (plan §11 — Officer/Admin are
/// React's surface), unlike the web picker which covers all four roles.
///
/// Not persisted to disk (no `shared_preferences` dependency was justified
/// for this alone) — resets to the default identity on app restart, same as
/// any other in-memory dev/demo affordance in this codebase.
enum DevRole { buyer, farmer }

String devRoleToHeader(DevRole role) =>
    switch (role) { DevRole.buyer => 'Buyer', DevRole.farmer => 'Farmer' };

class DevIdentityProvider extends ChangeNotifier {
  // Matches backend/src/config/OrderLogisticsFixtures.cs's BuyerOne exactly,
  // so a fresh app instance immediately has orders to see against the seeded
  // demo data.
  static const defaultBuyerId = '4f2b3001-0000-0000-0000-000000000001';
  static const defaultFarmerId = '4f2b2001-0000-0000-0000-000000000001';

  DevRole _role = DevRole.buyer;
  String _userId = defaultBuyerId;

  DevRole get role => _role;
  String get userId => _userId;

  void setRole(DevRole role) {
    _role = role;
    _userId = role == DevRole.buyer ? defaultBuyerId : defaultFarmerId;
    notifyListeners();
  }

  void setUserId(String userId) {
    _userId = userId;
    notifyListeners();
  }
}
