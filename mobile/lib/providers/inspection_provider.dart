import 'package:flutter/foundation.dart';
import '../models/inspection_models.dart';
import '../services/api_service.dart';

// ─────────────────────────────────────────────────────────────────────────────
// InspectionProvider — Component C state management
// ADR decision 2: Provider + ChangeNotifier pattern
// ─────────────────────────────────────────────────────────────────────────────

enum LoadState { idle, loading, loaded, error }

class InspectionProvider extends ChangeNotifier {
  // ── Dashboard stats ───────────────────────────────────────────────────────

  QualityDashboardStats? _stats;
  LoadState _statsState = LoadState.idle;
  String? _statsError;

  QualityDashboardStats? get stats => _stats;
  LoadState get statsState => _statsState;
  String? get statsError => _statsError;

  // ── Farmer's listings ─────────────────────────────────────────────────────

  List<ListingSummary> _listings = [];
  List<ListingSummary> _filteredListings = [];
  LoadState _listingsState = LoadState.idle;
  String? _listingsError;
  String _searchQuery = '';

  List<ListingSummary> get listings => _filteredListings;
  LoadState get listingsState => _listingsState;
  String? get listingsError => _listingsError;
  String get searchQuery => _searchQuery;

  int get pendingInspectionCount =>
      _listings.where((l) => l.status == 'PendingApproval' && !l.hasBeenInspected).length;

  int get activeDiscrepancyCount =>
      _listings.where((l) => l.hasUnresolvedDiscrepancy).length;

  // ── Per-listing inspections ───────────────────────────────────────────────

  final Map<String, List<InspectionRecord>> _inspectionsByListing = {};
  final Map<String, LoadState> _inspectionLoadStates = {};
  final Map<String, String> _inspectionErrors = {};

  List<InspectionRecord> getListingInspections(String listingId) =>
      _inspectionsByListing[listingId] ?? [];

  LoadState getInspectionLoadState(String listingId) =>
      _inspectionLoadStates[listingId] ?? LoadState.idle;

  String? getInspectionError(String listingId) =>
      _inspectionErrors[listingId];

  // ── Actions ───────────────────────────────────────────────────────────────

  /// Load dashboard summary stats.
  Future<void> loadDashboardStats() async {
    if (_statsState == LoadState.loading) return;
    _statsState = LoadState.loading;
    _statsError = null;
    notifyListeners();

    try {
      _stats = await ApiService.getDashboardStats();
      _statsState = LoadState.loaded;
    } catch (e) {
      _statsState = LoadState.error;
      _statsError = 'Unable to load dashboard stats. Please try again.';
    }
    notifyListeners();
  }

  /// Load the farmer's own listings (FR11, FR13).
  Future<void> loadMyListings() async {
    if (_listingsState == LoadState.loading) return;
    _listingsState = LoadState.loading;
    _listingsError = null;
    notifyListeners();

    try {
      _listings = await ApiService.getMyListings();
      _applyFilter();
      _listingsState = LoadState.loaded;
    } catch (e) {
      _listingsState = LoadState.error;
      _listingsError = 'Unable to load your listings. Please try again.';
    }
    notifyListeners();
  }

  /// Refresh listings (pull-to-refresh).
  Future<void> refreshListings() async {
    _listingsState = LoadState.idle;
    await loadMyListings();
    // Also refresh stats
    _statsState = LoadState.idle;
    await loadDashboardStats();
  }

  /// Update search query and refilter listings.
  void updateSearch(String query) {
    _searchQuery = query;
    _applyFilter();
    notifyListeners();
  }

  /// Clear search filter.
  void clearSearch() {
    _searchQuery = '';
    _applyFilter();
    notifyListeners();
  }

  void _applyFilter() {
    if (_searchQuery.isEmpty) {
      _filteredListings = List<ListingSummary>.from(_listings);
      return;
    }
    final q = _searchQuery.toLowerCase();
    _filteredListings = _listings.where((l) {
      return l.cropName.toLowerCase().contains(q) ||
          l.regionName.toLowerCase().contains(q) ||
          l.claimedGrade.toLowerCase().contains(q);
    }).toList();
  }

  /// Load inspection history for a specific listing (FR13).
  Future<void> loadListingInspections(String listingId) async {
    if (_inspectionLoadStates[listingId] == LoadState.loading) return;
    _inspectionLoadStates[listingId] = LoadState.loading;
    _inspectionErrors.remove(listingId);
    notifyListeners();

    try {
      _inspectionsByListing[listingId] = await ApiService.getListingInspections(listingId);
      _inspectionLoadStates[listingId] = LoadState.loaded;
    } catch (e) {
      _inspectionLoadStates[listingId] = LoadState.error;
      _inspectionErrors[listingId] = 'Unable to load inspection history.';
    }
    notifyListeners();
  }

  /// Refresh inspections for a specific listing.
  Future<void> refreshListingInspections(String listingId) async {
    _inspectionLoadStates[listingId] = LoadState.idle;
    await loadListingInspections(listingId);
  }

  /// Get a single listing by ID.
  ListingSummary? getListingById(String listingId) {
    try {
      return _listings.firstWhere((l) => l.id == listingId);
    } catch (_) {
      return null;
    }
  }
}
