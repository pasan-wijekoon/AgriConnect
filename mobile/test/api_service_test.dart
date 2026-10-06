import 'dart:convert';

import 'package:agriconnect_mobile/services/api_service.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test(
    'MOB-U-01 – API client parses listing fields from a valid response',
    () async {
      final client = MockClient(
        (request) async => http.Response(
          jsonEncode({
            'items': [
              {
                'id': 'listing-1',
                'farmerId': 'farmer-1',
                'cropName': 'Tomato',
                'cropCategory': 'Vegetable',
                'regionName': 'Kandy',
                'quantity': 25.5,
                'unit': 'kg',
                'claimedGrade': 'A',
                'pickupWindowStart': '2026-10-05T00:00:00Z',
                'pickupWindowEnd': '2026-10-08T00:00:00Z',
                'status': 'Published',
                'minPrice': 150,
                'createdAt': '2026-10-01T00:00:00Z',
                'updatedAt': '2026-10-01T00:00:00Z',
                'photos': [],
              },
            ],
            'totalCount': 1,
            'page': 1,
            'pageSize': 10,
          }),
          200,
        ),
      );
      final api = ApiService(baseUrl: 'http://test/api', client: client);

      final result = await api.getListings();

      expect(result.items, hasLength(1));
      expect(result.items.single.cropName, 'Tomato');
      expect(result.items.single.quantity, 25.5);
      expect(result.items.single.minPrice, 150);
      expect(result.items.single.claimedGrade, 'A');
    },
  );

  test(
    'MOB-U-02 – missing listing fields use safe defaults without crashing',
    () async {
      final client = MockClient(
        (request) async => http.Response(
          jsonEncode({
            'items': [
              {'id': 'partial-listing'},
            ],
          }),
          200,
        ),
      );
      final api = ApiService(baseUrl: 'http://test/api', client: client);

      final result = await api.getListings();

      expect(result.items, hasLength(1));
      expect(result.items.single.id, 'partial-listing');
      expect(result.items.single.cropName, isEmpty);
      expect(result.items.single.quantity, 0);
      expect(result.items.single.unit, 'kg');
      expect(result.items.single.photos, isEmpty);
      expect(result.totalCount, 0);
      expect(result.page, 1);
      expect(result.pageSize, 10);
    },
  );
}
