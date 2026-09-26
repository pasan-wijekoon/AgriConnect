import 'package:flutter/material.dart';
import 'models/auth_user.dart';
import 'services/api_service.dart';
import 'services/auth_service.dart';
import 'screens/browse_listings_screen.dart';
import 'screens/create_listing_screen.dart';
import 'screens/login_screen.dart';
import 'screens/my_listings_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const AgriConnectApp());
}

class AgriConnectApp extends StatelessWidget {
  const AgriConnectApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AgriConnect',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF2E7D32), // Forest Green
          primary: const Color(0xFF2E7D32),
          secondary: const Color(0xFF388E3C),
          surface: Colors.white,
        ),
        appBarTheme: const AppBarTheme(
          centerTitle: false,
          elevation: 0,
          backgroundColor: Colors.white,
          foregroundColor: Colors.black87,
        ),
        scaffoldBackgroundColor: const Color(0xFFF9FBF9),
      ),
      home: const AuthGate(),
    );
  }
}

/// Root of the app below MaterialApp: shows a loading splash while checking
/// for a saved session, then either the login screen or the main app shell.
/// The backend now requires a valid bearer token on every listing/price
/// endpoint, so there is no more "just use the app as a demo farmer" fallback.
class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

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
    return MainNavigationShell(authService: _authService, onLogout: _handleLogout);
  }
}

class MainNavigationShell extends StatefulWidget {
  final AuthService authService;
  final VoidCallback onLogout;

  const MainNavigationShell({super.key, required this.authService, required this.onLogout});

  @override
  State<MainNavigationShell> createState() => _MainNavigationShellState();
}

class _MainNavigationShellState extends State<MainNavigationShell> {
  int _currentIndex = 0;
  late final ApiService _apiService;

  late final List<Widget> _screens;

  @override
  void initState() {
    super.initState();
    _apiService = ApiService(getToken: () => widget.authService.token);
    _screens = [
      BrowseListingsScreen(apiService: _apiService, onLogout: widget.onLogout),
      CreateListingScreen(
        apiService: _apiService,
        onListingCreated: () {
          setState(() => _currentIndex = 2); // Switch to My Listings
        },
      ),
      MyListingsScreen(apiService: _apiService),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: _screens,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (idx) => setState(() => _currentIndex = idx),
        indicatorColor: Colors.green.shade100,
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.storefront_outlined),
            selectedIcon: Icon(Icons.storefront, color: Color(0xFF2E7D32)),
            label: 'Marketplace',
          ),
          NavigationDestination(
            icon: Icon(Icons.add_circle_outline),
            selectedIcon: Icon(Icons.add_circle, color: Color(0xFF2E7D32)),
            label: 'New Listing',
          ),
          NavigationDestination(
            icon: Icon(Icons.inventory_2_outlined),
            selectedIcon: Icon(Icons.inventory_2, color: Color(0xFF2E7D32)),
            label: 'My Listings',
          ),
        ],
      ),
    );
  }
}
