import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/listing.dart';
import '../services/api_service.dart';

class ListingDetailScreen extends StatefulWidget {
  final String listingId;
  final ApiService apiService;

  const ListingDetailScreen({
    super.key,
    required this.listingId,
    required this.apiService,
  });

  @override
  State<ListingDetailScreen> createState() => _ListingDetailScreenState();
}

class _ListingDetailScreenState extends State<ListingDetailScreen> {
  Listing? _listing;
  bool _isLoading = true;
  bool _isRefreshingSuggestion = false;

  @override
  void initState() {
    super.initState();
    _loadListing();
  }

  Future<void> _loadListing() async {
    setState(() => _isLoading = true);
    try {
      final listing = await widget.apiService.getListingById(widget.listingId);
      if (mounted) {
        setState(() {
          _listing = listing;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to load listing: $e')),
        );
      }
    }
  }

  Future<void> _refreshPriceSuggestion() async {
    setState(() => _isRefreshingSuggestion = true);
    try {
      final suggestion = await widget.apiService.getPriceSuggestion(widget.listingId);
      if (mounted) {
        setState(() {
          if (_listing != null) {
            _listing = Listing(
              id: _listing!.id,
              farmerId: _listing!.farmerId,
              cropName: _listing!.cropName,
              cropCategory: _listing!.cropCategory,
              regionName: _listing!.regionName,
              quantity: _listing!.quantity,
              unit: _listing!.unit,
              claimedGrade: _listing!.claimedGrade,
              pickupWindowStart: _listing!.pickupWindowStart,
              pickupWindowEnd: _listing!.pickupWindowEnd,
              status: _listing!.status,
              minPrice: _listing!.minPrice,
              createdAt: _listing!.createdAt,
              updatedAt: _listing!.updatedAt,
              photos: _listing!.photos,
              priceSuggestion: suggestion,
            );
          }
          _isRefreshingSuggestion = false;
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('AI Price Suggestion refreshed!'), backgroundColor: Colors.green),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isRefreshingSuggestion = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to refresh AI suggestion: $e')),
        );
      }
    }
  }

  Future<void> _showEditDialog() async {
    if (_listing == null) return;
    final qtyController = TextEditingController(text: _listing!.quantity.toString());
    final priceController = TextEditingController(
      text: _listing!.minPrice != null ? _listing!.minPrice.toString() : '',
    );
    String grade = _listing!.claimedGrade;

    final updated = await showDialog<bool>(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: const Text('Edit Listing (FR7)'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextField(
                    controller: qtyController,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: InputDecoration(
                      labelText: 'Quantity (${_listing!.unit})',
                      border: const OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: priceController,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(
                      labelText: 'Floor Price (LKR)',
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<String>(
                    value: grade,
                    decoration: const InputDecoration(
                      labelText: 'Claimed Grade',
                      border: OutlineInputBorder(),
                    ),
                    items: const [
                      DropdownMenuItem(value: 'A', child: Text('Grade A')),
                      DropdownMenuItem(value: 'B', child: Text('Grade B')),
                      DropdownMenuItem(value: 'C', child: Text('Grade C')),
                    ],
                    onChanged: (val) {
                      if (val != null) setDialogState(() => grade = val);
                    },
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: const Text('Cancel'),
                ),
                ElevatedButton(
                  onPressed: () async {
                    final qty = double.tryParse(qtyController.text);
                    final prc = double.tryParse(priceController.text);
                    try {
                      await widget.apiService.updateListing(
                        _listing!.id,
                        quantity: qty,
                        minPrice: prc,
                        claimedGrade: grade,
                      );
                      if (context.mounted) Navigator.pop(context, true);
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Update failed: $e')),
                        );
                      }
                    }
                  },
                  child: const Text('Save Changes'),
                ),
              ],
            );
          },
        );
      },
    );

    if (updated == true) {
      _loadListing();
    }
  }

  Future<void> _confirmWithdraw() async {
    if (_listing == null) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Withdraw Listing?'),
        content: const Text(
          'Are you sure you want to withdraw this produce listing? This will mark it as Withdrawn.',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Withdraw Listing'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      try {
        await widget.apiService.withdrawListing(_listing!.id);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Listing withdrawn.')),
          );
          Navigator.pop(context, true);
        }
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Failed to withdraw: $e')),
          );
        }
      }
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'published':
        return Colors.green;
      case 'pendingapproval':
        return Colors.orange;
      case 'withdrawn':
        return Colors.red;
      case 'soldout':
        return Colors.grey;
      case 'draft':
      default:
        return Colors.blueGrey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('yyyy-MM-dd HH:mm');

    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Listing Details')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (_listing == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Listing Details')),
        body: const Center(child: Text('Listing not found')),
      );
    }

    final listing = _listing!;
    final canModify = listing.status == 'Draft' || listing.status == 'PendingApproval';

    return Scaffold(
      appBar: AppBar(
        title: Text(listing.cropName),
        actions: [
          if (canModify) ...[
            IconButton(
              icon: const Icon(Icons.edit_outlined),
              tooltip: 'Edit Listing (FR7)',
              onPressed: _showEditDialog,
            ),
            IconButton(
              icon: const Icon(Icons.delete_outline, color: Colors.red),
              tooltip: 'Withdraw Listing (FR7)',
              onPressed: _confirmWithdraw,
            ),
          ],
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Photos Carousel / Single Display
            if (listing.photos.isNotEmpty)
              SizedBox(
                height: 240,
                width: double.infinity,
                child: ListView.builder(
                  scrollDirection: Axis.horizontal,
                  itemCount: listing.photos.length,
                  itemBuilder: (context, i) {
                    return Image.network(
                      listing.photos[i].url,
                      width: MediaQuery.of(context).size.width,
                      fit: BoxFit.cover,
                      errorBuilder: (ctx, err, stack) => Container(
                        width: MediaQuery.of(context).size.width,
                        color: Colors.green.shade100,
                        child: const Icon(Icons.eco, size: 64, color: Colors.green),
                      ),
                    );
                  },
                ),
              ),

            Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title & Status Badge
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          listing.cropName,
                          style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                        decoration: BoxDecoration(
                          color: _getStatusColor(listing.status).withOpacity(0.15),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: _getStatusColor(listing.status)),
                        ),
                        child: Text(
                          listing.status,
                          style: TextStyle(
                            color: _getStatusColor(listing.status),
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Category: ${listing.cropCategory} · Region: ${listing.regionName}',
                    style: TextStyle(color: Colors.grey.shade700, fontSize: 14),
                  ),
                  const SizedBox(height: 20),

                  // ── AI Price Discovery Section (FR4, Business-Specific) ──
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        colors: [Colors.green.shade50, Colors.emerald.shade50 ?? Colors.teal.shade50],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: Colors.green.shade300),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(
                              children: [
                                Icon(Icons.auto_awesome, color: Colors.green.shade800),
                                const SizedBox(width: 8),
                                const Text(
                                  'AI Fair-Price Discovery',
                                  style: TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                            IconButton(
                              icon: _isRefreshingSuggestion
                                  ? const SizedBox(
                                      width: 16,
                                      height: 16,
                                      child: CircularProgressIndicator(strokeWidth: 2),
                                    )
                                  : const Icon(Icons.refresh, size: 20),
                              tooltip: 'Refresh AI Price Suggestion',
                              onPressed: _isRefreshingSuggestion ? null : _refreshPriceSuggestion,
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        if (listing.priceSuggestion != null) ...[
                          Text(
                            'LKR ${listing.priceSuggestion!.suggestedPriceMin.toStringAsFixed(0)} – ${listing.priceSuggestion!.suggestedPriceMax.toStringAsFixed(0)} / ${listing.unit}',
                            style: TextStyle(
                              fontSize: 22,
                              fontWeight: FontWeight.bold,
                              color: Colors.green.shade900,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: [
                              Text(
                                'Confidence: ${(listing.priceSuggestion!.confidence * 100).toStringAsFixed(0)}%',
                                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
                              ),
                              const SizedBox(width: 12),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                decoration: BoxDecoration(
                                  color: Colors.blue.shade100,
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: Text(
                                  'Status: ${listing.priceSuggestion!.status}',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    color: Colors.blue.shade900,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),
                          Text(
                            listing.priceSuggestion!.reasoningSummary,
                            style: TextStyle(fontSize: 13, color: Colors.grey.shade800),
                          ),
                        ] else ...[
                          const Text('No price suggestion currently available for this listing.'),
                          const SizedBox(height: 8),
                          ElevatedButton.icon(
                            icon: const Icon(Icons.bolt),
                            label: const Text('Generate Price Suggestion'),
                            onPressed: _refreshPriceSuggestion,
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Specifications Grid
                  const Text('Listing Specifications', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 12),
                  Card(
                    elevation: 0,
                    color: Colors.grey.shade50,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                      side: BorderSide(color: Colors.grey.shade200),
                    ),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        children: [
                          _buildDetailRow('Total Quantity', '${listing.quantity} ${listing.unit}'),
                          const Divider(),
                          _buildDetailRow('Claimed Quality Grade', 'Grade ${listing.claimedGrade}'),
                          const Divider(),
                          _buildDetailRow(
                            'Farmer Floor Price',
                            listing.minPrice != null ? 'LKR ${listing.minPrice} / ${listing.unit}' : 'None specified',
                          ),
                          const Divider(),
                          _buildDetailRow(
                            'Pickup Window Start',
                            dateFormat.format(listing.pickupWindowStart),
                          ),
                          const Divider(),
                          _buildDetailRow(
                            'Pickup Window End',
                            dateFormat.format(listing.pickupWindowEnd),
                          ),
                          const Divider(),
                          _buildDetailRow('Listing ID', listing.id),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey.shade700, fontSize: 13)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
        ],
      ),
    );
  }
}
