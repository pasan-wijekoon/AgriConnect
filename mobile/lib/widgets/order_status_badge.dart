import 'package:flutter/material.dart';

import '../models/order.dart';
import '../theme/app_colors.dart';

/// Flutter counterpart to `web/src/components/orders/OrderStatusBadge.tsx` —
/// same Design.md §6/§20 status→color mapping, icon-plus-color (never color
/// alone, §39) so the two clients read as one system.
class _StatusMeta {
  final Color fg;
  final Color bg;
  final String icon;
  const _StatusMeta(this.fg, this.bg, this.icon);
}

const Map<OrderStatus, _StatusMeta> _orderStatusMeta = {
  OrderStatus.pending: _StatusMeta(AppColors.pending, AppColors.pendingBg, '⏳'),
  OrderStatus.approved: _StatusMeta(AppColors.success, AppColors.successBg, '✓'),
  OrderStatus.scheduled: _StatusMeta(AppColors.info, AppColors.infoBg, '📅'),
  OrderStatus.completed: _StatusMeta(AppColors.success, AppColors.successBg, '✓'),
  OrderStatus.cancelled: _StatusMeta(AppColors.error, AppColors.errorBg, '✕'),
};

const Map<ScheduleStatus, _StatusMeta> _scheduleStatusMeta = {
  ScheduleStatus.proposed: _StatusMeta(AppColors.proposed, AppColors.proposedBg, '✦'),
  ScheduleStatus.confirmed: _StatusMeta(AppColors.success, AppColors.successBg, '✓'),
  ScheduleStatus.cancelled: _StatusMeta(AppColors.error, AppColors.errorBg, '✕'),
};

String orderStatusLabel(OrderStatus status) => switch (status) {
      OrderStatus.pending => 'Pending',
      OrderStatus.approved => 'Approved',
      OrderStatus.scheduled => 'Scheduled',
      OrderStatus.completed => 'Completed',
      OrderStatus.cancelled => 'Cancelled',
    };

String scheduleStatusLabel(ScheduleStatus status) => switch (status) {
      ScheduleStatus.proposed => 'Proposed',
      ScheduleStatus.confirmed => 'Confirmed',
      ScheduleStatus.cancelled => 'Cancelled',
    };

class _Badge extends StatelessWidget {
  final _StatusMeta meta;
  final String label;

  const _Badge(this.meta, this.label);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: meta.bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        '${meta.icon} $label',
        style: TextStyle(
          color: meta.fg,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

class OrderStatusBadge extends StatelessWidget {
  final OrderStatus status;
  const OrderStatusBadge({super.key, required this.status});

  @override
  Widget build(BuildContext context) =>
      _Badge(_orderStatusMeta[status]!, orderStatusLabel(status));
}

class ScheduleStatusBadge extends StatelessWidget {
  final ScheduleStatus status;
  const ScheduleStatusBadge({super.key, required this.status});

  @override
  Widget build(BuildContext context) =>
      _Badge(_scheduleStatusMeta[status]!, scheduleStatusLabel(status));
}
