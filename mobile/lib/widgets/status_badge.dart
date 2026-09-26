import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// StatusBadge — Bespoke grade / listing-status pill badge
// Design.md §20 & §6 & §38 & §39
//
// Pill shape: border-radius 999px, subtle 1px border, semantic background & text,
// accompanied by clear icons so color is never the only status indicator.
// ─────────────────────────────────────────────────────────────────────────────

enum BadgeVariant { success, pending, warning, error, info, neutral }

class StatusBadge extends StatelessWidget {
  final String label;
  final BadgeVariant variant;
  final IconData? icon;

  const StatusBadge({
    super.key,
    required this.label,
    required this.variant,
    this.icon,
  });

  /// Create badge from a listing/inspection status string.
  factory StatusBadge.fromStatus(String status) {
    final normalized = status.toLowerCase().trim();
    BadgeVariant variant;
    String displayLabel;
    IconData? icon;

    switch (normalized) {
      case 'published':
        variant = BadgeVariant.success;
        displayLabel = 'Published';
        icon = Icons.check_circle_outline_rounded;
      case 'approved':
        variant = BadgeVariant.success;
        displayLabel = 'Approved';
        icon = Icons.check_circle_outline_rounded;
      case 'completed':
        variant = BadgeVariant.success;
        displayLabel = 'Completed';
        icon = Icons.check_circle_outline_rounded;
      case 'grade a':
        variant = BadgeVariant.success;
        displayLabel = 'Grade A ✓';
        icon = Icons.verified_outlined;
      case 'pendingapproval':
      case 'pending':
        variant = BadgeVariant.pending;
        displayLabel = 'Pending';
        icon = Icons.schedule_rounded;
      case 'scheduled':
        variant = BadgeVariant.info;
        displayLabel = 'Scheduled';
        icon = Icons.event_available_rounded;
      case 'draft':
        variant = BadgeVariant.neutral;
        displayLabel = 'Draft';
        icon = Icons.edit_note_rounded;
      case 'withdrawn':
        variant = BadgeVariant.neutral;
        displayLabel = 'Withdrawn';
        icon = Icons.archive_outlined;
      case 'soldout':
        variant = BadgeVariant.neutral;
        displayLabel = 'Sold Out';
        icon = Icons.remove_circle_outline_rounded;
      case 'grade b':
        variant = BadgeVariant.warning;
        displayLabel = 'Grade B';
        icon = Icons.star_half_rounded;
      case 'grade c':
        variant = BadgeVariant.error;
        displayLabel = 'Grade C';
        icon = Icons.star_border_rounded;
      case 'rejected':
      case 'cancelled':
        variant = BadgeVariant.error;
        displayLabel = status;
        icon = Icons.cancel_outlined;
      case 'discrepancy':
        variant = BadgeVariant.warning;
        displayLabel = '⚠ Discrepancy';
        icon = Icons.warning_amber_rounded;
      case 'not inspected':
        variant = BadgeVariant.neutral;
        displayLabel = 'Not Inspected';
        icon = Icons.hourglass_empty_rounded;
      default:
        variant = BadgeVariant.neutral;
        displayLabel = status;
        icon = null;
    }

    return StatusBadge(label: displayLabel, variant: variant, icon: icon);
  }

  /// Create badge from a confirmed grade string.
  factory StatusBadge.fromGrade(String grade) {
    return StatusBadge.fromStatus(grade);
  }

  /// Create badge from a listing status string.
  factory StatusBadge.fromListingStatus(String status) {
    return StatusBadge.fromStatus(status);
  }

  @override
  Widget build(BuildContext context) {
    final (Color bg, Color fg, Color border) = _resolveColors();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(AppRadius.pill),
        border: Border.all(color: border, width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 13, color: fg),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: GoogleFonts.inter(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: fg,
              letterSpacing: 0.1,
            ),
          ),
        ],
      ),
    );
  }

  (Color, Color, Color) _resolveColors() {
    return switch (variant) {
      BadgeVariant.success => (
        AppColors.statusSuccessBg,
        AppColors.statusSuccess,
        AppColors.statusSuccessBorder,
      ),
      BadgeVariant.pending => (
        AppColors.statusPendingBg,
        AppColors.statusPending,
        AppColors.statusPendingBorder,
      ),
      BadgeVariant.warning => (
        AppColors.statusWarningBg,
        AppColors.statusWarning,
        AppColors.statusWarningBorder,
      ),
      BadgeVariant.error => (
        AppColors.statusErrorBg,
        AppColors.statusError,
        AppColors.statusErrorBorder,
      ),
      BadgeVariant.info => (
        AppColors.statusInfoBg,
        AppColors.statusInfo,
        AppColors.statusInfoBorder,
      ),
      BadgeVariant.neutral => (
        AppColors.statusNeutralBg,
        AppColors.statusNeutral,
        AppColors.statusNeutralBorder,
      ),
    };
  }
}
