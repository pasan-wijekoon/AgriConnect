import 'package:flutter/material.dart';
import '../models/listing.dart';
import '../services/api_service.dart';
import '../widgets/listing_card.dart';
import 'listing_detail_screen.dart';

class BrowseListingsScreen extends StatefulWidget {
  final ApiService apiService;
  final VoidCallback? onLogout;

  const BrowseListingsScreen({super.key, required this.apiService, this.onLogout});

  @override
  State<BrowseListingsScreen> createState() => _BrowseListingsScreenState();
}

class _BrowseListingsScreenState extends State<BrowseListingsScreen> {
  final TextEditingController _searchController = TextEditingController();
  List<Listing> _listings = [];
  List<Crop> _crops = [];
  List<Region> _regions = [];

  bool _isLoading = true;
  String? _selectedCropId;
  String? _selectedRegionId;
  String? _selectedGrade;
  String _sortBy = 'date';
  String _sortDir = 'desc';

  @override
  void initState() {
    super.initState();
    _loadReferenceData();
    _loadListings();
  }

  Future<void> _loadReferenceData() async {
    final crops = await widget.apiService.getCrops();
    final regions = await widget.apiService.getRegions();
    if (mounted) {
      setState(() {
        _crops = crops;
        _regions = regions;
      });
    }
  }

  Future<void> _loadListings() async {
    setState(() => _isLoading = true);
    try {
      final res = await widget.apiService.getListings(
        cropId: _selectedCropId,
        regionId: _selectedRegionId,
        grade: _selectedGrade,
        status: 'Published', // Buyers browse published listings
        search: _searchController.text.trim().isNotEmpty ? _searchController.text.trim() : null,
        sortBy: _sortBy,
        sortDir: _sortDir,
      );
      if (mounted) {
        setState(() {
          _listings = res.items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error loading listings: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.eco, color: Colors.green),
            SizedBox(width: 8),
            Text('AgriConnect Marketplace', style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh listings',
            onPressed: _loadListings,
          ),
          if (widget.onLogout != null)
            IconButton(
              icon: const Icon(Icons.logout),
              tooltip: 'Log out',
              onPressed: widget.onLogout,
            ),
        ],
      ),
      body: Column(
        children: [
          // Search Bar
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Search crops (e.g. Carrot, Tomato)...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _searchController.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _searchController.clear();
                          _loadListings();
                        },
                      )
                    : null,
                filled: true,
                fillColor: Colors.grey.shade100,
                contentPadding: const EdgeInsets.symmetric(vertical: 0, horizontal: 16),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide.none,
                ),
              ),
              onSubmitted: (_) => _loadListings(),
            ),
          ),

          // Filter & Sort Row
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            child: Row(
              children: [
                // Crop filter chip
                DropdownButton<String?>(
                  value: _selectedCropId,
                  hint: const Text('All Crops'),
                  underline: const SizedBox(),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All Crops')),
                    ..._crops.map((c) => DropdownMenuItem(value: c.id, child: Text(c.name))),
                  ],
                  onChanged: (val) {
                    setState(() => _selectedCropId = val);
                    _loadListings();
                  },
                ),
                const SizedBox(width: 12),
                // Region filter chip
                DropdownButton<String?>(
                  value: _selectedRegionId,
                  hint: const Text('All Regions'),
                  underline: const SizedBox(),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All Regions')),
                    ..._regions.map((r) => DropdownMenuItem(value: r.id, child: Text(r.name))),
                  ],
                  onChanged: (val) {
                    setState(() => _selectedRegionId = val);
                    _loadListings();
                  },
                ),
                const SizedBox(width: 12),
                // Grade filter chip
                DropdownButton<String?>(
                  value: _selectedGrade,
                  hint: const Text('All Grades'),
                  underline: const SizedBox(),
                  items: const [
                    DropdownMenuItem(value: null, child: Text('All Grades')),
                    DropdownMenuItem(value: 'A', child: Text('Grade A')),
                    DropdownMenuItem(value: 'B', child: Text('Grade B')),
                    DropdownMenuItem(value: 'C', child: Text('Grade C')),
                  ],
                  onChanged: (val) {
                    setState(() => _selectedGrade = val);
                    _loadListings();
                  },
                ),
                const SizedBox(width: 12),
                // Sort by
                DropdownButton<String>(
                  value: '$_sortBy:$_sortDir',
                  underline: const SizedBox(),
                  items: const [
                    DropdownMenuItem(value: 'date:desc', child: Text('Newest First')),
                    DropdownMenuItem(value: 'date:asc', child: Text('Oldest First')),
                    DropdownMenuItem(value: 'price:asc', child: Text('Price: Low to High')),
                    DropdownMenuItem(value: 'price:desc', child: Text('Price: High to Low')),
                    DropdownMenuItem(value: 'quantity:desc', child: Text('Quantity: High to Low')),
                  ],
                  onChanged: (val) {
                    if (val != null) {
                      final parts = val.split(':');
                      setState(() {
                        _sortBy = parts[0];
                        _sortDir = parts[1];
                      });
                      _loadListings();
                    }
                  },
                ),
              ],
            ),
          ),
          const Divider(height: 1),

          // Listing Cards List
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _listings.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.inventory_2_outlined, size: 64, color: Colors.grey.shade400),
                            const SizedBox(height: 12),
                            Text(
                              'No listings found',
                              style: TextStyle(fontSize: 16, color: Colors.grey.shade600),
                            ),
                            const SizedBox(height: 8),
                            ElevatedButton(
                              onPressed: () {
                                setState(() {
                                  _selectedCropId = null;
                                  _selectedRegionId = null;
                                  _selectedGrade = null;
                                  _searchController.clear();
                                });
                                _loadListings();
                              },
                              child: const Text('Clear Filters'),
                            ),
                          ],
                        ),
                      )
                    : RefreshIndicator(
                        onRefresh: _loadListings,
                        child: ListView.builder(
                          itemCount: _listings.length,
                          itemBuilder: (context, index) {
                            final listing = _listings[index];
                            return ListingCard(
                              listing: listing,
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
                                  _loadListings();
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
}
