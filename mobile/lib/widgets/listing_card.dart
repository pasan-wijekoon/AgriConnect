import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/listing.dart';
import '../models/listing_inspection_status.dart';

class ListingCard extends StatelessWidget {
  final Listing listing;
  final VoidCallback onTap;

  /// Component C — Quality Grading & Inspection (FR12–FR14). Optional: only
  /// MyListingsScreen has this data (from a second, joined API call), so
  /// every other existing caller of ListingCard is unaffected.
  final ListingInspectionStatus? inspectionStatus;

  const ListingCard({
    super.key,
    required this.listing,
    required this.onTap,
    this.inspectionStatus,
  });

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

  String _formatStatus(String status) {
    if (status == 'PendingApproval') return 'Pending Approval';
    if (status == 'SoldOut') return 'Sold Out';
    return status;
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('MMM d');
    final firstPhoto = listing.photos.isNotEmpty ? listing.photos.first.url : null;

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image with status and grade overlays
            Stack(
              children: [
                Container(
                  height: 160,
                  width: double.infinity,
                  color: Colors.green.shade50,
                  child: firstPhoto != null && firstPhoto.startsWith('http')
                      ? Image.network(
                          firstPhoto,
                          fit: BoxFit.cover,
                          errorBuilder: (ctx, err, stack) => _buildPlaceholder(),
                        )
                      : _buildPlaceholder(),
                ),
                Positioned(
                  top: 12,
                  left: 12,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: _getStatusColor(listing.status).withOpacity(0.9),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      _formatStatus(listing.status),
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ),
                Positioned(
                  top: 12,
                  right: 12,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.black.withOpacity(0.7),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      'Grade ${listing.claimedGrade}',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ),
              ],
            ),
            // Listing Info
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          listing.cropName,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: Colors.green.shade100,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          listing.cropCategory,
                          style: TextStyle(
                            color: Colors.green.shade900,
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.location_on_outlined, size: 16, color: Colors.grey.shade600),
                      const SizedBox(width: 4),
                      Text(
                        listing.regionName,
                        style: TextStyle(color: Colors.grey.shade700, fontSize: 13),
                      ),
                      const SizedBox(width: 16),
                      Icon(Icons.scale_outlined, size: 16, color: Colors.grey.shade600),
                      const SizedBox(width: 4),
                      Text(
                        '${listing.quantity.toStringAsFixed(0)} ${listing.unit}',
                        style: TextStyle(color: Colors.grey.shade700, fontSize: 13),
                      ),
                    ],
                  ),
                  if (inspectionStatus != null) ...[
                    const SizedBox(height: 8),
                    _buildInspectionStatusRow(inspectionStatus!),
                  ],
                  const Divider(height: 24),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      // Pricing display
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            listing.priceSuggestion != null
                                ? 'AI Fair Price'
                                : 'Floor Price',
                            style: TextStyle(fontSize: 11, color: Colors.grey.shade600),
                          ),
                          const SizedBox(height: 2),
                          if (listing.priceSuggestion != null)
                            Text(
                              'LKR ${listing.priceSuggestion!.suggestedPriceMin.toStringAsFixed(0)} - ${listing.priceSuggestion!.suggestedPriceMax.toStringAsFixed(0)} / ${listing.unit}',
                              style: const TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.bold,
                                color: Colors.green,
                              ),
                            )
                          else if (listing.minPrice != null)
                            Text(
                              'LKR ${listing.minPrice!.toStringAsFixed(0)} / ${listing.unit}',
                              style: const TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.bold,
                              ),
                            )
                          else
                            Text(
                              'Negotiable',
                              style: TextStyle(
                                fontSize: 14,
                                fontStyle: FontStyle.italic,
                                color: Colors.grey.shade700,
                              ),
                            ),
                        ],
                      ),
                      // Pickup window
                      Row(
                        children: [
                          Icon(Icons.calendar_today_outlined, size: 14, color: Colors.grey.shade600),
                          const SizedBox(width: 4),
                          Text(
                            '${dateFormat.format(listing.pickupWindowStart)} - ${dateFormat.format(listing.pickupWindowEnd)}',
                            style: TextStyle(fontSize: 12, color: Colors.grey.shade700),
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInspectionStatusRow(ListingInspectionStatus status) {
    final children = <Widget>[];

    if (status.inspectionCount == 0) {
      children.add(_statusChip('Not yet inspected', Colors.blueGrey, Icons.hourglass_empty));
    } else if (status.latestConfirmedGrade != null) {
      children.add(_statusChip(
        'Confirmed: ${status.latestConfirmedGrade}',
        Colors.teal,
        Icons.verified_outlined,
      ));
    }

    if (status.hasUnresolvedDiscrepancy) {
      if (children.isNotEmpty) children.add(const SizedBox(width: 8));
      children.add(_statusChip('Grade discrepancy flagged', Colors.deepOrange, Icons.warning_amber_rounded));
    }

    return Wrap(spacing: 8, runSpacing: 4, children: children);
  }

  Widget _statusChip(String label, Color color, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withOpacity(0.3)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: color),
          const SizedBox(width: 4),
          Text(label, style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: color)),
        ],
      ),
    );
  }

  Widget _buildPlaceholder() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.eco_outlined, size: 48, color: Colors.green.shade400),
          const SizedBox(height: 4),
          Text(
            listing.cropName,
            style: TextStyle(color: Colors.green.shade700, fontWeight: FontWeight.w500),
          ),
        ],
      ),
    );
  }
}
