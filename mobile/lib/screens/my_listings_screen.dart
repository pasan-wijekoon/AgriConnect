import 'package:flutter/material.dart';
import '../models/listing.dart';
import '../models/listing_inspection_status.dart';
import '../services/api_service.dart';
import '../widgets/listing_card.dart';
import 'listing_detail_screen.dart';

class MyListingsScreen extends StatefulWidget {
  final ApiService apiService;

  const MyListingsScreen({super.key, required this.apiService});

  @override
  State<MyListingsScreen> createState() => _MyListingsScreenState();
}

class _MyListingsScreenState extends State<MyListingsScreen> {
  List<Listing> _myListings = [];
  // Component C — Quality Grading & Inspection (FR12–FR14): a farmer's own
  // inspection/discrepancy status per listing, fetched separately and joined
  // by listing id (see models/listing_inspection_status.dart for why).
  Map<String, ListingInspectionStatus> _inspectionStatusByListingId = {};
  bool _isLoading = true;
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    _loadMyListings();
  }

  Future<void> _loadMyListings() async {
    setState(() => _isLoading = true);
    try {
      final results = await Future.wait([
        widget.apiService.getListings(
          status: _statusFilter,
          sortBy: 'date',
          sortDir: 'desc',
          pageSize: 50,
        ),
        // Best-effort: if this fails, listings still render, just without
        // inspection-status badges.
        widget.apiService.getMyListingsInspectionStatus().catchError((_) => <String, ListingInspectionStatus>{}),
      ]);
      if (mounted) {
        setState(() {
          _myListings = (results[0] as PagedListings).items;
          _inspectionStatusByListingId = results[1] as Map<String, ListingInspectionStatus>;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to load listings: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Produce Listings'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadMyListings,
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter Chips for Status
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                _buildFilterChip('All Statuses', null),
                const SizedBox(width: 8),
                _buildFilterChip('Pending Approval', 'PendingApproval'),
                const SizedBox(width: 8),
                _buildFilterChip('Published', 'Published'),
                const SizedBox(width: 8),
                _buildFilterChip('Withdrawn', 'Withdrawn'),
                const SizedBox(width: 8),
                _buildFilterChip('Draft', 'Draft'),
              ],
            ),
          ),
          const Divider(height: 1),

          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _myListings.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.spa_outlined, size: 64, color: Colors.grey.shade400),
                            const SizedBox(height: 12),
                            const Text(
                              'No listings found in this category',
                              style: TextStyle(fontSize: 16, color: Colors.grey),
                            ),
                          ],
                        ),
                      )
                    : RefreshIndicator(
                        onRefresh: _loadMyListings,
                        child: ListView.builder(
                          itemCount: _myListings.length,
                          itemBuilder: (context, index) {
                            final listing = _myListings[index];
                            return ListingCard(
                              listing: listing,
                              inspectionStatus: _inspectionStatusByListingId[listing.id],
                              onTap: () async {
                                final refreshed = await Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => ListingDetailScreen(
                                      listingId: listing.id,
                                      apiService: widget.apiService,
                                    ),
                                  ),
                                );
                                if (refreshed == true) {
                                  _loadMyListings();
                                }
                              },
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(String label, String? status) {
    final isSelected = _statusFilter == status;
    return ChoiceChip(
      label: Text(label),
      selected: isSelected,
      onSelected: (selected) {
        setState(() => _statusFilter = selected ? status : null);
        _loadMyListings();
      },
    );
  }
}
