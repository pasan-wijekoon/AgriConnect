import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/order.dart';
import '../../providers/order_provider.dart';
import '../../providers/session_provider.dart';
import '../../theme/app_colors.dart';
import '../../widgets/notification_bell.dart';
import '../../widgets/order_status_badge.dart';
import '../../widgets/state_views.dart';
import '../orders/order_detail_screen.dart';

class HomeAction {
  final IconData icon;
  final String label;
  final String subtitle;
  final VoidCallback onTap;

  const HomeAction({
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.onTap,
  });
}

/// Landing dashboard after sign-in: greeting, order summary, role-specific
/// shortcuts and the latest orders. Shortcuts are supplied by the shell so this
/// screen stays free of navigation/tab knowledge.
class HomeScreen extends StatelessWidget {
  final List<HomeAction> actions;
  final VoidCallback onSeeOrders;

  const HomeScreen({super.key, required this.actions, required this.onSeeOrders});

  String _greeting() {
    final h = DateTime.now().hour;
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<SessionProvider>().user;
    final orders = context.watch<OrderProvider>();
    if (user == null) return const SizedBox.shrink();

    final firstName = user.fullName.trim().split(' ').first;
    final pending = orders.orders.where((o) => o.status == OrderStatus.pending).length;
    final scheduled = orders.orders.where((o) => o.status == OrderStatus.scheduled).length;
    final recent = orders.orders.take(3).toList();

    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.eco, color: AppColors.primary),
            SizedBox(width: 8),
            Text('AgriConnect', style: TextStyle(fontWeight: FontWeight.w800)),
          ],
        ),
        actions: const [NotificationBell()],
      ),
      body: RefreshIndicator(
        onRefresh: orders.load,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [AppColors.primary, Color(0xFF43A047)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${_greeting()}, $firstName',
                      style: const TextStyle(
                          color: Colors.white, fontSize: 22, fontWeight: FontWeight.w800)),
                  const SizedBox(height: 4),
                  Text(
                    user.isFarmer
                        ? 'Manage your listings and the orders buyers place on them.'
                        : 'Find fresh produce and track your orders end to end.',
                    style: const TextStyle(color: Colors.white70),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(child: _Stat(label: 'Active', value: orders.activeCount, color: AppColors.primary)),
                const SizedBox(width: 10),
                Expanded(child: _Stat(label: 'Awaiting review', value: pending, color: AppColors.pending)),
                const SizedBox(width: 10),
                Expanded(child: _Stat(label: 'Scheduled', value: scheduled, color: AppColors.info)),
              ],
            ),
            if (orders.error != null && orders.orders.isEmpty) ...[
              const SizedBox(height: 12),
              InfoBanner(message: 'Could not load your orders: ${orders.error}'),
            ],
            const SizedBox(height: 20),
            const Text('Quick actions', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 10),
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 10,
              crossAxisSpacing: 10,
              childAspectRatio: 1.45,
              children: [for (final a in actions) _ActionTile(action: a)],
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                const Expanded(
                  child: Text('Recent orders',
                      style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                ),
                TextButton(onPressed: onSeeOrders, child: const Text('See all')),
              ],
            ),
            if (orders.loading && orders.orders.isEmpty)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (recent.isEmpty)
              const Card(
                child: Padding(
                  padding: EdgeInsets.all(20),
                  child: Text('No orders yet. They will appear here as soon as one is placed.',
                      style: TextStyle(color: AppColors.textSecondary)),
                ),
              )
            else
              for (final o in recent) ...[
                _RecentOrder(order: o),
                const SizedBox(height: 8),
              ],
          ],
        ),
      ),
    );
  }
}

class _Stat extends StatelessWidget {
  final String label;
  final int value;
  final Color color;

  const _Stat({required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 10),
        child: Column(
          children: [
            Text('$value', style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: color)),
            const SizedBox(height: 2),
            Text(label,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 11, color: AppColors.textSecondary)),
          ],
        ),
      ),
    );
  }
}

class _ActionTile extends StatelessWidget {
  final HomeAction action;
  const _ActionTile({required this.action});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: action.onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(action.icon, color: AppColors.primary),
              const SizedBox(height: 8),
              Text(action.label, style: const TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 2),
              Text(action.subtitle,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
            ],
          ),
        ),
      ),
    );
  }
}

class _RecentOrder extends StatelessWidget {
  final Order order;
  const _RecentOrder({required this.order});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => OrderDetailScreen(orderId: order.id)),
        ),
        title: Text(order.title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text('${order.quantityLabel} · ${formatDate(order.createdAt)}'),
        trailing: OrderStatusBadge(status: order.status),
      ),
    );
  }
}
