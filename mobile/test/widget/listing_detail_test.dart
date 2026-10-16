import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:agriconnect_mobile/models/listing.dart';
import 'package:agriconnect_mobile/models/listing_inspection_status.dart';
import 'package:agriconnect_mobile/screens/listing_detail_screen.dart';
import '../helpers/mocks.dart';

void main() {
  group('ListingDetailScreen — Inspection History (FR13)', () {
    late MockApiService api;
    late Listing listing;

    setUp(() {
      api = MockApiService();
      listing = fakeListing(id: 'listing-1', cropName: 'Carrot');
      when(() => api.getListingById('listing-1')).thenAnswer((_) async => listing);
    });

    testWidgets('shows loading indicator while fetching inspections', (tester) async {
      final completer = Completer<List<ListingInspectionRecord>>();
      when(() => api.getListingInspections('listing-1')).thenAnswer((_) => completer.future);

      await tester.pumpWidget(
        MaterialApp(home: ListingDetailScreen(listingId: 'listing-1', apiService: api)),
      );
      await tester.pump(); // trigger initState

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('This listing has not been inspected by an officer yet.'), findsNothing);

      completer.complete([]);
      await tester.pumpAndSettle();
    });

    testWidgets('shows "not inspected yet" when loading succeeds and list is empty', (tester) async {
      when(() => api.getListingInspections('listing-1')).thenAnswer((_) async => []);

      await tester.pumpWidget(
        MaterialApp(home: ListingDetailScreen(listingId: 'listing-1', apiService: api)),
      );
      await tester.pumpAndSettle();

      expect(find.byType(CircularProgressIndicator), findsNothing);
      expect(find.text('This listing has not been inspected by an officer yet.'), findsOneWidget);
    });

    testWidgets('shows inspection record with correct grade (not "Grade Grade A")', (tester) async {
      final inspection = ListingInspectionRecord(
        id: 'insp-1',
        confirmedGrade: 'Grade A',
        notes: 'Looks good',
        inspectedAt: DateTime.utc(2026, 10, 15),
        officerName: 'Officer One',
        hasDiscrepancy: false,
      );
      when(() => api.getListingInspections('listing-1')).thenAnswer((_) async => [inspection]);

      await tester.pumpWidget(
        MaterialApp(home: ListingDetailScreen(listingId: 'listing-1', apiService: api)),
      );
      await tester.pumpAndSettle();

      // Should show "Grade A" not "Grade Grade A"
      // Note: claimed grade also shows "Grade A" in specs, so we check the inspection card specifically
      expect(find.text('Grade A'), findsAtLeastNWidgets(1));
      expect(find.text('Grade Grade A'), findsNothing);
      expect(find.text('Inspected by Officer One'), findsOneWidget);
      expect(find.text('Looks good'), findsOneWidget);
    });

    testWidgets('shows error with Retry button on failure', (tester) async {
      when(() => api.getListingInspections('listing-1'))
          .thenThrow(Exception('Failed to load inspections: 403 Forbidden'));

      await tester.pumpWidget(
        MaterialApp(home: ListingDetailScreen(listingId: 'listing-1', apiService: api)),
      );
      await tester.pumpAndSettle();

      expect(find.byType(CircularProgressIndicator), findsNothing);
      expect(find.text('Failed to load inspection history.'), findsOneWidget);
      expect(find.widgetWithText(OutlinedButton, 'Retry'), findsOneWidget);
    });

    testWidgets('tapping Retry re-loads inspections', (tester) async {
      var callCount = 0;
      when(() => api.getListingInspections('listing-1')).thenAnswer((_) async {
        callCount++;
        if (callCount == 1) throw Exception('Network error');
        return [];
      });

      await tester.pumpWidget(
        MaterialApp(home: ListingDetailScreen(listingId: 'listing-1', apiService: api)),
      );
      await tester.pumpAndSettle();

      // First load failed
      expect(find.text('Failed to load inspection history.'), findsOneWidget);
      final retryButton = find.widgetWithText(OutlinedButton, 'Retry');
      expect(retryButton, findsOneWidget);

      // Tap Retry - ensure it's visible by scrolling
      await tester.ensureVisible(retryButton);
      await tester.tap(retryButton);
      await tester.pumpAndSettle();

      // Second load succeeded (empty list)
      expect(find.text('This listing has not been inspected by an officer yet.'), findsOneWidget);
      expect(callCount, 2);
    });

    testWidgets('shows discrepancy warning when hasDiscrepancy is true', (tester) async {
      final inspection = ListingInspectionRecord(
        id: 'insp-2',
        confirmedGrade: 'Grade B',
        notes: 'Grade differs from claimed',
        inspectedAt: DateTime.utc(2026, 10, 15),
        officerName: 'Officer Two',
        hasDiscrepancy: true,
      );
      when(() => api.getListingInspections('listing-1')).thenAnswer((_) async => [inspection]);

      await tester.pumpWidget(
        MaterialApp(home: ListingDetailScreen(listingId: 'listing-1', apiService: api)),
      );
      await tester.pumpAndSettle();

      expect(find.text('Grade B'), findsOneWidget);
      expect(find.text('Confirmed grade differs from the claimed grade.'), findsOneWidget);
      // Card should have orange styling for discrepancy
      expect(find.byWidgetPredicate((widget) => widget is Card && widget.color == Colors.orange.shade50), findsOneWidget);
    });
  });
}