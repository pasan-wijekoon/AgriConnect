import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/notification_item.dart';
import '../../providers/notification_provider.dart';
import '../../theme/app_colors.dart';
import '../../widgets/state_views.dart';

/// Notifications inbox (FR22): order and schedule updates written by the
/// backend on every status change, newest first.
class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) context.read<NotificationProvider>().load();
    });
  }

  IconData _iconFor(String type) => switch (type) {
        'OrderPlaced' => Icons.shopping_basket_outlined,
        'OrderStatusChanged' => Icons.local_shipping_outlined,
        'ScheduleUpdate' => Icons.event_available_outlined,
        _ => Icons.notifications_outlined,
      };

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<NotificationProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Notifications'),
        actions: [
          if (provider.unread > 0)
            TextButton(
              onPressed: provider.markAllRead,
              child: const Text('Mark all read'),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: provider.load,
        child: _body(provider),
      ),
    );
  }

  Widget _body(NotificationProvider provider) {
    if (provider.loading && provider.items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (provider.error != null && provider.items.isEmpty) {
      return CenteredMessage(
        icon: Icons.error_outline,
        title: 'Could not load notifications',
        subtitle: provider.error!,
        actionLabel: 'Try again',
        onAction: provider.load,
      );
    }
    if (provider.items.isEmpty) {
      return const CenteredMessage(
        icon: Icons.notifications_none,
        title: 'You\'re all caught up',
        subtitle: 'Order and pickup updates will appear here.',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: provider.items.length,
      separatorBuilder: (_, _) => const SizedBox(height: 10),
      itemBuilder: (context, i) {
        final n = provider.items[i];
        return _NotificationTile(
          item: n,
          icon: _iconFor(n.type),
          onTap: () => provider.markRead(n),
        );
      },
    );
  }
}

class _NotificationTile extends StatelessWidget {
  final NotificationItem item;
  final IconData icon;
  final VoidCallback onTap;

  const _NotificationTile({required this.item, required this.icon, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      color: item.isRead ? AppColors.card : AppColors.successBg,
      child: ListTile(
        onTap: onTap,
        leading: CircleAvatar(
          backgroundColor: item.isRead ? AppColors.neutralBg : AppColors.card,
          child: Icon(icon, color: item.isRead ? AppColors.textMuted : AppColors.primary),
        ),
        title: Text(
          item.title ?? item.message,
          style: TextStyle(fontWeight: item.isRead ? FontWeight.w400 : FontWeight.w600),
        ),
        subtitle: Padding(
          padding: const EdgeInsets.only(top: 4),
          child: Text(
            item.title == null ? formatDateTime(item.createdAt.toLocal()) : '${item.message}\n${formatDateTime(item.createdAt.toLocal())}',
            style: const TextStyle(fontSize: 12),
          ),
        ),
        trailing: item.isRead
            ? null
            : const Icon(Icons.circle, size: 10, color: AppColors.primary),
      ),
    );
  }
}
