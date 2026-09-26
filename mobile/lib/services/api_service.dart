import 'dart:convert';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import '../models/inspection_models.dart';

// ─────────────────────────────────────────────────────────────────────────────
// AgriConnect Mobile API Service — Component C
//
// Dual-mode strategy (mirrors web/src/services/api.ts):
//   1. Attempt real HTTP call to backend (configured via .env: API_BASE_URL)
//   2. On failure: fall back to rich in-memory mock data
//
// This guarantees screens are never blank, even without the backend running.
// ─────────────────────────────────────────────────────────────────────────────

String get _configuredBaseUrl {
  final url = dotenv.env['API_BASE_URL']?.trim();
  if (url != null && url.isNotEmpty) {
    return url.endsWith('/') ? url.substring(0, url.length - 1) : url;
  }
  return 'http://10.0.2.2:5000/api';
}

String get _currentFarmerId {
  final id = dotenv.env['FARMER_ID']?.trim();
  if (id != null && id.isNotEmpty) {
    return id;
  }
  return '22222222-2222-2222-2222-222222222222';
}

// Fallback IDs matching seeded test data in DbSeeder.cs
const String _mockFarmerId = '22222222-2222-2222-2222-222222222222';
const String _mockOfficerId = '11111111-1111-1111-1111-111111111111';

// ─────────────────────────────────────────────────────────────────────────────
// In-memory mock data — matches web/src/services/api.ts mock data exactly
// ─────────────────────────────────────────────────────────────────────────────

List<ListingSummary> _mockListings = [
  ListingSummary(
    id: 'e1111111-1111-1111-1111-111111111111',
    farmerId: _mockFarmerId,
    farmerName: 'Sunil Perera',
    farmerPhone: '+94 71 987 6543',
    cropId: 'd1111111-0000-0000-0000-000000000001',
    cropName: 'Tomatoes (Thalathuoya)',
    category: 'Vegetables',
    regionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    regionName: 'Western - Colombo',
    quantity: 250,
    unit: 'kg',
    claimedGrade: 'Grade A',
    pickupWindowStart: DateTime.now().add(const Duration(days: 1)).toIso8601String(),
    pickupWindowEnd: DateTime.now().add(const Duration(days: 3)).toIso8601String(),
    status: 'PendingApproval',
    minPrice: 280.00,
    createdAt: DateTime.now().subtract(const Duration(hours: 4)).toIso8601String(),
    inspectionCount: 0,
    hasUnresolvedDiscrepancy: false,
    listingPhotos: [
      'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=600&q=80',
    ],
  ),
  ListingSummary(
    id: 'e2222222-2222-2222-2222-222222222222',
    farmerId: _mockFarmerId,
    farmerName: 'Sunil Perera',
    farmerPhone: '+94 71 987 6543',
    cropId: 'd1111111-0000-0000-0000-000000000002',
    cropName: 'Carrots (Nuwara Eliya)',
    category: 'Vegetables',
    regionId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    regionName: 'Western - Gampaha',
    quantity: 180,
    unit: 'kg',
    claimedGrade: 'Grade A',
    pickupWindowStart: DateTime.now().add(const Duration(days: 2)).toIso8601String(),
    pickupWindowEnd: DateTime.now().add(const Duration(days: 4)).toIso8601String(),
    status: 'PendingApproval',
    minPrice: 320.00,
    createdAt: DateTime.now().subtract(const Duration(hours: 12)).toIso8601String(),
    inspectionCount: 0,
    hasUnresolvedDiscrepancy: false,
    listingPhotos: [
      'https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=600&q=80',
    ],
  ),
  ListingSummary(
    id: 'e3333333-3333-3333-3333-333333333333',
    farmerId: _mockFarmerId,
    farmerName: 'Sunil Perera',
    farmerPhone: '+94 71 987 6543',
    cropId: 'd1111111-0000-0000-0000-000000000004',
    cropName: 'Bell Peppers (Yellow/Red)',
    category: 'Vegetables',
    regionId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    regionName: 'Central - Kandy',
    quantity: 120,
    unit: 'kg',
    claimedGrade: 'Grade A',
    latestConfirmedGrade: 'Grade B',
    pickupWindowStart: DateTime.now().add(const Duration(days: 1)).toIso8601String(),
    pickupWindowEnd: DateTime.now().add(const Duration(days: 2)).toIso8601String(),
    status: 'PendingApproval',
    minPrice: 450.00,
    createdAt: DateTime.now().subtract(const Duration(days: 1)).toIso8601String(),
    inspectionCount: 1,
    hasUnresolvedDiscrepancy: true,
    listingPhotos: [
      'https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=600&q=80',
    ],
  ),
  ListingSummary(
    id: 'e4444444-4444-4444-4444-444444444444',
    farmerId: _mockFarmerId,
    farmerName: 'Sunil Perera',
    farmerPhone: '+94 71 987 6543',
    cropId: 'd1111111-0000-0000-0000-000000000003',
    cropName: 'Green Beans',
    category: 'Vegetables',
    regionId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    regionName: 'Western - Colombo',
    quantity: 300,
    unit: 'kg',
    claimedGrade: 'Grade A',
    latestConfirmedGrade: 'Grade A',
    pickupWindowStart: DateTime.now().add(const Duration(days: 1)).toIso8601String(),
    pickupWindowEnd: DateTime.now().add(const Duration(days: 5)).toIso8601String(),
    status: 'Published',
    minPrice: 210.00,
    createdAt: DateTime.now().subtract(const Duration(days: 2)).toIso8601String(),
    inspectionCount: 1,
    hasUnresolvedDiscrepancy: false,
    listingPhotos: [
      'https://images.unsplash.com/photo-1551893665-f843f600794e?auto=format&fit=crop&w=600&q=80',
    ],
  ),
];

final List<InspectionRecord> _mockInspections = [
  InspectionRecord(
    id: 'insp-001',
    listingId: 'e3333333-3333-3333-3333-333333333333',
    cropName: 'Bell Peppers (Yellow/Red)',
    quantity: 120,
    unit: 'kg',
    claimedGrade: 'Grade A',
    confirmedGrade: 'Grade B',
    notes:
        'Produce size is inconsistent with Grade A export standards (diameter varies between 4–7 cm). '
        'Slight skin blemishes on ~15% of samples. Downgraded to Grade B standard commercial grade.',
    inspectedAt: DateTime.now().subtract(const Duration(hours: 8)).toIso8601String(),
    officerId: _mockOfficerId,
    officerName: 'Kamal Gunawardena',
    farmerName: 'Sunil Perera',
    regionName: 'Central - Kandy',
    listingStatus: 'PendingApproval',
    hasDiscrepancy: true,
    photos: [
      InspectionPhoto(
        id: 'p1',
        url: 'https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=600&q=80',
        uploadedAt: DateTime.now().subtract(const Duration(hours: 8)).toIso8601String(),
      ),
    ],
  ),
  InspectionRecord(
    id: 'insp-002',
    listingId: 'e4444444-4444-4444-4444-444444444444',
    cropName: 'Green Beans',
    quantity: 300,
    unit: 'kg',
    claimedGrade: 'Grade A',
    confirmedGrade: 'Grade A',
    notes:
        'Fresh harvest, uniform green colour, crisp pods, zero pest damage. '
        'Passed Grade A verification criteria.',
    inspectedAt: DateTime.now().subtract(const Duration(days: 1)).toIso8601String(),
    officerId: _mockOfficerId,
    officerName: 'Kamal Gunawardena',
    farmerName: 'Sunil Perera',
    regionName: 'Western - Colombo',
    listingStatus: 'Published',
    hasDiscrepancy: false,
    photos: [
      InspectionPhoto(
        id: 'p2',
        url: 'https://images.unsplash.com/photo-1551893665-f843f600794e?auto=format&fit=crop&w=600&q=80',
        uploadedAt: DateTime.now().subtract(const Duration(days: 1)).toIso8601String(),
      ),
    ],
  ),
];

// ─────────────────────────────────────────────────────────────────────────────
// HTTP helper
// ─────────────────────────────────────────────────────────────────────────────

Future<T> _apiRequest<T>(
  String endpoint,
  T Function(dynamic) parser, {
  String method = 'GET',
  Map<String, dynamic>? body,
}) async {
  final base = _configuredBaseUrl;
  final uri = Uri.parse('$base$endpoint');
  final headers = {
    'Content-Type': 'application/json',
    // Farmer auth header matching current environment/session
    'X-Farmer-Id': _currentFarmerId,
  };

  http.Response response;
  switch (method) {
    case 'POST':
      response = await http
          .post(uri, headers: headers, body: json.encode(body))
          .timeout(const Duration(seconds: 8));
    default:
      response = await http.get(uri, headers: headers).timeout(const Duration(seconds: 8));
  }

  if (response.statusCode >= 200 && response.statusCode < 300) {
    return parser(json.decode(response.body));
  }
  throw Exception('HTTP ${response.statusCode}: ${response.body}');
}

// ─────────────────────────────────────────────────────────────────────────────
// Public API service
// ─────────────────────────────────────────────────────────────────────────────

class ApiService {
  ApiService._();

  /// Returns the configured backend API base URL from `.env`
  static String get baseUrl => _configuredBaseUrl;

  /// Returns the configured farmer ID from `.env`
  static String get currentFarmerId => _currentFarmerId;

  // ── Dashboard stats ──────────────────────────────────────────────────────

  static Future<QualityDashboardStats> getDashboardStats() async {
    try {
      return await _apiRequest<QualityDashboardStats>(
        '/inspections/stats',
        (data) => QualityDashboardStats.fromJson(data as Map<String, dynamic>),
      );
    } catch (e) {
      // Mock fallback
      final pending = _mockListings.where((l) => l.status == 'PendingApproval' && !l.hasBeenInspected).length;
      final discrepancies = _mockListings.where((l) => l.hasUnresolvedDiscrepancy).length;
      final published = _mockListings.where((l) => l.status == 'Published').length;
      return QualityDashboardStats(
        pendingInspections: pending,
        completedToday: 1,
        totalInspections: _mockInspections.length,
        activeDiscrepancies: discrepancies,
        publishedListings: published,
        gradeAComplianceRate: 66.7,
      );
    }
  }

  // ── Farmer's own listings (FR11 — order status tracking; FR13 — inspection history) ──

  static Future<List<ListingSummary>> getMyListings() async {
    try {
      return await _apiRequest<List<ListingSummary>>(
        '/listings/my-listings',
        (data) => (data as List<dynamic>)
            .map((item) => ListingSummary.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
    } catch (e) {
      // Mock fallback: return all mock listings (all belong to the mock farmer)
      return List<ListingSummary>.from(_mockListings);
    }
  }

  // ── Inspection history for a specific listing (FR13) ─────────────────────

  static Future<List<InspectionRecord>> getListingInspections(String listingId) async {
    try {
      return await _apiRequest<List<InspectionRecord>>(
        '/listings/$listingId/inspections',
        (data) => (data as List<dynamic>)
            .map((item) => InspectionRecord.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
    } catch (e) {
      // Mock fallback
      return _mockInspections.where((i) => i.listingId == listingId).toList();
    }
  }

  // ── Single inspection detail ──────────────────────────────────────────────

  static Future<InspectionRecord?> getInspectionDetail(String inspectionId) async {
    try {
      return await _apiRequest<InspectionRecord>(
        '/inspections/$inspectionId',
        (data) => InspectionRecord.fromJson(data as Map<String, dynamic>),
      );
    } catch (e) {
      // Mock fallback
      try {
        return _mockInspections.firstWhere((i) => i.id == inspectionId);
      } catch (_) {
        return null;
      }
    }
  }
}
