import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/order.dart';
import '../../providers/order_provider.dart';
import '../../providers/session_provider.dart';
import '../../theme/app_colors.dart';
import '../../widgets/order_status_badge.dart';
import '../../widgets/state_views.dart';
import 'order_detail_screen.dart';

enum _Filter { all, active, completed, cancelled }

/// "My Orders" / "Incoming Orders" (FR11): the signed-in user's orders
/// (role-scoped server-side), filterable, each opening a tracking timeline.
class OrderTrackingScreen extends StatefulWidget {
  const OrderTrackingScreen({super.key});

  @override
  State<OrderTrackingScreen> createState() => _OrderTrackingScreenState();
}

class _OrderTrackingScreenState extends State<OrderTrackingScreen> {
  _Filter _filter = _Filter.all;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final orders = context.read<OrderProvider>();
      if (!orders.loaded && !orders.loading) orders.load();
    });
  }

  bool _matches(Order o) => switch (_filter) {
        _Filter.all => true,
        _Filter.active => o.status == OrderStatus.pending ||
            o.status == OrderStatus.approved ||
            o.status == OrderStatus.scheduled,
        _Filter.completed => o.status == OrderStatus.completed,
        _Filter.cancelled => o.status == OrderStatus.cancelled,
      };

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrderProvider>();
    final isFarmer = context.watch<SessionProvider>().user?.isFarmer ?? false;
    final visible = orders.orders.where(_matches).toList();

    return Scaffold(
      appBar: AppBar(title: Text(isFarmer ? 'Incoming Orders' : 'My Orders')),
      body: Column(
        children: [
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                for (final f in _Filter.values) ...[
                  ChoiceChip(
                    label: Text(switch (f) {
                      _Filter.all => 'All',
                      _Filter.active => 'Active',
                      _Filter.completed => 'Completed',
                      _Filter.cancelled => 'Cancelled',
                    }),
                    selected: _filter == f,
                    onSelected: (_) => setState(() => _filter = f),
                  ),
                  const SizedBox(width: 8),
                ],
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: RefreshIndicator(
              onRefresh: orders.load,
              child: _body(orders, visible, isFarmer),
            ),
          ),
        ],
      ),
    );
  }

  Widget _body(OrderProvider orders, List<Order> visible, bool isFarmer) {
    if (orders.loading && orders.orders.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (orders.error != null && orders.orders.isEmpty) {
      return CenteredMessage(
        icon: Icons.error_outline,
        title: 'Could not load orders',
        subtitle: orders.error!,
        actionLabel: 'Try again',
        onAction: orders.load,
      );
    }

    if (orders.orders.isEmpty) {
      return CenteredMessage(
        icon: Icons.inbox_outlined,
        title: 'No orders yet',
        subtitle: isFarmer
            ? 'Orders placed against your listings will show up here.'
            : 'Browse the marketplace and place your first order.',
      );
    }

    if (visible.isEmpty) {
      return const CenteredMessage(
        icon: Icons.filter_list_off,
        title: 'Nothing in this view',
        subtitle: 'Try a different filter.',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: visible.length,
      separatorBuilder: (_, _) => const SizedBox(height: 12),
      itemBuilder: (context, index) => _OrderCard(order: visible[index], isFarmer: isFarmer),
    );
  }
}

class _OrderCard extends StatelessWidget {
  final Order order;
  final bool isFarmer;

  const _OrderCard({required this.order, required this.isFarmer});

  @override
  Widget build(BuildContext context) {
    final counterpart = isFarmer ? order.buyerName : order.farmerName;

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => OrderDetailScreen(orderId: order.id)),
        ),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(order.title,
                        style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                  ),
                  OrderStatusBadge(status: order.status),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                '${order.quantityLabel} · ${deliveryPreferenceToJson(order.deliveryPreference)}'
                '${counterpart != null ? ' · ${isFarmer ? 'Buyer' : 'Farmer'}: $counterpart' : ''}',
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              if (order.slotStart != null) ...[
                const SizedBox(height: 10),
                Row(
                  children: [
                    const Icon(Icons.event_outlined, size: 16, color: AppColors.textSecondary),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        '${formatDateTime(order.slotStart!)}'
                        '${order.collectionCentreName != null ? ' · ${order.collectionCentreName}' : ''}',
                        style: const TextStyle(fontSize: 13),
                      ),
                    ),
                    if (order.scheduleStatus != null) ScheduleStatusBadge(status: order.scheduleStatus!),
                  ],
                ),
              ] else if (order.status == OrderStatus.pending) ...[
                const SizedBox(height: 10),
                const Text('Waiting for an officer to review your order',
                    style: TextStyle(fontSize: 13, color: AppColors.pending)),
              ],
              const SizedBox(height: 8),
              Text('Placed ${formatDate(order.createdAt)}',
                  style: const TextStyle(fontSize: 12, color: AppColors.textMuted)),
            ],
          ),
        ),
      ),
    );
  }
}
