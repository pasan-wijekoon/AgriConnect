import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../providers/dev_identity_provider.dart';
import '../theme/app_colors.dart';

/// Dev-only role/user-id picker — the Flutter counterpart to the header
/// picker in `web/src/components/layout/AppShell.tsx`. No real sign-in
/// exists yet (plan §6/§0.3), so this is how a tester switches between
/// Buyer and Farmer identities to exercise role-scoped behaviour. Must be
/// replaced together with the backend's dev-auth bypass once real auth
/// lands (see known issues carried over from Phase 9).
class DevIdentityScreen extends StatelessWidget {
  const DevIdentityScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final identity = context.watch<DevIdentityProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Me (Dev Identity)')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.pendingBg,
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Text(
                'No sign-in exists yet — pick a role/user id to test as. '
                'Never ships to a real environment.',
                style: TextStyle(color: AppColors.pending, fontSize: 13),
              ),
            ),
            const SizedBox(height: 24),
            const Text('Role', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            SegmentedButton<DevRole>(
              segments: const [
                ButtonSegment(value: DevRole.buyer, label: Text('Buyer')),
                ButtonSegment(value: DevRole.farmer, label: Text('Farmer')),
              ],
              selected: {identity.role},
              onSelectionChanged: (selection) =>
                  context.read<DevIdentityProvider>().setRole(selection.first),
            ),
            const SizedBox(height: 20),
            const Text('User Id', style: TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            TextFormField(
              key: ValueKey(identity.userId),
              initialValue: identity.userId,
              decoration: const InputDecoration(border: OutlineInputBorder()),
              onFieldSubmitted: (value) =>
                  context.read<DevIdentityProvider>().setUserId(value),
            ),
          ],
        ),
      ),
    );
  }
}
