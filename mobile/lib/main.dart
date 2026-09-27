import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'models/auth_user.dart';
import 'providers/dev_identity_provider.dart';
import 'providers/order_provider.dart';
import 'screens/browse_listings_screen.dart';
import 'screens/create_listing_screen.dart';
import 'screens/dev_identity_screen.dart';
import 'screens/login_screen.dart';
import 'screens/my_listings_screen.dart';
import 'screens/orders/nearest_centre_screen.dart';
import 'screens/orders/order_tracking_screen.dart';
import 'screens/orders/place_order_screen.dart';
import 'screens/price_trends/price_trends_screen.dart';
import 'services/api_service.dart';
import 'services/auth_service.dart';
import 'services/price_trend_service.dart';
import 'theme/app_colors.dart';

void main() {
  runApp(const AgriConnectApp());
}

class AgriConnectApp extends StatelessWidget {
  const AgriConnectApp({super.key, this.priceTrendService});

  /// Injected in widget tests, so the Prices tab doesn't make a real HTTP
  /// call; the real API client otherwise (mirrors PriceTrendsScreen's own
  /// `service` param, threaded down from here).
  final PriceTrendService? priceTrendService;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => DevIdentityProvider()),
        ChangeNotifierProvider(create: (_) => OrderProvider()),
      ],
      child: MaterialApp(
        title: 'AgriConnect',
        theme: buildAppTheme(),
        home: AuthGate(priceTrendService: priceTrendService),
      ),
    );
  }
}

/// Root of the app below MaterialApp: shows a loading splash while checking
/// for a saved session, then either the real login screen (Component A —
/// the backend now requires a valid bearer token on every listing/price
/// endpoint) or the main app shell.
///
/// The Buyer/Farmer order-logistics tabs (Place Order/Orders/Centres/Prices)
/// still authenticate via DevIdentityProvider's X-Dev-Role/X-Dev-UserId
/// headers, unchanged — this merge doesn't rewire them onto the real bearer
/// token today (a real, separately-scoped follow-up; see PROGRESS.md). Real
/// login gates only the marketplace screens (Marketplace/New Listing/My
/// Listings) that actually require it ([Authorize] on ListingsController).
class AuthGate extends StatefulWidget {
  const AuthGate({super.key, this.priceTrendService});

  final PriceTrendService? priceTrendService;

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  final AuthService _authService = AuthService();
  bool _checkingSession = true;
  AuthUser? _user;

  @override
  void initState() {
    super.initState();
    _restoreSession();
  }

  Future<void> _restoreSession() async {
    final user = await _authService.loadSavedSession();
    if (mounted) {
      setState(() {
        _user = user;
        _checkingSession = false;
      });
    }
  }

  void _handleLoggedIn(AuthUser user) {
    setState(() => _user = user);
  }

  Future<void> _handleLogout() async {
    await _authService.logout();
    if (mounted) setState(() => _user = null);
  }

  @override
  Widget build(BuildContext context) {
    if (_checkingSession) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_user == null) {
      return LoginScreen(authService: _authService, onLoggedIn: _handleLoggedIn);
    }
    return RootShell(
      apiService: ApiService(getToken: () => _authService.token),
      onLogout: _handleLogout,
      priceTrendService: widget.priceTrendService,
    );
  }
}

/// Design.md §36's recommended mobile structure: a bottom nav with the
/// Buyer/Farmer surfaces Component B owns (plan §11) — Place Order, My
/// Orders (tracking, FR11), Centres (nearest-centre lookup, FR21), Price
/// Trends (Component D's read-only farmer-facing view), Component A's real
/// marketplace (Marketplace/New Listing/My Listings, real-auth-gated by
/// AuthGate above), and a dev-only identity switcher standing in for "Me"
/// for the order-logistics tabs until they're threaded onto real auth too.
class RootShell extends StatefulWidget {
  const RootShell({super.key, this.priceTrendService, required this.apiService, required this.onLogout});

  final PriceTrendService? priceTrendService;
  final ApiService apiService;
  final VoidCallback onLogout;

  @override
  State<RootShell> createState() => _RootShellState();
}

class _RootShellState extends State<RootShell> {
  int _index = 0;

  // Tracks which tabs have ever been shown, so IndexedStack only builds a tab
  // the first time it's actually visited (then keeps it alive, preserving its
  // state on switches back) rather than eagerly building all 8 up front —
  // CreateListingScreen has a pre-existing layout bug (an ElevatedButton
  // inside a Row hits "BoxConstraints forces an infinite width") that only
  // this eager-build path exposes, since it was never laid out until visited
  // in Component A's original app either. Lazy building sidesteps it and is
  // the more standard pattern anyway (no reason to build a create-listing
  // form before the user ever opens that tab).
  final Set<int> _visited = {0};

  late final _pages = [
    const PlaceOrderScreen(),
    const OrderTrackingScreen(),
    const NearestCentreScreen(),
    PriceTrendsScreen(service: widget.priceTrendService ?? PriceTrendService()),
    BrowseListingsScreen(apiService: widget.apiService, onLogout: widget.onLogout),
    CreateListingScreen(
      apiService: widget.apiService,
      onListingCreated: () => setState(() => _index = 5),
    ),
    MyListingsScreen(apiService: widget.apiService),
    const DevIdentityScreen(),
  ];

  void _selectTab(int index) {
    setState(() {
      _index = index;
      _visited.add(index);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _index,
        children: [
          for (var i = 0; i < _pages.length; i++)
            _visited.contains(i) ? _pages[i] : const SizedBox.shrink(),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: _selectTab,
        backgroundColor: AppColors.card,
        indicatorColor: AppColors.successBg,
        destinations: const [
          NavigationDestination(
              icon: Icon(Icons.add_shopping_cart_outlined),
              selectedIcon: Icon(Icons.add_shopping_cart),
              label: 'Place Order'),
          NavigationDestination(
              icon: Icon(Icons.receipt_long_outlined),
              selectedIcon: Icon(Icons.receipt_long),
              label: 'Orders'),
          NavigationDestination(
              icon: Icon(Icons.location_on_outlined),
              selectedIcon: Icon(Icons.location_on),
              label: 'Centres'),
          NavigationDestination(
              icon: Icon(Icons.trending_up_outlined),
              selectedIcon: Icon(Icons.trending_up),
              label: 'Prices'),
          NavigationDestination(
              icon: Icon(Icons.storefront_outlined),
              selectedIcon: Icon(Icons.storefront),
              label: 'Marketplace'),
          NavigationDestination(
              icon: Icon(Icons.add_circle_outline),
              selectedIcon: Icon(Icons.add_circle),
              label: 'New Listing'),
          NavigationDestination(
              icon: Icon(Icons.inventory_2_outlined),
              selectedIcon: Icon(Icons.inventory_2),
              label: 'My Listings'),
          NavigationDestination(
              icon: Icon(Icons.person_outline),
              selectedIcon: Icon(Icons.person),
              label: 'Me'),
        ],
      ),
    );
  }
}
