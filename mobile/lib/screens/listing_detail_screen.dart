import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../models/inspection_models.dart';
import '../providers/inspection_provider.dart';
import '../theme/app_theme.dart';
import '../widgets/app_card.dart';
import '../widgets/error_state.dart';
import '../widgets/info_row.dart';
import '../widgets/inspection_card.dart';
import '../widgets/loading_state.dart';
import '../widgets/status_badge.dart';
import 'inspection_detail_screen.dart';

// ─────────────────────────────────────────────────────────────────────────────
// ListingDetailScreen — Per-listing inspection status and history (FR11, FR13)
// Design.md §14 & §31 & §32
// ─────────────────────────────────────────────────────────────────────────────

class ListingDetailScreen extends StatefulWidget {
  final String listingId;

  const ListingDetailScreen({
    super.key,
    required this.listingId,
  });

  @override
  State<ListingDetailScreen> createState() => _ListingDetailScreenState();
}

class _ListingDetailScreenState extends State<ListingDetailScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<InspectionProvider>();
      provider.loadListingInspections(widget.listingId);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<InspectionProvider>(
      builder: (context, provider, _) {
        final listing = provider.getListingById(widget.listingId);

        if (listing == null) {
          if (provider.listingsState == LoadState.loading) {
            return const Scaffold(
              backgroundColor: AppColors.background,
              body: Center(child: LoadingState(message: 'Loading listing details…')),
            );
          }
          return Scaffold(
            backgroundColor: AppColors.background,
            appBar: AppBar(
              title: Text(
                'Listing Detail',
                style: GoogleFonts.inter(
                  fontSize: 18,
                  fontWeight: FontWeight.w600,
                  color: Colors.white,
                ),
              ),
            ),
            body: Center(
              child: ErrorState(
                title: 'Listing Not Found',
                description: 'The requested produce listing could not be located.',
                onRetry: () => provider.loadMyListings(),
              ),
            ),
          );
        }

        final inspections = provider.getListingInspections(widget.listingId);
        final inspectionLoadState = provider.getInspectionLoadState(widget.listingId);
        final inspectionError = provider.getInspectionError(widget.listingId);

        return Scaffold(
          backgroundColor: AppColors.background,
          appBar: AppBar(
            title: Text(
              listing.cropName,
              style: GoogleFonts.inter(
                fontSize: 18,
                fontWeight: FontWeight.w600,
                color: Colors.white,
              ),
            ),
            actions: [
              IconButton(
                icon: const Icon(Icons.refresh_rounded, color: Colors.white),
                tooltip: 'Refresh',
                onPressed: () => provider.refreshListingInspections(widget.listingId),
              ),
            ],
          ),
          body: RefreshIndicator(
            color: AppColors.primaryGreen,
            onRefresh: () async {
              await provider.refreshListingInspections(widget.listingId);
              await provider.refreshListings();
            },
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(AppSpacing.base),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Discrepancy Alert Banner ───────────────────────────
                  if (listing.hasUnresolvedDiscrepancy) ...[
                    _buildDiscrepancyBanner(listing),
                    const SizedBox(height: AppSpacing.base),
                  ],

                  // ── Listing Hero & Overview ────────────────────────────
                  _buildListingOverviewCard(listing),
                  const SizedBox(height: AppSpacing.base),

                  // ── Quality & Inspection Summary ───────────────────────
                  _buildQualitySummaryCard(listing),
                  const SizedBox(height: AppSpacing.base),

                  // ── Inspection History Section ─────────────────────────
                  _buildInspectionHistorySection(
                    context,
                    inspections,
                    inspectionLoadState,
                    inspectionError,
                    provider,
                  ),
                  const SizedBox(height: AppSpacing.xl),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _buildDiscrepancyBanner(ListingSummary listing) {
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
                  'Quality Grade Discrepancy Flagged',
                  style: GoogleFonts.inter(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.statusWarning,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Your claimed grade (${listing.claimedGrade}) was inspected and updated to ${listing.latestConfirmedGrade ?? "a different grade"} by the agricultural officer. Please review the inspection notes or contact the collection center.',
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

  Widget _buildListingOverviewCard(ListingSummary listing) {
    final start = DateTime.tryParse(listing.pickupWindowStart);
    final end = DateTime.tryParse(listing.pickupWindowEnd);
    final pickupRange = (start != null && end != null)
        ? '${DateFormat('dd MMM').format(start)} – ${DateFormat('dd MMM yyyy').format(end)}'
        : '${listing.pickupWindowStart} – ${listing.pickupWindowEnd}';

    return AppCard(
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Image gallery / banner if photo available
          if (listing.listingPhotos.isNotEmpty)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(AppRadius.card),
              ),
              child: CachedNetworkImage(
                imageUrl: listing.listingPhotos.first,
                height: 180,
                width: double.infinity,
                fit: BoxFit.cover,
                placeholder: (context, url) => Container(
                  height: 180,
                  color: AppColors.surfaceVariant,
                  child: const Center(
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppColors.primaryGreen,
                    ),
                  ),
                ),
                errorWidget: (context, url, error) => Container(
                  height: 180,
                  color: AppColors.surfaceVariant,
                  child: const Icon(Icons.eco_rounded, size: 48, color: AppColors.secondaryGreen),
                ),
              ),
            ),

          Padding(
            padding: const EdgeInsets.all(AppSpacing.base),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        listing.cropName,
                        style: GoogleFonts.inter(
                          fontSize: 20,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                    StatusBadge.fromListingStatus(listing.status),
                  ],
                ),
                const SizedBox(height: AppSpacing.sm),
                Row(
                  children: [
                    const Icon(Icons.location_on_outlined, size: 16, color: AppColors.textSecondary),
                    const SizedBox(width: 4),
                    Text(
                      listing.regionName,
                      style: GoogleFonts.inter(
                        fontSize: 14,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),
                const Divider(height: 1, color: AppColors.borderLight),
                const SizedBox(height: AppSpacing.md),

                // Specs
                InfoRow(
                  label: 'Quantity',
                  value: '${listing.quantity.toStringAsFixed(0)} ${listing.unit}',
                  icon: Icons.scale_outlined,
                ),
                InfoRow(
                  label: 'Category',
                  value: listing.category,
                  icon: Icons.category_outlined,
                ),
                InfoRow(
                  label: 'Claimed Grade',
                  valueWidget: StatusBadge.fromGrade(listing.claimedGrade),
                  icon: Icons.verified_outlined,
                ),
                InfoRow(
                  label: 'Pickup Window',
                  value: pickupRange,
                  icon: Icons.date_range_outlined,
                  showDivider: false,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQualitySummaryCard(ListingSummary listing) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Quality & Grading Summary',
            style: GoogleFonts.inter(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.md),

          Row(
            children: [
              // Claimed
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
                      StatusBadge.fromGrade(listing.claimedGrade),
                    ],
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              // Latest Confirmed
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
                        'Confirmed Grade',
                        style: GoogleFonts.inter(fontSize: 12, color: AppColors.textSecondary),
                      ),
                      const SizedBox(height: 4),
                      if (listing.latestConfirmedGrade != null)
                        StatusBadge.fromGrade(listing.latestConfirmedGrade!)
                      else
                        Text(
                          'Pending Inspection',
                          style: GoogleFonts.inter(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: AppColors.statusPending,
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          InfoRow(
            label: 'Total Inspections',
            value: '${listing.inspectionCount}',
            icon: Icons.fact_check_outlined,
          ),
          InfoRow(
            label: 'Discrepancy Status',
            value: listing.hasUnresolvedDiscrepancy ? 'Discrepancy Flagged' : 'None (In Order)',
            icon: Icons.shield_outlined,
            textColor: listing.hasUnresolvedDiscrepancy
                ? AppColors.statusWarning
                : AppColors.statusSuccess,
            showDivider: false,
          ),
        ],
      ),
    );
  }

  Widget _buildInspectionHistorySection(
    BuildContext context,
    List<InspectionRecord> inspections,
    LoadState loadState,
    String? error,
    InspectionProvider provider,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              'Inspection History',
              style: GoogleFonts.inter(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
            if (inspections.isNotEmpty)
              Text(
                '${inspections.length} record${inspections.length > 1 ? 's' : ''}',
                style: GoogleFonts.inter(
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
              ),
          ],
        ),
        const SizedBox(height: AppSpacing.md),

        if (loadState == LoadState.loading)
          const LoadingListSkeleton(count: 2)
        else if (loadState == LoadState.error)
          ErrorState(
            title: 'Unable to load history',
            description: error ?? 'Could not retrieve inspection records.',
            onRetry: () => provider.loadListingInspections(widget.listingId),
          )
        else if (inspections.isEmpty)
          AppCard(
            padding: const EdgeInsets.all(AppSpacing.xl),
            child: Center(
              child: Column(
                children: [
                  const Icon(
                    Icons.assignment_turned_in_outlined,
                    size: 40,
                    color: AppColors.textMuted,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'No Inspections Recorded Yet',
                    style: GoogleFonts.inter(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'An agricultural officer will perform quality inspection once the batch is received at the collection hub.',
                    textAlign: TextAlign.center,
                    style: GoogleFonts.inter(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
          )
        else
          ...inspections.map(
            (inspection) => InspectionCard(
              inspection: inspection,
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => InspectionDetailScreen(
                      inspection: inspection,
                    ),
                  ),
                );
              },
            ),
          ),
      ],
    );
  }
}
