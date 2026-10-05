import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/order.dart';
import '../../providers/order_provider.dart';
import '../../providers/session_provider.dart';
import '../../theme/app_colors.dart';
import '../../widgets/order_status_badge.dart';
import '../../widgets/state_views.dart';

const _timelineStages = [
  OrderStatus.pending,
  OrderStatus.approved,
  OrderStatus.scheduled,
  OrderStatus.completed,
];

/// Single order: header, Design.md §32 timeline, pickup schedule (proposed /
/// confirmed) and the buyer's Cancel action. Refreshes itself from the API on
/// open and on pull-to-refresh so an officer's approval shows up promptly.
class OrderDetailScreen extends StatefulWidget {
  final String orderId;
  final bool justPlaced;

  const OrderDetailScreen({super.key, required this.orderId, this.justPlaced = false});

  @override
  State<OrderDetailScreen> createState() => _OrderDetailScreenState();
}

class _OrderDetailScreenState extends State<OrderDetailScreen> {
  bool _cancelling = false;
  bool _refreshing = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _refresh());
  }

  Order? _order(OrderProvider orders) {
    for (final o in orders.orders) {
      if (o.id == widget.orderId) return o;
    }
    return null;
  }

  Future<void> _refresh() async {
    if (!mounted) return;
    setState(() => _refreshing = true);
    await context.read<OrderProvider>().refreshOne(widget.orderId);
    if (!mounted) return;
    setState(() => _refreshing = false);
  }

  Future<void> _cancel() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cancel this order?'),
        content: const Text('The stock reserved for you will be released.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Keep order')),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            child: const Text('Cancel order'),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;

    setState(() => _cancelling = true);
    final orders = context.read<OrderProvider>();
    final result = await orders.cancel(widget.orderId);
    if (!mounted) return;
    setState(() => _cancelling = false);
    if (result == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(orders.error ?? 'Could not cancel the order.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrderProvider>();
    final user = context.watch<SessionProvider>().user;
    final order = _order(orders);

    if (order == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Order')),
        body: _refreshing
            ? const Center(child: CircularProgressIndicator())
            : CenteredMessage(
                icon: Icons.search_off,
                title: 'Order not found',
                subtitle: orders.error ?? 'It may have been removed.',
                actionLabel: 'Try again',
                onAction: _refresh,
              ),
      );
    }

    final isBuyer = user?.isBuyer ?? false;
    final canCancel = isBuyer &&
        (order.status == OrderStatus.pending || order.status == OrderStatus.approved);
    final isCancelled = order.status == OrderStatus.cancelled;
    final currentIndex = _timelineStages.indexOf(order.status);

    return Scaffold(
      appBar: AppBar(title: const Text('Order tracking')),
      body: RefreshIndicator(
        onRefresh: _refresh,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            if (widget.justPlaced && order.status == OrderStatus.pending) ...[
              const InfoBanner(
                message: 'Order placed! Your stock is reserved while an officer reviews it.',
                color: AppColors.success,
                background: AppColors.successBg,
                icon: Icons.check_circle_outline,
              ),
              const SizedBox(height: 12),
            ],
            _header(order, isBuyer),
            const SizedBox(height: 16),
            if (isCancelled)
              const InfoBanner(
                message: 'This order was cancelled and its reserved stock was released.',
                icon: Icons.cancel_outlined,
              )
            else
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Progress', style: TextStyle(fontWeight: FontWeight.w700)),
                      const SizedBox(height: 12),
                      for (var i = 0; i < _timelineStages.length; i++)
                        _TimelineRow(
                          label: orderStatusLabel(_timelineStages[i]),
                          hint: _hintFor(_timelineStages[i]),
                          completed: i <= currentIndex,
                          current: i == currentIndex,
                          isLast: i == _timelineStages.length - 1,
                        ),
                    ],
                  ),
                ),
              ),
            if (order.slotStart != null && !isCancelled) ...[
              const SizedBox(height: 16),
              _scheduleCard(order),
            ],
            if (canCancel) ...[
              const SizedBox(height: 20),
              OutlinedButton(
                onPressed: _cancelling ? null : _cancel,
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.error,
                  minimumSize: const Size.fromHeight(48),
                ),
                child: _cancelling
                    ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Cancel order'),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String? _hintFor(OrderStatus s) => switch (s) {
        OrderStatus.pending => 'An officer reviews your order',
        OrderStatus.approved => 'A pickup slot is proposed',
        OrderStatus.scheduled => 'Pickup slot confirmed',
        OrderStatus.completed => 'Produce collected',
        _ => null,
      };

  Widget _header(Order order, bool isBuyer) {
    final counterpartLabel = isBuyer ? 'Farmer' : 'Buyer';
    final counterpart = isBuyer ? order.farmerName : order.buyerName;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(order.title,
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 20)),
                ),
                OrderStatusBadge(status: order.status),
              ],
            ),
            const SizedBox(height: 12),
            _kv('Quantity', order.quantityLabel),
            _kv('Handling', deliveryPreferenceToJson(order.deliveryPreference)),
            if (order.regionName != null) _kv('Region', order.regionName!),
            if (counterpart != null) _kv(counterpartLabel, counterpart),
            if (order.collectionCentreName != null) _kv('Collection centre', order.collectionCentreName!),
            _kv('Placed', formatDateTime(order.createdAt)),
            _kv('Order ref', '#${order.id.substring(0, 8)}'),
            if (order.status == OrderStatus.pending && order.reservationExpiresAt != null) ...[
              const SizedBox(height: 8),
              InfoBanner.warning(
                message: 'Stock held for you until ${formatDateTime(order.reservationExpiresAt!)}. '
                    'It is released if the order is not approved by then.',
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _scheduleCard(Order order) {
    final status = order.scheduleStatus;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Expanded(
                  child: Text('Pickup schedule', style: TextStyle(fontWeight: FontWeight.w700)),
                ),
                if (status != null) ScheduleStatusBadge(status: status),
              ],
            ),
            const SizedBox(height: 12),
            _kv('Date', formatDate(order.slotStart!)),
            _kv('Time',
                '${_time(order.slotStart!)}${order.slotEnd != null ? ' – ${_time(order.slotEnd!)}' : ''}'),
            if (order.collectionCentreName != null) _kv('Centre', order.collectionCentreName!),
            if (status == ScheduleStatus.proposed) ...[
              const SizedBox(height: 8),
              const InfoBanner.info(
                message: 'This slot is a proposal. It becomes final once the officer confirms it.',
              ),
            ],
          ],
        ),
      ),
    );
  }

  static String _time(DateTime d) =>
      '${d.hour.toString().padLeft(2, '0')}:${d.minute.toString().padLeft(2, '0')}';

  Widget _kv(String k, String v) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(width: 120, child: Text(k, style: const TextStyle(color: AppColors.textSecondary))),
            Expanded(child: Text(v, style: const TextStyle(fontWeight: FontWeight.w500))),
          ],
        ),
      );
}

class _TimelineRow extends StatelessWidget {
  final String label;
  final String? hint;
  final bool completed;
  final bool current;
  final bool isLast;

  const _TimelineRow({
    required this.label,
    required this.hint,
    required this.completed,
    required this.current,
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
            Icon(completed ? Icons.check_circle : Icons.circle_outlined, color: color, size: 22),
            if (!isLast)
              Container(width: 2, height: 34, color: completed ? AppColors.success : AppColors.border),
          ],
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(top: 1, bottom: 10),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    color: completed ? AppColors.text : AppColors.textMuted,
                    fontWeight: completed ? FontWeight.w700 : FontWeight.w400,
                  ),
                ),
                if (hint != null && current)
                  Text(hint!, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
