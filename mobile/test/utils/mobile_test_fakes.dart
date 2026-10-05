import 'package:agriconnect_mobile/models/listing.dart';
import 'package:agriconnect_mobile/models/listing_inspection_status.dart';
import 'package:agriconnect_mobile/services/api_service.dart';

class TestApiService extends ApiService {
  TestApiService({
    this.listings = const [],
    this.listingDetails,
    this.loadListings,
  }) : super(baseUrl: 'http://test');

  final List<Listing> listings;
  final Listing? listingDetails;
  Future<PagedListings> Function()? loadListings;
  int createListingCalls = 0;

  @override
  Future<List<Crop>> getCrops() async => [
    Crop(id: 'crop-tomato', name: 'Tomato', category: 'Vegetable'),
  ];

  @override
  Future<List<Region>> getRegions() async => [
    Region(id: 'region-kandy', name: 'Kandy'),
  ];

  @override
  Future<PagedListings> getListings({
    String? cropId,
    String? regionId,
    String? grade,
    String? status,
    double? minPrice,
    double? maxPrice,
    String? search,
    String sortBy = 'date',
    String sortDir = 'desc',
    int page = 1,
    int pageSize = 10,
  }) async {
    final loader = loadListings;
    if (loader != null) return loader();
    return PagedListings(
      items: listings,
      totalCount: listings.length,
      page: page,
      pageSize: pageSize,
    );
  }

  @override
  Future<Listing> getListingById(String id) async {
    return listingDetails ?? makeTestListing();
  }

  @override
  Future<List<ListingInspectionRecord>> getListingInspections(
    String listingId,
  ) async => [];

  @override
  Future<Map<String, dynamic>> getQuickPriceEstimate({
    required String cropId,
    required String regionId,
    String? cropName,
    String? regionName,
    String grade = 'A',
    double quantity = 100,
  }) async => {
    'suggestedPriceMin': 100.0,
    'suggestedPriceMax': 140.0,
    'confidence': 0.9,
    'reasoningSummary': 'Test price estimate',
  };

  @override
  Future<Listing> createListing({
    required String cropId,
    required String regionId,
    required double quantity,
    required String unit,
    required String claimedGrade,
    required DateTime pickupWindowStart,
    required DateTime pickupWindowEnd,
    double? minPrice,
    required List<String> photoUrls,
  }) async {
    createListingCalls++;
    return listingDetails ?? makeTestListing();
  }
}

Listing makeTestListing() => Listing(
  id: 'listing-tomato',
  farmerId: 'farmer-1',
  cropName: 'Tomato',
  cropCategory: 'Vegetable',
  regionName: 'Kandy',
  quantity: 25,
  unit: 'kg',
  claimedGrade: 'A',
  pickupWindowStart: DateTime.utc(2026, 10, 5),
  pickupWindowEnd: DateTime.utc(2026, 10, 8),
  status: 'Published',
  minPrice: 150,
  createdAt: DateTime.utc(2026, 10, 1),
  updatedAt: DateTime.utc(2026, 10, 1),
  photos: const [],
);
