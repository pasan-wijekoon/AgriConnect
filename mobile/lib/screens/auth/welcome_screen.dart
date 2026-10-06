import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import 'login_screen.dart';
import 'register_screen.dart';

/// Landing page shown to signed-out users: what AgriConnect is, how an order
/// works end to end, and the two ways in (sign in / create account).
class WelcomeScreen extends StatelessWidget {
  const WelcomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) => SingleChildScrollView(
            child: ConstrainedBox(
              constraints: BoxConstraints(minHeight: constraints.maxHeight),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 480),
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(24, 32, 24, 24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Center(
                          child: Container(
                            padding: const EdgeInsets.all(18),
                            decoration: const BoxDecoration(
                              color: AppColors.successBg,
                              shape: BoxShape.circle,
                            ),
                            child: const Icon(Icons.eco, size: 48, color: AppColors.primary),
                          ),
                        ),
                        const SizedBox(height: 20),
                        const Text(
                          'AgriConnect',
                          textAlign: TextAlign.center,
                          style: TextStyle(fontSize: 32, fontWeight: FontWeight.w800, color: AppColors.text),
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Sell fresh produce and buy it at a fair price, with pickup handled through your nearest collection centre.',
                          textAlign: TextAlign.center,
                          style: TextStyle(fontSize: 15, color: AppColors.textSecondary, height: 1.4),
                        ),
                        const SizedBox(height: 32),
                        const _Step(
                          icon: Icons.storefront_outlined,
                          title: 'List or browse',
                          text: 'Farmers list produce with AI-assisted fair prices. Buyers browse verified listings.',
                        ),
                        const _Step(
                          icon: Icons.shopping_basket_outlined,
                          title: 'Order with reserved stock',
                          text: 'Your quantity is held for you the moment you order, so it can\'t be sold twice.',
                        ),
                        const _Step(
                          icon: Icons.event_available_outlined,
                          title: 'Pickup scheduled for you',
                          text: 'A collection-centre officer confirms a conflict-free pickup slot.',
                        ),
                        const _Step(
                          icon: Icons.timeline_outlined,
                          title: 'Track every step',
                          text: 'Follow your order from Pending to Completed and get notified on each change.',
                        ),
                        const SizedBox(height: 28),
                        FilledButton(
                          onPressed: () => Navigator.of(context).push(
                            MaterialPageRoute(builder: (_) => const LoginScreen()),
                          ),
                          style: FilledButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            minimumSize: const Size.fromHeight(50),
                          ),
                          child: const Text('Sign in'),
                        ),
                        const SizedBox(height: 12),
                        OutlinedButton(
                          onPressed: () => Navigator.of(context).push(
                            MaterialPageRoute(builder: (_) => const RegisterScreen()),
                          ),
                          style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(50)),
                          child: const Text('Create an account'),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Step extends StatelessWidget {
  final IconData icon;
  final String title;
  final String text;

  const _Step({required this.icon, required this.title, required this.text});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: AppColors.card,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: AppColors.border),
            ),
            child: Icon(icon, color: AppColors.primary),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15)),
                const SizedBox(height: 2),
                Text(text, style: const TextStyle(color: AppColors.textSecondary, height: 1.35)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
