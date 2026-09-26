import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';
import 'app_button.dart';

// ─────────────────────────────────────────────────────────────────────────────
// ErrorState — Bespoke Error State implementing Design.md §27
//
// Clear and understandable message, no stack trace or technical jargon, retry button.
// ─────────────────────────────────────────────────────────────────────────────

class ErrorState extends StatelessWidget {
  final String title;
  final String description;
  final VoidCallback? onRetry;
  final String retryLabel;

  const ErrorState({
    super.key,
    required this.title,
    required this.description,
    this.onRetry,
    this.retryLabel = 'Try Again',
  });

  @override
  Widget build(BuildContext context) {
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
                color: AppColors.statusErrorBg,
                borderRadius: BorderRadius.circular(AppRadius.container),
                border: Border.all(color: AppColors.statusErrorBorder, width: 1),
              ),
              child: const Icon(
                Icons.error_outline_rounded,
                size: 32,
                color: AppColors.statusError,
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
            if (onRetry != null) ...[
              const SizedBox(height: AppSpacing.xl),
              SizedBox(
                width: 160,
                child: AppButton(
                  text: retryLabel,
                  onPressed: onRetry,
                  variant: AppButtonVariant.primary,
                  size: AppButtonSize.medium,
                  icon: Icons.refresh_rounded,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
