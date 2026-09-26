// ─────────────────────────────────────────────────────────────────────────────
// Component C — Quality Grading & Inspection: Data Models
// Mirrors TypeScript types in web/src/types/inspection.ts
// and backend DTOs in backend/src/dtos/InspectionDtos.cs
// ─────────────────────────────────────────────────────────────────────────────

/// Photo attached to an inspection record.
class InspectionPhoto {
  final String id;
  final String url;
  final String uploadedAt;

  const InspectionPhoto({
    required this.id,
    required this.url,
    required this.uploadedAt,
  });

  factory InspectionPhoto.fromJson(Map<String, dynamic> json) {
    return InspectionPhoto(
      id: json['id'] as String,
      url: json['url'] as String,
      uploadedAt: json['uploadedAt'] as String,
    );
  }
}

/// Full inspection record — matches InspectionResponse DTO.
class InspectionRecord {
  final String id;
  final String listingId;
  final String cropName;
  final double quantity;
  final String unit;
  final String claimedGrade;
  final String confirmedGrade;
  final String? notes;
  final String inspectedAt;
  final String officerId;
  final String officerName;
  final String farmerName;
  final String regionName;
  final String listingStatus;
  final bool hasDiscrepancy;
  final List<InspectionPhoto> photos;

  const InspectionRecord({
    required this.id,
    required this.listingId,
    required this.cropName,
    required this.quantity,
    required this.unit,
    required this.claimedGrade,
    required this.confirmedGrade,
    this.notes,
    required this.inspectedAt,
    required this.officerId,
    required this.officerName,
    required this.farmerName,
    required this.regionName,
    required this.listingStatus,
    required this.hasDiscrepancy,
    required this.photos,
  });

  factory InspectionRecord.fromJson(Map<String, dynamic> json) {
    return InspectionRecord(
      id: json['id'] as String,
      listingId: json['listingId'] as String,
      cropName: json['cropName'] as String,
      quantity: (json['quantity'] as num).toDouble(),
      unit: json['unit'] as String,
      claimedGrade: json['claimedGrade'] as String,
      confirmedGrade: json['confirmedGrade'] as String,
      notes: json['notes'] as String?,
      inspectedAt: json['inspectedAt'] as String,
      officerId: json['officerId'] as String,
      officerName: json['officerName'] as String,
      farmerName: json['farmerName'] as String,
      regionName: json['regionName'] as String,
      listingStatus: json['listingStatus'] as String,
      hasDiscrepancy: json['hasDiscrepancy'] as bool,
      photos: ((json['photos'] as List<dynamic>?) ?? [])
          .map((p) => InspectionPhoto.fromJson(p as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Returns true if confirmed grade differs from claimed grade.
  bool get isGradeMatch => claimedGrade.toLowerCase() == confirmedGrade.toLowerCase();
}

/// Farmer's produce listing summary — matches ListingSummary DTO.
class ListingSummary {
  final String id;
  final String farmerId;
  final String farmerName;
  final String farmerPhone;
  final String cropId;
  final String cropName;
  final String category;
  final String regionId;
  final String regionName;
  final double quantity;
  final String unit;
  final String claimedGrade;
  final String? latestConfirmedGrade;
  final String pickupWindowStart;
  final String pickupWindowEnd;
  final String status;
  final double? minPrice;
  final String createdAt;
  final int inspectionCount;
  final bool hasUnresolvedDiscrepancy;
  final List<String> listingPhotos;

  const ListingSummary({
    required this.id,
    required this.farmerId,
    required this.farmerName,
    required this.farmerPhone,
    required this.cropId,
    required this.cropName,
    required this.category,
    required this.regionId,
    required this.regionName,
    required this.quantity,
    required this.unit,
    required this.claimedGrade,
    this.latestConfirmedGrade,
    required this.pickupWindowStart,
    required this.pickupWindowEnd,
    required this.status,
    this.minPrice,
    required this.createdAt,
    required this.inspectionCount,
    required this.hasUnresolvedDiscrepancy,
    required this.listingPhotos,
  });

  factory ListingSummary.fromJson(Map<String, dynamic> json) {
    return ListingSummary(
      id: json['id'] as String,
      farmerId: json['farmerId'] as String,
      farmerName: json['farmerName'] as String,
      farmerPhone: json['farmerPhone'] as String? ?? '',
      cropId: json['cropId'] as String,
      cropName: json['cropName'] as String,
      category: json['category'] as String,
      regionId: json['regionId'] as String,
      regionName: json['regionName'] as String,
      quantity: (json['quantity'] as num).toDouble(),
      unit: json['unit'] as String,
      claimedGrade: json['claimedGrade'] as String,
      latestConfirmedGrade: json['latestConfirmedGrade'] as String?,
      pickupWindowStart: json['pickupWindowStart'] as String,
      pickupWindowEnd: json['pickupWindowEnd'] as String,
      status: json['status'] as String,
      minPrice: json['minPrice'] != null ? (json['minPrice'] as num).toDouble() : null,
      createdAt: json['createdAt'] as String,
      inspectionCount: json['inspectionCount'] as int,
      hasUnresolvedDiscrepancy: json['hasUnresolvedDiscrepancy'] as bool,
      listingPhotos: ((json['listingPhotos'] as List<dynamic>?) ?? [])
          .map((p) => p as String)
          .toList(),
    );
  }

  /// Whether the listing has been inspected at least once.
  bool get hasBeenInspected => inspectionCount > 0;

  /// Derives a display label for inspection state.
  String get inspectionStatusLabel {
    if (!hasBeenInspected) return 'Not Inspected';
    if (hasUnresolvedDiscrepancy) return 'Discrepancy';
    if (latestConfirmedGrade == 'Rejected') return 'Rejected';
    return latestConfirmedGrade ?? claimedGrade;
  }
}

/// Grade discrepancy flag — matches GradeDiscrepancy DTO.
class GradeDiscrepancy {
  final String id;
  final String listingId;
  final String cropName;
  final String farmerName;
  final double quantity;
  final String unit;
  final String claimedGrade;
  final String confirmedGrade;
  final String flaggedAt;
  final String? resolvedAt;
  final String? resolutionNotes;
  final String? resolvedByOfficerName;
  final bool isResolved;

  const GradeDiscrepancy({
    required this.id,
    required this.listingId,
    required this.cropName,
    required this.farmerName,
    required this.quantity,
    required this.unit,
    required this.claimedGrade,
    required this.confirmedGrade,
    required this.flaggedAt,
    this.resolvedAt,
    this.resolutionNotes,
    this.resolvedByOfficerName,
    required this.isResolved,
  });

  factory GradeDiscrepancy.fromJson(Map<String, dynamic> json) {
    return GradeDiscrepancy(
      id: json['id'] as String,
      listingId: json['listingId'] as String,
      cropName: json['cropName'] as String,
      farmerName: json['farmerName'] as String,
      quantity: (json['quantity'] as num).toDouble(),
      unit: json['unit'] as String,
      claimedGrade: json['claimedGrade'] as String,
      confirmedGrade: json['confirmedGrade'] as String,
      flaggedAt: json['flaggedAt'] as String,
      resolvedAt: json['resolvedAt'] as String?,
      resolutionNotes: json['resolutionNotes'] as String?,
      resolvedByOfficerName: json['resolvedByOfficerName'] as String?,
      isResolved: json['isResolved'] as bool,
    );
  }
}

/// Quality dashboard summary stats.
class QualityDashboardStats {
  final int pendingInspections;
  final int completedToday;
  final int totalInspections;
  final int activeDiscrepancies;
  final int publishedListings;
  final double gradeAComplianceRate;

  const QualityDashboardStats({
    required this.pendingInspections,
    required this.completedToday,
    required this.totalInspections,
    required this.activeDiscrepancies,
    required this.publishedListings,
    required this.gradeAComplianceRate,
  });

  factory QualityDashboardStats.fromJson(Map<String, dynamic> json) {
    return QualityDashboardStats(
      pendingInspections: json['pendingInspections'] as int,
      completedToday: json['completedToday'] as int,
      totalInspections: json['totalInspections'] as int,
      activeDiscrepancies: json['activeDiscrepancies'] as int,
      publishedListings: json['publishedListings'] as int,
      gradeAComplianceRate: (json['gradeAComplianceRate'] as num).toDouble(),
    );
  }
}

/// Generic paginated result wrapper.
class PagedResult<T> {
  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;

  const PagedResult({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
  });
}
