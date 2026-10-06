/// Component C — Quality Grading & Inspection (FR12–FR14). A lightweight
/// projection of `ListingSummaryDto`'s inspection-status fields, keyed by
/// listing id and joined client-side against the existing `Listing` model
/// (from `GET /api/listings/my`) rather than replacing it — the two DTOs
/// don't share a shape (this one's `listingPhotos` is a list of URL strings,
/// not the full `Photo` objects `Listing` uses), so merging them into one
/// model would mean forcing an awkward common shape onto both call sites.
class ListingInspectionStatus {
  final String listingId;
  final int inspectionCount;
  final String? latestConfirmedGrade;
  final bool hasUnresolvedDiscrepancy;

  ListingInspectionStatus({
    required this.listingId,
    required this.inspectionCount,
    this.latestConfirmedGrade,
    required this.hasUnresolvedDiscrepancy,
  });

  factory ListingInspectionStatus.fromJson(Map<String, dynamic> json) {
    return ListingInspectionStatus(
      listingId: json['id'] as String? ?? '',
      inspectionCount: json['inspectionCount'] as int? ?? 0,
      latestConfirmedGrade: json['latestConfirmedGrade'] as String?,
      hasUnresolvedDiscrepancy: json['hasUnresolvedDiscrepancy'] as bool? ?? false,
    );
  }
}

/// A single recorded inspection (`GET /api/listings/{id}/inspections`),
/// shown on ListingDetailScreen's inspection-history section (FR13).
class ListingInspectionRecord {
  final String id;
  final String confirmedGrade;
  final String? notes;
  final DateTime inspectedAt;
  final String officerName;
  final bool hasDiscrepancy;

  ListingInspectionRecord({
    required this.id,
    required this.confirmedGrade,
    this.notes,
    required this.inspectedAt,
    required this.officerName,
    required this.hasDiscrepancy,
  });

  factory ListingInspectionRecord.fromJson(Map<String, dynamic> json) {
    return ListingInspectionRecord(
      id: json['id'] as String? ?? '',
      confirmedGrade: json['confirmedGrade'] as String? ?? '',
      notes: json['notes'] as String?,
      inspectedAt: DateTime.tryParse(json['inspectedAt'] as String? ?? '') ?? DateTime.now(),
      officerName: json['officerName'] as String? ?? 'Officer',
      hasDiscrepancy: json['hasDiscrepancy'] as bool? ?? false,
    );
  }
}
