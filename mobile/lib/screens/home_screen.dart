import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../providers/inspection_provider.dart';
import '../theme/app_theme.dart';
import '../widgets/app_button.dart';
import '../widgets/app_card.dart';
import '../widgets/loading_state.dart';
import '../widgets/error_state.dart';
import 'my_listings_screen.dart';

// ─────────────────────────────────────────────────────────────────────────────
// HomeScreen — Farmer dashboard summary
// Design.md §21 — Dashboard Cards: number visually dominant
// ─────────────────────────────────────────────────────────────────────────────

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<InspectionProvider>();
      if (provider.statsState == LoadState.idle) {
        provider.loadDashboardStats();
      }
      if (provider.listingsState == LoadState.idle) {
        provider.loadMyListings();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          'AgriConnect',
          style: GoogleFonts.inter(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            color: Colors.white,
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_none_rounded, color: Colors.white),
            onPressed: () {
              // Notifications
            },
          ),
        ],
      ),
      body: Consumer<InspectionProvider>(
        builder: (context, provider, _) {
          return RefreshIndicator(
            color: AppColors.primaryGreen,
            onRefresh: provider.refreshListings,
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(AppSpacing.base),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Greeting ─────────────────────────────────────────
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'Hello, Sunil! 👋',
                    style: GoogleFonts.inter(
                      fontSize: 24,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                      letterSpacing: -0.5,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    "Here's your produce & inspection overview.",
                    style: GoogleFonts.inter(
                      fontSize: 14,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xl),

                  // ── Stats row ─────────────────────────────────────────
                  if (provider.statsState == LoadState.loading)
                    const LoadingState(message: 'Loading stats…')
                  else if (provider.statsState == LoadState.error)
                    ErrorState(
                      title: 'Unable to load stats',
                      description: provider.statsError ?? '',
                      onRetry: provider.loadDashboardStats,
                    )
                  else ...[
                    _buildStatsGrid(provider),
                    const SizedBox(height: AppSpacing.xl),
                    _buildInspectionSummaryCard(context, provider),
                    const SizedBox(height: AppSpacing.xl),
                    _buildQuickActions(context),
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildStatsGrid(InspectionProvider provider) {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      crossAxisSpacing: AppSpacing.md,
      mainAxisSpacing: AppSpacing.md,
      childAspectRatio: 1.35,
      children: [
        _StatCard(
          label: 'My Listings',
          value: provider.listings.length.toString(),
          icon: Icons.inventory_2_outlined,
          color: AppColors.primaryGreen,
        ),
        _StatCard(
          label: 'Published',
          value: provider.listings.where((l) => l.status == 'Published').length.toString(),
          icon: Icons.check_circle_outline_rounded,
          color: AppColors.statusSuccess,
        ),
        _StatCard(
          label: 'Pending Inspection',
          value: provider.pendingInspectionCount.toString(),
          icon: Icons.hourglass_empty_rounded,
          color: AppColors.statusPending,
        ),
        _StatCard(
          label: 'Discrepancies',
          value: provider.activeDiscrepancyCount.toString(),
          icon: Icons.warning_amber_rounded,
          color: AppColors.statusWarning,
        ),
      ],
    );
  }

  Widget _buildInspectionSummaryCard(BuildContext context, InspectionProvider provider) {
    final hasIssues = provider.activeDiscrepancyCount > 0;
    final hasPending = provider.pendingInspectionCount > 0;

    String statusTitle;
    String statusMessage;
    Color statusColor;
    IconData statusIcon;

    if (hasIssues) {
      statusTitle = 'Action Required';
      statusMessage = '${provider.activeDiscrepancyCount} listing(s) have an unresolved grade discrepancy. '
          'Contact the collection centre for resolution.';
      statusColor = AppColors.statusWarning;
      statusIcon = Icons.warning_amber_rounded;
    } else if (hasPending) {
      statusTitle = 'Awaiting Inspection';
      statusMessage = '${provider.pendingInspectionCount} listing(s) are waiting for quality inspection '
          'by a collection-centre officer.';
      statusColor = AppColors.statusPending;
      statusIcon = Icons.hourglass_empty_rounded;
    } else {
      statusTitle = 'All Listings In Order';
      statusMessage = 'No pending inspections or grade discrepancies. Your listings are up to date.';
      statusColor = AppColors.statusSuccess;
      statusIcon = Icons.check_circle_outline_rounded;
    }

    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(statusIcon, size: 20, color: statusColor),
              const SizedBox(width: AppSpacing.sm),
              Text(
                statusTitle,
                style: GoogleFonts.inter(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            statusMessage,
            style: GoogleFonts.inter(
              fontSize: 13,
              color: AppColors.textSecondary,
              height: 1.5,
            ),
          ),
          const SizedBox(height: AppSpacing.base),
          AppButton(
            text: 'View My Listings',
            variant: AppButtonVariant.secondary,
            size: AppButtonSize.medium,
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const MyListingsScreen()),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildQuickActions(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'QUICK ACTIONS',
          style: GoogleFonts.inter(
            fontSize: 12,
            fontWeight: FontWeight.w600,
            color: AppColors.textSecondary,
            letterSpacing: 0.5,
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        _QuickActionTile(
          icon: Icons.inventory_2_outlined,
          label: 'My Listings',
          description: 'View your produce listings and inspection status',
          onTap: () {
            Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const MyListingsScreen()),
            );
          },
        ),
        const SizedBox(height: AppSpacing.sm),
        _QuickActionTile(
          icon: Icons.place_outlined,
          label: 'Find Collection Centre',
          description: 'Locate the nearest centre for drop-off',
          onTap: () {
            // Collection centre map (Component B)
          },
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Private widgets
// ─────────────────────────────────────────────────────────────────────────────

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;

  const _StatCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: color),
          const Spacer(),
          Text(
            value,
            style: GoogleFonts.inter(
              fontSize: 26,
              fontWeight: FontWeight.w700,
              color: color,
              letterSpacing: -0.5,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            label,
            style: GoogleFonts.inter(
              fontSize: 12,
              fontWeight: FontWeight.w500,
              color: AppColors.textSecondary,
            ),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

class _QuickActionTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String description;
  final VoidCallback onTap;

  const _QuickActionTile({
    required this.icon,
    required this.label,
    required this.description,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.base),
      onTap: onTap,
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: AppColors.primaryGreenLight,
              borderRadius: BorderRadius.circular(AppRadius.control),
              border: Border.all(color: AppColors.primaryGreen.withValues(alpha: 0.15), width: 1),
            ),
            child: Icon(icon, color: AppColors.primaryGreen, size: 22),
          ),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: GoogleFonts.inter(
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  description,
                  style: GoogleFonts.inter(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          const Icon(Icons.arrow_forward_rounded, size: 16, color: AppColors.textMuted),
        ],
      ),
    );
  }
}
