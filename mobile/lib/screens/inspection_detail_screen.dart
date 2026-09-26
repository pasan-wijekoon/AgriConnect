import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../models/inspection_models.dart';
import '../theme/app_theme.dart';
import '../widgets/app_card.dart';
import '../widgets/info_row.dart';
import '../widgets/status_badge.dart';

// ─────────────────────────────────────────────────────────────────────────────
// InspectionDetailScreen — Full individual inspection record (FR12, FR13)
// Design.md §14 & §31 & §32
// ─────────────────────────────────────────────────────────────────────────────

class InspectionDetailScreen extends StatelessWidget {
  final InspectionRecord inspection;

  const InspectionDetailScreen({
    super.key,
    required this.inspection,
  });

  void _showImageDialog(BuildContext context, String imageUrl, String tag) {
    showDialog(
      context: context,
      builder: (context) => Dialog(
        backgroundColor: Colors.transparent,
        insetPadding: const EdgeInsets.all(AppSpacing.base),
        child: Stack(
          alignment: Alignment.topRight,
          children: [
            InteractiveViewer(
              minScale: 0.8,
              maxScale: 3.5,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(AppRadius.container),
                child: CachedNetworkImage(
                  imageUrl: imageUrl,
                  fit: BoxFit.contain,
                  placeholder: (context, url) => Container(
                    height: 300,
                    color: Colors.black26,
                    child: const Center(
                      child: CircularProgressIndicator(color: Colors.white),
                    ),
                  ),
                  errorWidget: (context, url, error) => Container(
                    height: 300,
                    color: Colors.black54,
                    child: const Center(
                      child: Icon(Icons.broken_image_rounded, color: Colors.white, size: 48),
                    ),
                  ),
                ),
              ),
            ),
            Positioned(
              top: 8,
              right: 8,
              child: IconButton(
                icon: const Icon(Icons.close_rounded, color: Colors.white, size: 28),
                onPressed: () => Navigator.of(context).pop(),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final parsedDate = DateTime.tryParse(inspection.inspectedAt);
    final dateStr = parsedDate != null
        ? DateFormat('EEEE, dd MMMM yyyy · HH:mm').format(parsedDate.toLocal())
        : inspection.inspectedAt;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          'Inspection Record',
          style: GoogleFonts.inter(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            color: Colors.white,
          ),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppSpacing.base),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Discrepancy Notice Banner ──────────────────────────────
            if (inspection.hasDiscrepancy) ...[
              _buildDiscrepancyBanner(),
              const SizedBox(height: AppSpacing.base),
            ],

            // ── Quality Outcome Card ───────────────────────────────────
            _buildQualityOutcomeCard(dateStr),
            const SizedBox(height: AppSpacing.base),

            // ── Officer Notes Card ─────────────────────────────────────
            _buildOfficerNotesCard(),
            const SizedBox(height: AppSpacing.base),

            // ── Inspection Photos Card ─────────────────────────────────
            _buildPhotosCard(context),
            const SizedBox(height: AppSpacing.base),

            // ── Inspection Metadata & Officer Card ─────────────────────
            _buildMetadataCard(dateStr),
            const SizedBox(height: AppSpacing.xl),
          ],
        ),
      ),
    );
  }

  Widget _buildDiscrepancyBanner() {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.base),
      decoration: BoxDecoration(
        color: AppColors.statusWarningBg,
        borderRadius: BorderRadius.circular(AppRadius.card),
        border: Border.all(color: AppColors.statusWarningBorder, width: 1),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(
            Icons.warning_amber_rounded,
            color: AppColors.statusWarning,
            size: 24,
          ),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Grade Discrepancy Flagged',
                  style: GoogleFonts.inter(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.statusWarning,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'The officer confirmed Grade ${inspection.confirmedGrade}, differing from the claimed Grade ${inspection.claimedGrade}.',
                  style: GoogleFonts.inter(
                    fontSize: 13,
                    color: AppColors.textPrimary,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQualityOutcomeCard(String dateStr) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Quality Outcome',
                style: GoogleFonts.inter(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              StatusBadge.fromGrade(inspection.confirmedGrade),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          // Side-by-side comparison
          Row(
            children: [
              Expanded(
                child: Container(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  decoration: BoxDecoration(
                    color: AppColors.background,
                    borderRadius: BorderRadius.circular(AppRadius.control),
                    border: Border.all(color: AppColors.borderLight, width: 1),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Claimed Grade',
                        style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
                      ),
                      const SizedBox(height: 4),
                      StatusBadge.fromGrade(inspection.claimedGrade),
                    ],
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              const Icon(Icons.arrow_forward_rounded, color: AppColors.textMuted, size: 20),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: Container(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  decoration: BoxDecoration(
                    color: AppColors.background,
                    borderRadius: BorderRadius.circular(AppRadius.control),
                    border: Border.all(
                      color: inspection.hasDiscrepancy
                          ? AppColors.statusWarningBorder
                          : AppColors.borderLight,
                      width: 1,
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Confirmed Grade',
                        style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
                      ),
                      const SizedBox(height: 4),
                      StatusBadge.fromGrade(inspection.confirmedGrade),
                    ],
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          InfoRow(
            label: 'Crop & Batch',
            value: '${inspection.cropName} (${inspection.quantity.toStringAsFixed(0)} ${inspection.unit})',
            icon: Icons.eco_outlined,
          ),
          InfoRow(
            label: 'Discrepancy Detected',
            value: inspection.hasDiscrepancy ? 'Yes' : 'No',
            icon: Icons.flag_outlined,
            textColor: inspection.hasDiscrepancy
                ? AppColors.statusWarning
                : AppColors.statusSuccess,
            showDivider: false,
          ),
        ],
      ),
    );
  }

  Widget _buildOfficerNotesCard() {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.notes_rounded, size: 18, color: AppColors.primaryGreen),
              const SizedBox(width: AppSpacing.xs),
              Text(
                'Officer Notes & Observations',
                style: GoogleFonts.inter(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.base),
            decoration: BoxDecoration(
              color: AppColors.background,
              borderRadius: BorderRadius.circular(AppRadius.control),
              border: Border.all(color: AppColors.borderLight, width: 1),
            ),
            child: Text(
              (inspection.notes != null && inspection.notes!.trim().isNotEmpty)
                  ? inspection.notes!
                  : 'No specific notes recorded for this inspection.',
              style: GoogleFonts.inter(
                fontSize: 14,
                color: AppColors.textPrimary,
                height: 1.5,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPhotosCard(BuildContext context) {
    if (inspection.photos.isEmpty) {
      return AppCard(
        padding: const EdgeInsets.all(AppSpacing.base),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Inspection Photos',
              style: GoogleFonts.inter(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              'No inspection photos were attached to this record.',
              style: GoogleFonts.inter(fontSize: 13, color: AppColors.textSecondary),
            ),
          ],
        ),
      );
    }

    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Inspection Photos',
                style: GoogleFonts.inter(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              Text(
                '${inspection.photos.length} photo${inspection.photos.length > 1 ? 's' : ''}',
                style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          SizedBox(
            height: 120,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: inspection.photos.length,
              separatorBuilder: (context, index) => const SizedBox(width: AppSpacing.sm),
              itemBuilder: (context, index) {
                final photo = inspection.photos[index];
                return GestureDetector(
                  onTap: () => _showImageDialog(context, photo.url, photo.id),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(AppRadius.control),
                    child: CachedNetworkImage(
                      imageUrl: photo.url,
                      width: 140,
                      height: 120,
                      fit: BoxFit.cover,
                      placeholder: (context, url) => Container(
                        width: 140,
                        color: AppColors.surfaceVariant,
                        child: const Center(
                          child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.primaryGreen),
                        ),
                      ),
                      errorWidget: (context, url, error) => Container(
                        width: 140,
                        color: AppColors.surfaceVariant,
                        child: const Icon(Icons.broken_image_outlined, color: AppColors.textMuted),
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMetadataCard(String dateStr) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Officer & Inspection Details',
            style: GoogleFonts.inter(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          InfoRow(
            label: 'Inspecting Officer',
            value: inspection.officerName,
            icon: Icons.badge_outlined,
          ),
          InfoRow(
            label: 'Date & Time',
            value: dateStr,
            icon: Icons.calendar_today_outlined,
          ),
          InfoRow(
            label: 'Hub Region',
            value: inspection.regionName,
            icon: Icons.location_on_outlined,
          ),
          InfoRow(
            label: 'Listing Status',
            value: inspection.listingStatus,
            icon: Icons.info_outline_rounded,
            showDivider: false,
          ),
        ],
      ),
    );
  }
}
