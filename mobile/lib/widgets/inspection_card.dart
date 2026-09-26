import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';
import '../models/inspection_models.dart';
import '../theme/app_theme.dart';
import 'app_card.dart';
import 'status_badge.dart';

// ─────────────────────────────────────────────────────────────────────────────
// InspectionCard — Bespoke inspection record card for history list
// Used in listing_detail_screen.dart inspection history section (Design.md §14)
// ─────────────────────────────────────────────────────────────────────────────

class InspectionCard extends StatelessWidget {
  final InspectionRecord inspection;
  final VoidCallback onTap;

  const InspectionCard({
    super.key,
    required this.inspection,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final date = DateTime.tryParse(inspection.inspectedAt);
    final dateLabel = date != null
        ? DateFormat('dd MMM yyyy · HH:mm').format(date.toLocal())
        : inspection.inspectedAt;

    return AppCard(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      padding: const EdgeInsets.all(AppSpacing.base),
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Header: date + grade badge ────────────────────────────────
          Row(
            children: [
              const Icon(
                Icons.calendar_today_rounded,
                size: 14,
                color: AppColors.textMuted,
              ),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  dateLabel,
                  style: GoogleFonts.inter(
                    fontSize: 13,
                    fontWeight: FontWeight.w500,
                    color: AppColors.textSecondary,
                  ),
                ),
              ),
              StatusBadge.fromGrade(inspection.confirmedGrade),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),

          // ── Officer info ──────────────────────────────────────────────
          Row(
            children: [
              const Icon(
                Icons.person_outline_rounded,
                size: 14,
                color: AppColors.textMuted,
              ),
              const SizedBox(width: 6),
              Text(
                'Officer: ${inspection.officerName}',
                style: GoogleFonts.inter(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),

          // ── Discrepancy notice ────────────────────────────────────────
          if (inspection.hasDiscrepancy) ...[
            const SizedBox(height: AppSpacing.sm),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: AppColors.statusWarningBg,
                borderRadius: BorderRadius.circular(AppRadius.control),
                border: Border.all(color: AppColors.statusWarningBorder, width: 1),
              ),
              child: Row(
                children: [
                  const Icon(
                    Icons.warning_amber_rounded,
                    size: 14,
                    color: AppColors.statusWarning,
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      'Grade discrepancy noted (${inspection.claimedGrade} → ${inspection.confirmedGrade})',
                      style: GoogleFonts.inter(
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                        color: AppColors.statusWarning,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],

          // ── Footer tap hint ───────────────────────────────────────────
          const SizedBox(height: AppSpacing.sm),
          const Divider(height: 1, color: AppColors.borderLight),
          const SizedBox(height: AppSpacing.sm),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'View Inspection',
                style: GoogleFonts.inter(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.primaryGreen,
                ),
              ),
              const SizedBox(width: 4),
              const Icon(
                Icons.arrow_forward_rounded,
                size: 12,
                color: AppColors.primaryGreen,
              ),
            ],
          ),
        ],
      ),
    );
  }
}
