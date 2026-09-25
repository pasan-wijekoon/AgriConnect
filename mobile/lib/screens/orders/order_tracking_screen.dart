import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/order.dart';
import '../../providers/dev_identity_provider.dart';
import '../../providers/order_provider.dart';
import '../../theme/app_colors.dart';
import '../../widgets/order_status_badge.dart';

/// Order tracking screen (FR11, plan §11 / Design.md §32 — a timeline, with
/// completed stages visually distinguishable from upcoming ones, working
/// consistently on mobile). Shows the buyer/farmer's own orders (role-scoped
/// server-side) and opens a per-order timeline on tap.
class OrderTrackingScreen extends StatefulWidget {
  const OrderTrackingScreen({super.key});

  @override
  State<OrderTrackingScreen> createState() => _OrderTrackingScreenState();
}

class _OrderTrackingScreenState extends State<OrderTrackingScreen> {
  // RootShell keeps every tab alive in an IndexedStack, so this screen's
  // initState only ever runs once at app startup — reloading purely on
  // initState would mean switching the dev identity (Me tab) and coming
  // back here would keep showing the previous identity's stale order list.
  // Track which identity the current data was loaded for and re-fetch
  // whenever it changes instead.
  String? _loadedForKey;

  void _load(DevIdentityProvider identity) {
    context
        .read<OrderProvider>()
        .loadOrders(devRoleToHeader(identity.role), identity.userId);
  }

  void _loadIfIdentityChanged(DevIdentityProvider identity) {
    final key = '${identity.role}:${identity.userId}';
    if (_loadedForKey == key) return;
    _loadedForKey = key;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      _load(identity);
    });
  }

  @override
  Widget build(BuildContext context) {
    final identity = context.watch<DevIdentityProvider>();
    _loadIfIdentityChanged(identity);
    final orders = context.watch<OrderProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('My Orders')),
      body: RefreshIndicator(
        onRefresh: () async => _load(identity),
        child: _buildBody(orders, identity),
      ),
    );
  }

  Widget _buildBody(OrderProvider orders, DevIdentityProvider identity) {
    if (orders.loading && orders.orders.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (orders.error != null && orders.orders.isEmpty) {
      return _CenteredMessage(
        icon: Icons.error_outline,
        title: 'Could not load orders',
        subtitle: orders.error!,
        actionLabel: 'Try Again',
        onAction: () => _load(identity),
      );
    }

    if (orders.orders.isEmpty) {
      return const _CenteredMessage(
        icon: Icons.inbox_outlined,
        title: 'No orders yet',
        subtitle: 'Orders you place will show up here.',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: orders.orders.length,
      separatorBuilder: (_, _) => const SizedBox(height: 12),
      itemBuilder: (context, index) {
        final order = orders.orders[index];
        return Card(
          child: ListTile(
            contentPadding: const EdgeInsets.all(16),
            title: Text(
              '${order.quantity.toStringAsFixed(0)} kg · ${deliveryPreferenceToJson(order.deliveryPreference)}',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            subtitle: Padding(
              padding: const EdgeInsets.only(top: 8),
              child: OrderStatusBadge(status: order.status),
            ),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => _OrderTimelinePage(order: order)),
            ),
          ),
        );
      },
    );
  }
}

const _timelineStages = [
  OrderStatus.pending,
  OrderStatus.approved,
  OrderStatus.scheduled,
  OrderStatus.completed,
];

/// Per-order detail — Design.md §32's timeline, kept as a private page
/// within this file rather than a new top-level screen (plan §11 only calls
/// for one tracking screen; the timeline is that screen's own detail view).
class _OrderTimelinePage extends StatefulWidget {
  final Order order;
  const _OrderTimelinePage({required this.order});

  @override
  State<_OrderTimelinePage> createState() => _OrderTimelinePageState();
}

class _OrderTimelinePageState extends State<_OrderTimelinePage> {
  late Order _order;
  bool _cancelling = false;

  @override
  void initState() {
    super.initState();
    _order = widget.order;
  }

  Future<void> _cancel(DevIdentityProvider identity) async {
    setState(() => _cancelling = true);
    final ok = await context.read<OrderProvider>().cancelOrder(
          devRoleToHeader(identity.role),
          identity.userId,
          _order.id,
        );
    if (!mounted) return;
    setState(() => _cancelling = false);
    if (ok) {
      final updated = context
          .read<OrderProvider>()
          .orders
          .firstWhere((o) => o.id == _order.id, orElse: () => _order);
      setState(() => _order = updated);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
            content: Text(
                context.read<OrderProvider>().error ?? 'Could not cancel the order.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final order = _order;
    final identity = context.watch<DevIdentityProvider>();
    final canCancel = identity.role == DevRole.buyer &&
        (order.status == OrderStatus.pending || order.status == OrderStatus.approved);
    final isCancelled = order.status == OrderStatus.cancelled;
    final currentIndex = _timelineStages.indexOf(order.status);

    return Scaffold(
      appBar: AppBar(title: const Text('Order Tracking')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${order.quantity.toStringAsFixed(0)} kg · ${deliveryPreferenceToJson(order.deliveryPreference)}',
                    style: const TextStyle(
                        fontWeight: FontWeight.w700, fontSize: 18),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Placed ${_formatDate(order.createdAt)}',
                    style: const TextStyle(color: AppColors.textSecondary),
                  ),
                  if (order.reservationExpiresAt != null &&
                      order.status == OrderStatus.pending) ...[
                    const SizedBox(height: 8),
                    Text(
                      'Reservation held until ${_formatDate(order.reservationExpiresAt!)}',
                      style: const TextStyle(color: AppColors.pending, fontSize: 13),
                    ),
                  ],
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),
          if (isCancelled)
            Card(
              color: AppColors.errorBg,
              child: const Padding(
                padding: EdgeInsets.all(16),
                child: Row(
                  children: [
                    Icon(Icons.cancel_outlined, color: AppColors.error),
                    SizedBox(width: 12),
                    Text('This order was cancelled.',
                        style: TextStyle(color: AppColors.error)),
                  ],
                ),
              ),
            )
          else
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    for (var i = 0; i < _timelineStages.length; i++)
                      _TimelineRow(
                        label: orderStatusLabel(_timelineStages[i]),
                        completed: i <= currentIndex,
                        isLast: i == _timelineStages.length - 1,
                      ),
                  ],
                ),
              ),
            ),
          if (canCancel) ...[
            const SizedBox(height: 20),
            OutlinedButton(
              onPressed: _cancelling ? null : () => _cancel(identity),
              style: OutlinedButton.styleFrom(foregroundColor: AppColors.error),
              child: _cancelling
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Cancel Order'),
            ),
          ],
        ],
      ),
    );
  }

  static String _formatDate(DateTime date) =>
      '${date.day}/${date.month}/${date.year} ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
}

class _TimelineRow extends StatelessWidget {
  final String label;
  final bool completed;
  final bool isLast;

  const _TimelineRow({
    required this.label,
    required this.completed,
    required this.isLast,
  });

  @override
  Widget build(BuildContext context) {
    final color = completed ? AppColors.success : AppColors.textMuted;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Icon(
              completed ? Icons.check_circle : Icons.circle_outlined,
              color: color,
              size: 20,
            ),
            if (!isLast)
              Container(
                width: 2,
                height: 28,
                color: completed ? AppColors.success : AppColors.border,
              ),
          ],
        ),
        const SizedBox(width: 12),
        Padding(
          padding: const EdgeInsets.only(top: 1),
          child: Text(
            label,
            style: TextStyle(
              color: completed ? AppColors.text : AppColors.textMuted,
              fontWeight: completed ? FontWeight.w600 : FontWeight.w400,
            ),
          ),
        ),
      ],
    );
  }
}

class _CenteredMessage extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final String? actionLabel;
  final VoidCallback? onAction;

  const _CenteredMessage({
    required this.icon,
    required this.title,
    required this.subtitle,
    this.actionLabel,
    this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 48, color: AppColors.textMuted),
            const SizedBox(height: 16),
            Text(title,
                style: const TextStyle(
                    fontWeight: FontWeight.w600, fontSize: 16)),
            const SizedBox(height: 8),
            Text(
              subtitle,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            if (actionLabel != null) ...[
              const SizedBox(height: 16),
              OutlinedButton(onPressed: onAction, child: Text(actionLabel!)),
            ],
          ],
        ),
      ),
    );
  }
}
