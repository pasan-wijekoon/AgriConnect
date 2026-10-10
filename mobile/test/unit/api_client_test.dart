import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mocktail/mocktail.dart';

import 'package:agriconnect_mobile/models/listing.dart';
import 'package:agriconnect_mobile/services/api_service.dart';

class MockHttpClient extends Mock implements http.Client {}

class FakeUri extends Fake implements Uri {}

void main() {
  late MockHttpClient client;
  late ApiService apiService;

  setUpAll(() {
    registerFallbackValue(FakeUri());
  });

  setUp(() {
    client = MockHttpClient();
    apiService = ApiService(
      baseUrl: 'https://api.example.test/api',
      client: client,
    );
  });

  // MOB-U-01
  test('parses listing fields from a valid API response', () async {
    final pickupStart = DateTime.utc(2026, 10, 8, 8);
    final pickupEnd = DateTime.utc(2026, 10, 8, 12);
    final createdAt = DateTime.utc(2026, 10, 7, 10);
    final updatedAt = DateTime.utc(2026, 10, 7, 11);
    final payload = {
      'items': [
        {
          'id': 'listing-1',
          'farmerId': 'farmer-1',
          'cropName': 'Carrot',
          'cropCategory': 'Vegetable',
          'regionName': 'Nuwara Eliya',
          'quantity': 12.5,
          'availableQuantity': 7.5,
          'unit': 'kg',
          'claimedGrade': 'A',
          'pickupWindowStart': pickupStart.toIso8601String(),
          'pickupWindowEnd': pickupEnd.toIso8601String(),
          'status': 'Published',
          'minPrice': 240,
          'createdAt': createdAt.toIso8601String(),
          'updatedAt': updatedAt.toIso8601String(),
          'photos': [
            {
              'id': 'photo-1',
              'url': 'https://images.example.test/carrot.png',
              'uploadedAt': createdAt.toIso8601String(),
            },
          ],
        },
      ],
      'totalCount': 1,
      'page': 2,
      'pageSize': 5,
    };

    when(
      () => client.get(any(), headers: any(named: 'headers')),
    ).thenAnswer((_) async => http.Response(jsonEncode(payload), 200));

    final result = await apiService.getListings(page: 2, pageSize: 5);
    final listing = result.items.single;

    expect(result.totalCount, 1);
    expect(result.page, 2);
    expect(result.pageSize, 5);
    expect(listing.id, 'listing-1');
    expect(listing.cropName, 'Carrot');
    expect(listing.cropCategory, 'Vegetable');
    expect(listing.regionName, 'Nuwara Eliya');
    expect(listing.quantity, 12.5);
    expect(listing.availableQuantity, 7.5);
    expect(listing.unit, 'kg');
    expect(listing.claimedGrade, 'A');
    expect(listing.pickupWindowStart, pickupStart);
    expect(listing.pickupWindowEnd, pickupEnd);
    expect(listing.status, 'Published');
    expect(listing.minPrice, 240);
    expect(listing.photos.single.url, 'https://images.example.test/carrot.png');

    final request =
        verify(
              () => client.get(captureAny(), headers: any(named: 'headers')),
            ).captured.single
            as Uri;
    expect(request.path, '/api/listings');
    expect(request.queryParameters['page'], '2');
    expect(request.queryParameters['pageSize'], '5');
  });

  // MOB-U-02
  test(
    'uses the model defaults when listing fields are missing from JSON',
    () async {
      when(() => client.get(any(), headers: any(named: 'headers'))).thenAnswer(
        (_) async => http.Response(
          jsonEncode({
            'items': [{}],
            'totalCount': 1,
            'page': 1,
            'pageSize': 10,
          }),
          200,
        ),
      );

      final result = await apiService.getListings();
      final Listing listing = result.items.single;

      expect(listing.id, isEmpty);
      expect(listing.farmerId, isEmpty);
      expect(listing.cropName, isEmpty);
      expect(listing.quantity, 0);
      expect(listing.availableQuantity, 0);
      expect(listing.unit, 'kg');
      expect(listing.claimedGrade, isEmpty);
      expect(listing.status, 'Draft');
      expect(listing.minPrice, isNull);
      expect(listing.photos, isEmpty);
      expect(listing.priceSuggestion, isNull);
    },
  );
}
