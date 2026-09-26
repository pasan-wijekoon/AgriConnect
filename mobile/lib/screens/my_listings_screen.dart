import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../models/inspection_models.dart';
import '../providers/inspection_provider.dart';
import '../theme/app_theme.dart';
import '../widgets/empty_state.dart';
import '../widgets/error_state.dart';
import '../widgets/listing_card.dart';
import '../widgets/loading_state.dart';
import 'listing_detail_screen.dart';

// ─────────────────────────────────────────────────────────────────────────────
// MyListingsScreen — Main Component C screen for Farmer
// Implements FR11 & FR13: View own produce listings and inspection statuses
// Design.md §22 — Search and Filters: clean, easy to remove, clearly labelled
// ─────────────────────────────────────────────────────────────────────────────

class MyListingsScreen extends StatefulWidget {
  const MyListingsScreen({super.key});

  @override
  State<MyListingsScreen> createState() => _MyListingsScreenState();
}

class _MyListingsScreenState extends State<MyListingsScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _selectedFilter = 'all'; // 'all', 'inspected', 'pending', 'discrepancy'

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<InspectionProvider>();
      if (provider.listingsState == LoadState.idle) {
        provider.loadMyListings();
      }
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  List<ListingSummary> _applyFilterChips(List<ListingSummary> listings) {
    switch (_selectedFilter) {
      case 'inspected':
        return listings.where((l) => l.hasBeenInspected).toList();
      case 'pending':
        return listings.where((l) => !l.hasBeenInspected).toList();
      case 'discrepancy':
        return listings.where((l) => l.hasUnresolvedDiscrepancy).toList();
      case 'all':
      default:
        return listings;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          'My Listings',
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
            onPressed: () => context.read<InspectionProvider>().refreshListings(),
          ),
        ],
      ),
      body: Consumer<InspectionProvider>(
        builder: (context, provider, _) {
          return Column(
            children: [
              // ── Search & Filter header ─────────────────────────────────
              _buildSearchAndFilters(provider),

              // ── Listings Content ───────────────────────────────────────
              Expanded(
                child: _buildBody(provider),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSearchAndFilters(InspectionProvider provider) {
    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(
          bottom: BorderSide(color: AppColors.borderLight, width: 1),
        ),
      ),
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.base,
        AppSpacing.md,
        AppSpacing.base,
        AppSpacing.sm,
      ),
      child: Column(
        children: [
          // Search input
          TextField(
            controller: _searchController,
            onChanged: provider.updateSearch,
            style: GoogleFonts.inter(fontSize: 14, color: AppColors.textPrimary),
            decoration: InputDecoration(
              hintText: 'Search by crop, region, or grade…',
              prefixIcon: const Icon(Icons.search_rounded, color: AppColors.textSecondary, size: 20),
              suffixIcon: provider.searchQuery.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear_rounded, size: 18, color: AppColors.textSecondary),
                      onPressed: () {
                        _searchController.clear();
                        provider.clearSearch();
                      },
                    )
                  : null,
              contentPadding: const EdgeInsets.symmetric(
                vertical: AppSpacing.sm,
                horizontal: AppSpacing.md,
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.sm),

          // Filter chips
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _buildCustomFilterChip('all', 'All'),
                const SizedBox(width: AppSpacing.sm),
                _buildCustomFilterChip('inspected', 'Inspected'),
                const SizedBox(width: AppSpacing.sm),
                _buildCustomFilterChip('pending', 'Pending'),
                const SizedBox(width: AppSpacing.sm),
                _buildCustomFilterChip('discrepancy', 'Discrepancies'),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCustomFilterChip(String value, String label) {
    final isSelected = _selectedFilter == value;
    return GestureDetector(
      onTap: () => setState(() => _selectedFilter = value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primaryGreen : AppColors.background,
          borderRadius: BorderRadius.circular(AppRadius.pill),
          border: Border.all(
            color: isSelected ? AppColors.primaryGreen : AppColors.borderLight,
            width: 1,
          ),
        ),
        child: Text(
          label,
          style: GoogleFonts.inter(
            fontSize: 12,
            fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
            color: isSelected ? Colors.white : AppColors.textSecondary,
          ),
        ),
      ),
    );
  }

  Widget _buildBody(InspectionProvider provider) {
    if (provider.listingsState == LoadState.loading) {
      return const LoadingListSkeleton(count: 4);
    }

    if (provider.listingsState == LoadState.error) {
      return Center(
        child: ErrorState(
          title: 'Unable to load listings',
          description: provider.listingsError ?? 'An unexpected error occurred.',
          onRetry: provider.loadMyListings,
        ),
      );
    }

    final filteredList = _applyFilterChips(provider.listings);

    if (filteredList.isEmpty) {
      final isSearchingOrFiltering =
          provider.searchQuery.isNotEmpty || _selectedFilter != 'all';
      return EmptyState(
        title: isSearchingOrFiltering ? 'No matching listings' : 'No Listings Yet',
        description: isSearchingOrFiltering
            ? 'Try changing your search keywords or filter selection.'
            : 'Your produce listings will appear here once you create them.',
        icon: Icons.inventory_2_outlined,
        buttonText: isSearchingOrFiltering ? 'Clear Filters' : null,
        onAction: isSearchingOrFiltering
            ? () {
                _searchController.clear();
                provider.clearSearch();
                setState(() => _selectedFilter = 'all');
              }
            : null,
      );
    }

    return RefreshIndicator(
      color: AppColors.primaryGreen,
      onRefresh: provider.refreshListings,
      child: ListView.builder(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(AppSpacing.base),
        itemCount: filteredList.length,
        itemBuilder: (context, index) {
          final listing = filteredList[index];
          return ListingCard(
            listing: listing,
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => ListingDetailScreen(listingId: listing.id),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
