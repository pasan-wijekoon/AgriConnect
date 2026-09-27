class Crop {
  final String id;
  final String name;
  final String category;

  Crop({
    required this.id,
    required this.name,
    required this.category,
  });

  factory Crop.fromJson(Map<String, dynamic> json) {
    return Crop(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      category: json['category'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'category': category,
  };
}

class Region {
  final String id;
  final String name;
  final String? collectionCentreId;

  Region({
    required this.id,
    required this.name,
    this.collectionCentreId,
  });

  factory Region.fromJson(Map<String, dynamic> json) {
    return Region(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      collectionCentreId: json['collectionCentreId'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'collectionCentreId': collectionCentreId,
  };
}

class Photo {
  final String id;
  final String url;
  final DateTime uploadedAt;

  Photo({
    required this.id,
    required this.url,
    required this.uploadedAt,
  });

  factory Photo.fromJson(Map<String, dynamic> json) {
    return Photo(
      id: json['id'] as String? ?? '',
      url: json['url'] as String? ?? '',
      uploadedAt: DateTime.tryParse(json['uploadedAt'] as String? ?? '') ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'url': url,
    'uploadedAt': uploadedAt.toIso8601String(),
  };
}

class PriceSuggestion {
  final String id;
  final String listingId;
  final double suggestedPriceMin;
  final double suggestedPriceMax;
  final double confidence;
  final String reasoningSummary;
  final String status;
  final DateTime createdAt;

  PriceSuggestion({
    required this.id,
    required this.listingId,
    required this.suggestedPriceMin,
    required this.suggestedPriceMax,
    required this.confidence,
    required this.reasoningSummary,
    required this.status,
    required this.createdAt,
  });

  factory PriceSuggestion.fromJson(Map<String, dynamic> json) {
    return PriceSuggestion(
      id: json['id'] as String? ?? '',
      listingId: json['listingId'] as String? ?? '',
      suggestedPriceMin: (json['suggestedPriceMin'] as num?)?.toDouble() ?? 0.0,
      suggestedPriceMax: (json['suggestedPriceMax'] as num?)?.toDouble() ?? 0.0,
      confidence: (json['confidence'] as num?)?.toDouble() ?? 0.0,
      reasoningSummary: json['reasoningSummary'] as String? ?? '',
      status: json['status'] as String? ?? 'Proposed',
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'listingId': listingId,
    'suggestedPriceMin': suggestedPriceMin,
    'suggestedPriceMax': suggestedPriceMax,
    'confidence': confidence,
    'reasoningSummary': reasoningSummary,
    'status': status,
    'createdAt': createdAt.toIso8601String(),
  };
}

class Listing {
  final String id;
  final String farmerId;
  final String cropName;
  final String cropCategory;
  final String regionName;
  final double quantity;
  final String unit;
  final String claimedGrade;
  final DateTime pickupWindowStart;
  final DateTime pickupWindowEnd;
  final String status;
  final double? minPrice;
  final DateTime createdAt;
  final DateTime updatedAt;
  final List<Photo> photos;
  final PriceSuggestion? priceSuggestion;

  Listing({
    required this.id,
    required this.farmerId,
    required this.cropName,
    required this.cropCategory,
    required this.regionName,
    required this.quantity,
    required this.unit,
    required this.claimedGrade,
    required this.pickupWindowStart,
    required this.pickupWindowEnd,
    required this.status,
    this.minPrice,
    required this.createdAt,
    required this.updatedAt,
    required this.photos,
    this.priceSuggestion,
  });

  factory Listing.fromJson(Map<String, dynamic> json) {
    return Listing(
      id: json['id'] as String? ?? '',
      farmerId: json['farmerId'] as String? ?? '',
      cropName: json['cropName'] as String? ?? '',
      cropCategory: json['cropCategory'] as String? ?? '',
      regionName: json['regionName'] as String? ?? '',
      quantity: (json['quantity'] as num?)?.toDouble() ?? 0.0,
      unit: json['unit'] as String? ?? 'kg',
      claimedGrade: json['claimedGrade'] as String? ?? '',
      pickupWindowStart: DateTime.tryParse(json['pickupWindowStart'] as String? ?? '') ?? DateTime.now(),
      pickupWindowEnd: DateTime.tryParse(json['pickupWindowEnd'] as String? ?? '') ?? DateTime.now(),
      status: json['status'] as String? ?? 'Draft',
      minPrice: (json['minPrice'] as num?)?.toDouble(),
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt'] as String? ?? '') ?? DateTime.now(),
      photos: (json['photos'] as List<dynamic>?)
              ?.map((p) => Photo.fromJson(p as Map<String, dynamic>))
              .toList() ??
          [],
      priceSuggestion: json['priceSuggestion'] != null
          ? PriceSuggestion.fromJson(json['priceSuggestion'] as Map<String, dynamic>)
          : null,
    );
  }
}

class PagedListings {
  final List<Listing> items;
  final int totalCount;
  final int page;
  final int pageSize;

  PagedListings({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
  });

  factory PagedListings.fromJson(Map<String, dynamic> json) {
    return PagedListings(
      items: (json['items'] as List<dynamic>?)
              ?.map((item) => Listing.fromJson(item as Map<String, dynamic>))
              .toList() ??
          [],
      totalCount: json['totalCount'] as int? ?? 0,
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 10,
    );
  }
}
