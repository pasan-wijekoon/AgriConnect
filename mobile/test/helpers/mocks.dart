import 'package:mocktail/mocktail.dart';

import 'package:agriconnect_mobile/models/listing.dart';
import 'package:agriconnect_mobile/services/api_service.dart';

class MockApiService extends Mock implements ApiService {}

Listing fakeListing({
  String id = 'listing-1',
  String cropName = 'Carrot',
  double quantity = 12,
  String grade = 'A',
  double? minPrice = 240,
  String status = 'Published',
}) {
  final now = DateTime.utc(2026, 10, 7);
  return Listing(
    id: id,
    farmerId: 'farmer-1',
    cropName: cropName,
    cropCategory: 'Vegetable',
    regionName: 'Nuwara Eliya',
    quantity: quantity,
    unit: 'kg',
    claimedGrade: grade,
    pickupWindowStart: now.add(const Duration(days: 1)),
    pickupWindowEnd: now.add(const Duration(days: 3)),
    status: status,
    minPrice: minPrice,
    createdAt: now,
    updatedAt: now,
    photos: const [],
  );
}

PagedListings fakePagedListings([List<Listing> items = const []]) =>
    PagedListings(
      items: items,
      totalCount: items.length,
      page: 1,
      pageSize: 10,
    );

void stubBrowseReferenceData(MockApiService api) {
  when(() => api.getCrops()).thenAnswer(
    (_) async => [Crop(id: 'crop-1', name: 'Carrot', category: 'Vegetable')],
  );
  when(
    () => api.getRegions(),
  ).thenAnswer((_) async => [Region(id: 'region-1', name: 'Nuwara Eliya')]);
}

void stubBrowseListings(
  MockApiService api,
  Future<PagedListings> Function() response,
) {
  when(
    () => api.getListings(
      cropId: any(named: 'cropId'),
      regionId: any(named: 'regionId'),
      grade: any(named: 'grade'),
      status: any(named: 'status'),
      minPrice: any(named: 'minPrice'),
      maxPrice: any(named: 'maxPrice'),
      search: any(named: 'search'),
      sortBy: any(named: 'sortBy'),
      sortDir: any(named: 'sortDir'),
      page: any(named: 'page'),
      pageSize: any(named: 'pageSize'),
    ),
  ).thenAnswer((_) => response());
}
