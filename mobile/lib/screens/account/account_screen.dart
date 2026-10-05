import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/notification_provider.dart';
import '../../providers/session_provider.dart';
import '../../theme/app_colors.dart';
import '../notifications/notifications_screen.dart';

/// The signed-in user's profile, notifications entry point and sign-out —
/// this is where the old "Me (Dev Identity)" tab went, now backed by the real
/// login rather than a role picker.
class AccountScreen extends StatelessWidget {
  const AccountScreen({super.key});

  Future<void> _confirmSignOut(BuildContext context) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign out?'),
        content: const Text('You will need to sign in again to place or track orders.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Sign out')),
        ],
      ),
    );
    if (ok == true && context.mounted) {
      await context.read<SessionProvider>().logout();
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<SessionProvider>().user;
    final unread = context.watch<NotificationProvider>().unread;
    if (user == null) return const SizedBox.shrink();

    final initial = user.fullName.trim().isEmpty ? '?' : user.fullName.trim()[0].toUpperCase();

    return Scaffold(
      appBar: AppBar(title: const Text('Account')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 30,
                    backgroundColor: AppColors.successBg,
                    child: Text(initial,
                        style: const TextStyle(
                            fontSize: 26, fontWeight: FontWeight.w700, color: AppColors.primary)),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(user.fullName,
                            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                        const SizedBox(height: 4),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                          decoration: BoxDecoration(
                            color: AppColors.successBg,
                            borderRadius: BorderRadius.circular(999),
                          ),
                          child: Text(user.role,
                              style: const TextStyle(
                                  color: AppColors.primary, fontSize: 12, fontWeight: FontWeight.w600)),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: Column(
              children: [
                _InfoRow(icon: Icons.email_outlined, label: 'Email', value: user.email),
                const Divider(height: 1),
                _InfoRow(icon: Icons.phone_outlined, label: 'Phone', value: user.phone ?? 'Not provided'),
                const Divider(height: 1),
                _InfoRow(icon: Icons.place_outlined, label: 'Region', value: user.region ?? 'Not provided'),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: ListTile(
              leading: Badge(
                isLabelVisible: unread > 0,
                label: Text('$unread'),
                child: const Icon(Icons.notifications_outlined),
              ),
              title: const Text('Notifications'),
              subtitle: Text(unread > 0 ? '$unread unread' : 'No unread updates'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const NotificationsScreen()),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: ListTile(
              leading: const Icon(Icons.logout, color: AppColors.error),
              title: const Text('Sign out', style: TextStyle(color: AppColors.error)),
              onTap: () => _confirmSignOut(context),
            ),
          ),
          const SizedBox(height: 24),
          const Center(
            child: Text('AgriConnect · v1.0.0',
                style: TextStyle(color: AppColors.textMuted, fontSize: 12)),
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon, color: AppColors.textSecondary),
      title: Text(label, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
      subtitle: Text(value, style: const TextStyle(fontSize: 15, color: AppColors.text)),
    );
  }
}
