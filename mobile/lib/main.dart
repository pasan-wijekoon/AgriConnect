import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/dev_identity_provider.dart';
import 'providers/order_provider.dart';
import 'screens/dev_identity_screen.dart';
import 'screens/orders/nearest_centre_screen.dart';
import 'screens/orders/order_tracking_screen.dart';
import 'screens/orders/place_order_screen.dart';
import 'theme/app_colors.dart';

void main() {
  runApp(const AgriConnectApp());
}

class AgriConnectApp extends StatelessWidget {
  const AgriConnectApp({super.key});

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
        home: const RootShell(),
      ),
    );
  }
}

/// Design.md §36's recommended mobile structure: a bottom nav with the
/// Buyer/Farmer surfaces Component B owns (plan §11) — Place Order, My
/// Orders (tracking, FR11), Centres (nearest-centre lookup, FR21), and a
/// dev-only identity switcher standing in for "Me" until real auth lands.
class RootShell extends StatefulWidget {
  const RootShell({super.key});

  @override
  State<RootShell> createState() => _RootShellState();
}

class _RootShellState extends State<RootShell> {
  int _index = 0;

  static const _pages = [
    PlaceOrderScreen(),
    OrderTrackingScreen(),
    NearestCentreScreen(),
    DevIdentityScreen(),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(index: _index, children: _pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (index) => setState(() => _index = index),
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
              icon: Icon(Icons.person_outline),
              selectedIcon: Icon(Icons.person),
              label: 'Me'),
        ],
      ),
    );
  }
}
