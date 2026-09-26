import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';
import 'app_button.dart';

// ─────────────────────────────────────────────────────────────────────────────
// EmptyState — Bespoke Empty State implementing Design.md §26
//
// Clear heading, supportive subtitle, custom icon container, and action button.
// ─────────────────────────────────────────────────────────────────────────────

class EmptyState extends StatelessWidget {
  final IconData icon;
  final String title;
  final String description;
  final String? actionLabel;
  final String? buttonText;
  final VoidCallback? onAction;

  const EmptyState({
    super.key,
    this.icon = Icons.inventory_2_outlined,
    required this.title,
    required this.description,
    this.actionLabel,
    this.buttonText,
    this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    final effectiveLabel = buttonText ?? actionLabel;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xxl),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: BoxDecoration(
                color: AppColors.primaryGreenLight,
                borderRadius: BorderRadius.circular(AppRadius.container),
                border: Border.all(
                  color: AppColors.primaryGreen.withValues(alpha: 0.15),
                  width: 1,
                ),
              ),
              child: Icon(
                icon,
                size: 32,
                color: AppColors.primaryGreen,
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            Text(
              title,
              style: GoogleFonts.inter(
                fontSize: 18,
                fontWeight: FontWeight.w600,
                color: AppColors.textPrimary,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AppSpacing.sm),
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 320),
              child: Text(
                description,
                style: GoogleFonts.inter(
                  fontSize: 14,
                  color: AppColors.textSecondary,
                  height: 1.5,
                ),
                textAlign: TextAlign.center,
              ),
            ),
            if (effectiveLabel != null && onAction != null) ...[
              const SizedBox(height: AppSpacing.xl),
              SizedBox(
                width: 180,
                child: AppButton(
                  text: effectiveLabel,
                  onPressed: onAction,
                  variant: AppButtonVariant.primary,
                  size: AppButtonSize.medium,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
