import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import '../models/listing.dart';

class ApiService {
  // Default URL depends on platform: Android emulator uses 10.0.2.2, otherwise localhost
  static String get defaultBaseUrl {
    if (!kIsWeb && Platform.isAndroid) {
      return 'http://10.0.2.2:5000/api';
    }
    return 'http://localhost:5000/api';
  }

  final String baseUrl;

  /// Supplies the current bearer token for every request. The backend now
  /// requires authentication on all listing/price endpoints ([Authorize] in
  /// ListingsController/PricesController), so every call below sends it when
  /// available. Injected as a getter (rather than a fixed token) so this
  /// keeps working across login/logout without reconstructing ApiService.
  final String? Function() getToken;

  ApiService({String? baseUrl, String? Function()? getToken})
      : baseUrl = baseUrl ?? defaultBaseUrl,
        getToken = getToken ?? (() => null);

  Map<String, String> get _headers {
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
    final token = getToken();
    if (token != null && token.isNotEmpty) {
      headers['Authorization'] = 'Bearer $token';
    }
    return headers;
  }

  // ── Crops & Regions Reference Data ────────────────────────
  Future<List<Crop>> getCrops() async {
    try {
      final res = await http.get(Uri.parse('$baseUrl/crops'), headers: _headers);
      if (res.statusCode == 200) {
        final List<dynamic> data = jsonDecode(res.body);
        return data.map((item) => Crop.fromJson(item as Map<String, dynamic>)).toList();
      }
      return _fallbackCrops();
    } catch (e) {
      debugPrint('Error fetching crops: $e');
      return _fallbackCrops();
    }
  }

  Future<List<Region>> getRegions() async {
    try {
      final res = await http.get(Uri.parse('$baseUrl/regions'), headers: _headers);
      if (res.statusCode == 200) {
        final List<dynamic> data = jsonDecode(res.body);
        return data.map((item) => Region.fromJson(item as Map<String, dynamic>)).toList();
      }
      return _fallbackRegions();
    } catch (e) {
      debugPrint('Error fetching regions: $e');
      return _fallbackRegions();
    }
  }

  // ── Listings ──────────────────────────────────────────────
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
    final queryParams = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      'sortBy': sortBy,
      'sortDir': sortDir,
    };

    if (cropId != null && cropId.isNotEmpty) queryParams['cropId'] = cropId;
    if (regionId != null && regionId.isNotEmpty) queryParams['regionId'] = regionId;
    if (grade != null && grade.isNotEmpty) queryParams['grade'] = grade;
    if (status != null && status.isNotEmpty) queryParams['status'] = status;
    if (minPrice != null) queryParams['minPrice'] = minPrice.toString();
    if (maxPrice != null) queryParams['maxPrice'] = maxPrice.toString();
    if (search != null && search.isNotEmpty) queryParams['search'] = search;

    final uri = Uri.parse('$baseUrl/listings').replace(queryParameters: queryParams);
    final res = await http.get(uri, headers: _headers);

    if (res.statusCode == 200) {
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      return PagedListings.fromJson(data);
    }
    throw Exception('Failed to load listings: ${res.statusCode} ${res.body}');
  }

  Future<Listing> getListingById(String id) async {
    final res = await http.get(Uri.parse('$baseUrl/listings/$id'), headers: _headers);
    if (res.statusCode == 200) {
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      return Listing.fromJson(data);
    }
    throw Exception('Failed to load listing: ${res.statusCode}');
  }

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
    final body = jsonEncode({
      'cropId': cropId,
      'regionId': regionId,
      'quantity': quantity,
      'unit': unit,
      'claimedGrade': claimedGrade,
      'pickupWindowStart': pickupWindowStart.toIso8601String(),
      'pickupWindowEnd': pickupWindowEnd.toIso8601String(),
      'minPrice': minPrice,
      'photoUrls': photoUrls.isNotEmpty
          ? photoUrls
          : ['https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800'],
    });

    final res = await http.post(
      Uri.parse('$baseUrl/listings'),
      headers: _headers,
      body: body,
    );

    if (res.statusCode == 201 || res.statusCode == 200) {
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      return Listing.fromJson(data);
    }
    throw Exception('Failed to create listing: ${res.statusCode} ${res.body}');
  }

  Future<Listing> updateListing(
    String id, {
    String? cropId,
    String? regionId,
    double? quantity,
    String? unit,
    String? claimedGrade,
    DateTime? pickupWindowStart,
    DateTime? pickupWindowEnd,
    double? minPrice,
  }) async {
    final body = jsonEncode({
      if (cropId != null) 'cropId': cropId,
      if (regionId != null) 'regionId': regionId,
      if (quantity != null) 'quantity': quantity,
      if (unit != null) 'unit': unit,
      if (claimedGrade != null) 'claimedGrade': claimedGrade,
      if (pickupWindowStart != null) 'pickupWindowStart': pickupWindowStart.toIso8601String(),
      if (pickupWindowEnd != null) 'pickupWindowEnd': pickupWindowEnd.toIso8601String(),
      if (minPrice != null) 'minPrice': minPrice,
    });

    final res = await http.put(
      Uri.parse('$baseUrl/listings/$id'),
      headers: _headers,
      body: body,
    );

    if (res.statusCode == 200) {
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      return Listing.fromJson(data);
    }
    throw Exception('Failed to update listing: ${res.statusCode} ${res.body}');
  }

  Future<void> withdrawListing(String id) async {
    final res = await http.delete(
      Uri.parse('$baseUrl/listings/$id'),
      headers: _headers,
    );

    if (res.statusCode != 204 && res.statusCode != 200) {
      throw Exception('Failed to withdraw listing: ${res.statusCode}');
    }
  }

  Future<PriceSuggestion> getPriceSuggestion(String listingId) async {
    final res = await http.get(
      Uri.parse('$baseUrl/listings/$listingId/price-suggestion'),
      headers: _headers,
    );

    if (res.statusCode == 200) {
      final data = jsonDecode(res.body) as Map<String, dynamic>;
      return PriceSuggestion.fromJson(data);
    }
    throw Exception('Failed to get price suggestion: ${res.statusCode}');
  }

  // ── Upload Photo from Device or Camera ────────────────────
  Future<String> uploadPhoto(String filePath) async {
    try {
      final request = http.MultipartRequest('POST', Uri.parse('$baseUrl/upload'));
      final token = getToken();
      if (token != null && token.isNotEmpty) {
        request.headers['Authorization'] = 'Bearer $token';
      }
      request.files.add(await http.MultipartFile.fromPath('file', filePath));
      final streamedResponse = await request.send();
      final response = await http.Response.fromStream(streamedResponse);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        final url = data['url'] as String;
        if (url.startsWith('/')) {
          final uri = Uri.parse(baseUrl);
          return '${uri.scheme}://${uri.host}:${uri.port}$url';
        }
        return url;
      }
    } catch (e) {
      debugPrint('Direct upload failed, falling back to base64 data URI: $e');
    }

    try {
      final file = File(filePath);
      final bytes = await file.readAsBytes();
      final base64String = base64Encode(bytes);
      return 'data:image/jpeg;base64,$base64String';
    } catch (e) {
      debugPrint('Failed to convert file to data URI: $e');
      rethrow;
    }
  }

  // ── Quick Fair-Price Estimation (Component A Agentic AI) ───
  Future<Map<String, dynamic>> getQuickPriceEstimate({
    required String cropId,
    required String regionId,
    String? cropName,
    String? regionName,
    String grade = 'A',
    double quantity = 100,
  }) async {
    try {
      final queryParams = <String, String>{
        'cropId': cropId,
        'regionId': regionId,
        'grade': grade,
        'quantity': quantity.toString(),
      };
      if (cropName != null) queryParams['cropName'] = cropName;
      if (regionName != null) queryParams['regionName'] = regionName;

      final uri = Uri.parse('$baseUrl/prices/estimate').replace(queryParameters: queryParams);
      final res = await http.get(uri, headers: _headers);

      if (res.statusCode == 200) {
        return jsonDecode(res.body) as Map<String, dynamic>;
      }
    } catch (e) {
      debugPrint('Error getting quick price estimate: $e');
    }

    // Safe fallback benchmark
    return {
      'suggestedPriceMin': 220.0,
      'suggestedPriceMax': 260.0,
      'averagePrice': 240.0,
      'confidence': 0.88,
      'reasoningSummary': 'Estimated from wholesale baseline price corridor.'
    };
  }

  // Safe fallback mock reference data when API server isn't running yet
  List<Crop> _fallbackCrops() => [
        Crop(id: 'c0000001-0000-0000-0000-000000000001', name: 'Carrot', category: 'Vegetable'),
        Crop(id: 'c0000001-0000-0000-0000-000000000002', name: 'Tomato', category: 'Vegetable'),
        Crop(id: 'c0000001-0000-0000-0000-000000000003', name: 'Leek', category: 'Vegetable'),
        Crop(id: 'c0000001-0000-0000-0000-000000000004', name: 'Cabbage', category: 'Vegetable'),
        Crop(id: 'c0000001-0000-0000-0000-000000000005', name: 'Green Chili', category: 'Spices'),
        Crop(id: 'c0000001-0000-0000-0000-000000000006', name: 'Red Onion', category: 'Vegetable'),
      ];

  List<Region> _fallbackRegions() => [
        Region(id: 'r0000001-0000-0000-0000-000000000001', name: 'Nuwara Eliya'),
        Region(id: 'r0000001-0000-0000-0000-000000000002', name: 'Dambulla'),
        Region(id: 'r0000001-0000-0000-0000-000000000003', name: 'Jaffna'),
        Region(id: 'r0000001-0000-0000-0000-000000000004', name: 'Embilipitiya'),
      ];
}
