import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/inspection_models.dart';
import '../theme/app_theme.dart';
import 'app_card.dart';
import 'status_badge.dart';

// ─────────────────────────────────────────────────────────────────────────────
// ListingCard — Farmer's produce listing summary card
// Design.md §14 — Cards: white bg, 12px radius, subtle border, consistent padding
// ─────────────────────────────────────────────────────────────────────────────

class ListingCard extends StatelessWidget {
  final ListingSummary listing;
  final VoidCallback onTap;

  const ListingCard({
    super.key,
    required this.listing,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
      padding: EdgeInsets.zero,
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Top section: crop image, metadata, status badge ─────────────
          Padding(
            padding: const EdgeInsets.all(AppSpacing.base),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Crop photo thumbnail
                _buildCropImage(),
                const SizedBox(width: AppSpacing.md),
                // Crop info
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Crop name + status badge
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Text(
                              listing.cropName,
                              style: GoogleFonts.inter(
                                fontSize: 15,
                                fontWeight: FontWeight.w600,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ),
                          const SizedBox(width: AppSpacing.sm),
                          StatusBadge.fromStatus(listing.status),
                        ],
                      ),
                      const SizedBox(height: 4),
                      // Region · Quantity
                      Text(
                        '${listing.regionName} · ${listing.quantity.toStringAsFixed(0)} ${listing.unit}',
                        style: GoogleFonts.inter(
                          fontSize: 13,
                          color: AppColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: AppSpacing.sm),
                      // Inspection status row
                      _buildInspectionStatus(),
                    ],
                  ),
                ),
              ],
            ),
          ),

          // ── Discrepancy warning banner ──────────────────────────────────
          if (listing.hasUnresolvedDiscrepancy) _buildDiscrepancyBanner(),

          // ── Footer: view details hint ───────────────────────────────────
          Container(
            decoration: const BoxDecoration(
              border: Border(top: BorderSide(color: AppColors.borderLight, width: 1)),
            ),
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.base,
              vertical: AppSpacing.sm,
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Min Price: LKR ${listing.minPrice?.toStringAsFixed(2) ?? '—'} / ${listing.unit}',
                  style: GoogleFonts.inter(
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                    color: AppColors.textSecondary,
                  ),
                ),
                Row(
                  children: [
                    Text(
                      'View Details',
                      style: GoogleFonts.inter(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.primaryGreen,
                      ),
                    ),
                    const SizedBox(width: 4),
                    const Icon(
                      Icons.arrow_forward_rounded,
                      size: 14,
                      color: AppColors.primaryGreen,
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCropImage() {
    if (listing.listingPhotos.isNotEmpty) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(AppRadius.control),
        child: CachedNetworkImage(
          imageUrl: listing.listingPhotos.first,
          width: 72,
          height: 72,
          fit: BoxFit.cover,
          placeholder: (context, url) => _imagePlaceholder(),
          errorWidget: (context, url, error) => _imagePlaceholder(),
        ),
      );
    }
    return _imagePlaceholder();
  }

  Widget _imagePlaceholder() {
    return Container(
      width: 72,
      height: 72,
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant,
        borderRadius: BorderRadius.circular(AppRadius.control),
        border: Border.all(color: AppColors.borderLight, width: 1),
      ),
      child: const Icon(
        Icons.eco_rounded,
        color: AppColors.secondaryGreen,
        size: 32,
      ),
    );
  }

  Widget _buildInspectionStatus() {
    if (!listing.hasBeenInspected) {
      return Row(
        children: [
          const Icon(Icons.hourglass_empty_rounded, size: 14, color: AppColors.textMuted),
          const SizedBox(width: 4),
          Text(
            'No inspection yet',
            style: GoogleFonts.inter(fontSize: 12, color: AppColors.textMuted),
          ),
        ],
      );
    }

    if (listing.hasUnresolvedDiscrepancy) {
      return Row(
        children: [
          Text(
            'Claimed: ${listing.claimedGrade}',
            style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
          ),
          const Text(' → ', style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
          Text(
            'Confirmed: ${listing.latestConfirmedGrade ?? "—"}',
            style: GoogleFonts.inter(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: AppColors.statusWarning,
            ),
          ),
        ],
      );
    }

    return Row(
      children: [
        const Icon(Icons.check_circle_outline_rounded, size: 14, color: AppColors.statusSuccess),
        const SizedBox(width: 4),
        Text(
          'Inspection: ${listing.latestConfirmedGrade ?? listing.claimedGrade}',
          style: GoogleFonts.inter(
            fontSize: 12,
            fontWeight: FontWeight.w600,
            color: AppColors.statusSuccess,
          ),
        ),
      ],
    );
  }

  Widget _buildDiscrepancyBanner() {
    return Container(
      margin: const EdgeInsets.only(left: AppSpacing.base, right: AppSpacing.base, bottom: AppSpacing.sm),
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: AppColors.statusWarningBg,
        borderRadius: BorderRadius.circular(AppRadius.control),
        border: Border.all(color: AppColors.statusWarningBorder, width: 1),
      ),
      child: Row(
        children: [
          const Icon(Icons.warning_amber_rounded, size: 16, color: AppColors.statusWarning),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Text(
              'Grade discrepancy flagged — contact the collection centre',
              style: GoogleFonts.inter(
                fontSize: 12,
                fontWeight: FontWeight.w500,
                color: AppColors.statusWarning,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
