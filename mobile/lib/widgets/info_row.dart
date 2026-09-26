import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// InfoRow — Bespoke Label + Value display row for cards and detail screens
// Design.md §14 & §7: Inter typography and consistent spacing
// ─────────────────────────────────────────────────────────────────────────────

class InfoRow extends StatelessWidget {
  final String label;
  final String? value;
  final Widget? valueWidget;
  final IconData? icon;
  final Color? textColor;
  final bool showDivider;

  const InfoRow({
    super.key,
    required this.label,
    this.value,
    this.valueWidget,
    this.icon,
    this.textColor,
    this.showDivider = true,
  }) : assert(value != null || valueWidget != null,
            'Either value or valueWidget must be provided');

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (icon != null) ...[
                Icon(icon, size: 16, color: AppColors.textSecondary),
                const SizedBox(width: AppSpacing.xs),
              ],
              // Label
              SizedBox(
                width: 130,
                child: Text(
                  label,
                  style: GoogleFonts.inter(
                    fontSize: 13,
                    fontWeight: FontWeight.w500,
                    color: AppColors.textSecondary,
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              // Value
              Expanded(
                child: valueWidget ??
                    Text(
                      value ?? '—',
                      style: GoogleFonts.inter(
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                        color: textColor ?? AppColors.textPrimary,
                      ),
                    ),
              ),
            ],
          ),
        ),
        if (showDivider)
          const Divider(height: 1, color: AppColors.borderLight),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// SectionHeader — Bespoke section title implementing Design.md §7
// ─────────────────────────────────────────────────────────────────────────────

class SectionHeader extends StatelessWidget {
  final String title;
  final String? subtitle;

  const SectionHeader({
    super.key,
    required this.title,
    this.subtitle,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(
        top: AppSpacing.xl,
        bottom: AppSpacing.md,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: GoogleFonts.inter(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.textSecondary,
              letterSpacing: 0.5,
            ),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: 2),
            Text(
              subtitle!,
              style: GoogleFonts.inter(
                fontSize: 12,
                color: AppColors.textMuted,
              ),
            ),
          ],
          const SizedBox(height: AppSpacing.sm),
          const Divider(height: 1, color: AppColors.borderLight),
        ],
      ),
    );
  }
}
