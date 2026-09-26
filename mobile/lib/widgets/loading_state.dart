import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// LoadingState — Bespoke Shimmer / Spinner widgets implementing Design.md §25
//
// Never leave a blank screen while data is loading. Uses skeleton shimmer cards.
// ─────────────────────────────────────────────────────────────────────────────

/// Centered spinner with optional caption for page loading.
class LoadingState extends StatelessWidget {
  final String? message;

  const LoadingState({super.key, this.message});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const SizedBox(
            width: 32,
            height: 32,
            child: CircularProgressIndicator(
              color: AppColors.primaryGreen,
              strokeWidth: 2.5,
            ),
          ),
          if (message != null) ...[
            const SizedBox(height: AppSpacing.base),
            Text(
              message!,
              style: GoogleFonts.inter(
                fontSize: 14,
                color: AppColors.textSecondary,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Skeleton shimmer card — shown as placeholder while listings load.
class SkeletonCard extends StatefulWidget {
  const SkeletonCard({super.key});

  @override
  State<SkeletonCard> createState() => _SkeletonCardState();
}

class _SkeletonCardState extends State<SkeletonCard>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    )..repeat(reverse: true);
    _animation = Tween<double>(begin: 0.35, end: 0.85).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Widget _block({double width = double.infinity, double height = 14, double radius = 6}) {
    return AnimatedBuilder(
      animation: _animation,
      builder: (context, child) => Container(
        width: width,
        height: height,
        decoration: BoxDecoration(
          color: AppColors.borderLight.withValues(alpha: _animation.value),
          borderRadius: BorderRadius.circular(radius),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      padding: const EdgeInsets.all(AppSpacing.base),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.borderLight, width: 1),
        boxShadow: AppShadows.subtle,
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Placeholder image thumbnail
          _block(width: 72, height: 72, radius: AppRadius.control),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _block(width: 160, height: 16, radius: 4),
                const SizedBox(height: AppSpacing.sm),
                _block(width: 100, height: 12, radius: 4),
                const SizedBox(height: AppSpacing.md),
                _block(width: 80, height: 22, radius: AppRadius.pill),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Renders skeleton cards for listing list loading state.
class LoadingListSkeleton extends StatelessWidget {
  final int count;

  const LoadingListSkeleton({super.key, this.count = 4});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        children: List.generate(count, (_) => const SkeletonCard()),
      ),
    );
  }
}

/// Alias for backwards compatibility
typedef ListingsSkeletonLoader = LoadingListSkeleton;
