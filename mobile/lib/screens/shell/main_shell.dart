import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/notification_provider.dart';
import '../../providers/order_provider.dart';
import '../../providers/session_provider.dart';
import '../../services/api_service.dart';
import '../../services/price_trend_service.dart';
import '../../theme/app_colors.dart';
import '../account/account_screen.dart';
import '../browse_listings_screen.dart';
import '../home/home_screen.dart';
import '../my_listings_screen.dart';
import '../orders/nearest_centre_screen.dart';
import '../orders/order_tracking_screen.dart';
import '../price_trends/price_trends_screen.dart';

class _Tab {
  final String label;
  final IconData icon;
  final IconData selectedIcon;
  final Widget Function() build;

  const _Tab(this.label, this.icon, this.selectedIcon, this.build);
}

/// Signed-in shell. A bottom nav of five tabs at most, chosen by role — Buyers
/// browse and order, Farmers manage listings and see incoming orders — instead
/// of the previous single 8-tab bar that mixed both roles' screens together.
class MainShell extends StatefulWidget {
  final PriceTrendService? priceTrendService;

  const MainShell({super.key, this.priceTrendService});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _index = 0;
  final Set<int> _visited = {0};
  late final bool _isFarmer;
  late final List<_Tab> _tabs;

  @override
  void initState() {
    super.initState();
    final user = context.read<SessionProvider>().user!;
    _isFarmer = user.isFarmer;
    final api = context.read<ApiService>();
    final priceService = widget.priceTrendService;

    Widget prices() => PriceTrendsScreen(service: priceService ?? PriceTrendService());

    _tabs = _isFarmer
        ? [
            _Tab('Home', Icons.home_outlined, Icons.home, _home),
            _Tab('Listings', Icons.inventory_2_outlined, Icons.inventory_2,
                () => MyListingsScreen(apiService: api)),
            _Tab('Orders', Icons.receipt_long_outlined, Icons.receipt_long,
                () => const OrderTrackingScreen()),
            _Tab('Prices', Icons.trending_up_outlined, Icons.trending_up, prices),
            _Tab('Account', Icons.person_outline, Icons.person, () => const AccountScreen()),
          ]
        : [
            _Tab('Home', Icons.home_outlined, Icons.home, _home),
            _Tab('Market', Icons.storefront_outlined, Icons.storefront,
                () => BrowseListingsScreen(apiService: api)),
            _Tab('Orders', Icons.receipt_long_outlined, Icons.receipt_long,
                () => const OrderTrackingScreen()),
            _Tab('Centres', Icons.location_on_outlined, Icons.location_on,
                () => const NearestCentreScreen()),
            _Tab('Account', Icons.person_outline, Icons.person, () => const AccountScreen()),
          ];

    // First load of the data every tab shares.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      context.read<OrderProvider>().load();
      context.read<NotificationProvider>().refreshUnread();
    });
  }

  Widget _home() {
    final priceService = widget.priceTrendService;

    void openPrices() => Navigator.of(context).push(MaterialPageRoute(
        builder: (_) => PriceTrendsScreen(service: priceService ?? PriceTrendService())));

    final actions = _isFarmer
        ? [
            HomeAction(
                icon: Icons.add_circle_outline,
                label: 'New listing',
                subtitle: 'Sell your produce',
                onTap: () => _selectTab(1)),
            HomeAction(
                icon: Icons.inventory_2_outlined,
                label: 'My listings',
                subtitle: 'Status and inspections',
                onTap: () => _selectTab(1)),
            HomeAction(
                icon: Icons.receipt_long_outlined,
                label: 'Incoming orders',
                subtitle: 'Track buyer orders',
                onTap: () => _selectTab(2)),
            HomeAction(
                icon: Icons.trending_up,
                label: 'Market prices',
                subtitle: 'Weekly price trends',
                onTap: () => _selectTab(3)),
          ]
        : [
            HomeAction(
                icon: Icons.storefront_outlined,
                label: 'Browse market',
                subtitle: 'Fresh, verified produce',
                onTap: () => _selectTab(1)),
            HomeAction(
                icon: Icons.receipt_long_outlined,
                label: 'My orders',
                subtitle: 'Track status & pickup',
                onTap: () => _selectTab(2)),
            HomeAction(
                icon: Icons.location_on_outlined,
                label: 'Find a centre',
                subtitle: 'Nearest collection point',
                onTap: () => _selectTab(3)),
            HomeAction(
                icon: Icons.trending_up,
                label: 'Market prices',
                subtitle: 'Weekly price trends',
                onTap: openPrices),
          ];

    return HomeScreen(actions: actions, onSeeOrders: () => _selectTab(2));
  }

  void _selectTab(int index) {
    setState(() {
      _index = index;
      _visited.add(index);
    });
    // Orders change server-side (officer approvals), so refresh whenever the
    // user comes back to a screen that shows them.
    if (index == 0 || index == 2) {
      context.read<OrderProvider>().load();
      context.read<NotificationProvider>().refreshUnread();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _index,
        children: [
          for (var i = 0; i < _tabs.length; i++)
            _visited.contains(i) ? _tabs[i].build() : const SizedBox.shrink(),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: _selectTab,
        backgroundColor: AppColors.card,
        indicatorColor: AppColors.successBg,
        destinations: [
          for (final t in _tabs)
            NavigationDestination(icon: Icon(t.icon), selectedIcon: Icon(t.selectedIcon), label: t.label),
        ],
      ),
    );
  }
}
