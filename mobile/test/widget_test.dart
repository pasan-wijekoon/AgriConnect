import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:agriconnect_mobile/main.dart';
import 'package:agriconnect_mobile/models/inspection_models.dart';
import 'package:agriconnect_mobile/providers/inspection_provider.dart';
import 'package:agriconnect_mobile/services/api_service.dart';
import 'package:agriconnect_mobile/widgets/app_button.dart';
import 'package:agriconnect_mobile/widgets/app_card.dart';
import 'package:agriconnect_mobile/widgets/empty_state.dart';
import 'package:agriconnect_mobile/widgets/error_state.dart';
import 'package:agriconnect_mobile/widgets/info_row.dart';
import 'package:agriconnect_mobile/widgets/listing_card.dart';
import 'package:agriconnect_mobile/widgets/status_badge.dart';

void main() {
  setUpAll(() async {
    dotenv.testLoad(
      fileInput: '''
API_BASE_URL=http://10.0.2.2:5000/api
FARMER_ID=22222222-2222-2222-2222-222222222222
''',
    );
  });

  group('Environment and ApiService Tests', () {
    test('loads baseUrl and farmerId from dotenv correctly', () {
      expect(ApiService.baseUrl, 'http://10.0.2.2:5000/api');
      expect(ApiService.currentFarmerId, '22222222-2222-2222-2222-222222222222');
    });
  });
  group('StatusBadge Tests', () {
    testWidgets('renders Grade A badge correctly', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatusBadge.fromGrade('Grade A'),
          ),
        ),
      );

      expect(find.text('Grade A ✓'), findsOneWidget);
    });

    testWidgets('renders Grade B badge correctly', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatusBadge.fromGrade('Grade B'),
          ),
        ),
      );

      expect(find.text('Grade B'), findsOneWidget);
    });

    testWidgets('renders Published status badge correctly', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatusBadge.fromListingStatus('Published'),
          ),
        ),
      );

      expect(find.text('Published'), findsOneWidget);
    });
  });

  group('InfoRow Tests', () {
    testWidgets('renders label and value', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: InfoRow(
              label: 'Quantity',
              value: '300 kg',
              icon: Icons.scale_outlined,
            ),
          ),
        ),
      );

      expect(find.text('Quantity'), findsOneWidget);
      expect(find.text('300 kg'), findsOneWidget);
      expect(find.byIcon(Icons.scale_outlined), findsOneWidget);
    });
  });

  group('ListingCard Tests', () {
    testWidgets('renders listing details and discrepancy banner', (WidgetTester tester) async {
      const mockListing = ListingSummary(
        id: 'list-003',
        farmerId: 'f1',
        farmerName: 'Sunil Perera',
        farmerPhone: '0771234567',
        cropId: 'c3',
        cropName: 'Bell Peppers',
        category: 'Vegetables',
        regionId: 'r2',
        regionName: 'Central - Kandy',
        quantity: 120.0,
        unit: 'kg',
        claimedGrade: 'Grade A',
        latestConfirmedGrade: 'Grade B',
        pickupWindowStart: '2026-09-25T00:00:00Z',
        pickupWindowEnd: '2026-09-28T00:00:00Z',
        status: 'PendingApproval',
        createdAt: '2026-09-24T00:00:00Z',
        inspectionCount: 1,
        hasUnresolvedDiscrepancy: true,
        listingPhotos: [],
      );

      bool tapped = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ListingCard(
              listing: mockListing,
              onTap: () {
                tapped = true;
              },
            ),
          ),
        ),
      );

      expect(find.text('Bell Peppers'), findsOneWidget);
      expect(find.textContaining('Central - Kandy'), findsOneWidget);
      expect(find.textContaining('120 kg'), findsOneWidget);
      expect(find.textContaining('Grade discrepancy flagged'), findsOneWidget);

      await tester.tap(find.byType(ListingCard));
      expect(tapped, isTrue);
    });
  });

  group('EmptyState & ErrorState Tests', () {
    testWidgets('renders EmptyState with button callback', (WidgetTester tester) async {
      bool actionTriggered = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: EmptyState(
              title: 'No Listings Found',
              description: 'Try adjusting your filters.',
              buttonText: 'Reset Filters',
              onAction: () {
                actionTriggered = true;
              },
            ),
          ),
        ),
      );

      expect(find.text('No Listings Found'), findsOneWidget);
      expect(find.text('Try adjusting your filters.'), findsOneWidget);
      expect(find.text('Reset Filters'), findsOneWidget);

      await tester.tap(find.text('Reset Filters'));
      expect(actionTriggered, isTrue);
    });

    testWidgets('renders ErrorState with retry callback', (WidgetTester tester) async {
      bool retryTriggered = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ErrorState(
              title: 'Connection Error',
              description: 'Could not connect to server.',
              onRetry: () {
                retryTriggered = true;
              },
            ),
          ),
        ),
      );

      expect(find.text('Connection Error'), findsOneWidget);
      expect(find.text('Could not connect to server.'), findsOneWidget);
      expect(find.text('Try Again'), findsOneWidget);

      await tester.tap(find.text('Try Again'));
      expect(retryTriggered, isTrue);
    });
  });

  group('AppButton & AppCard Tests', () {
    testWidgets('renders AppCard with child and tap callback', (WidgetTester tester) async {
      bool tapped = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AppCard(
              onTap: () => tapped = true,
              child: const Text('Card Content'),
            ),
          ),
        ),
      );

      expect(find.text('Card Content'), findsOneWidget);
      await tester.tap(find.text('Card Content'));
      expect(tapped, isTrue);
    });

    testWidgets('renders AppButton with primary variant and triggers onPressed', (WidgetTester tester) async {
      bool pressed = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AppButton(
              text: 'Save Details',
              onPressed: () => pressed = true,
              variant: AppButtonVariant.primary,
            ),
          ),
        ),
      );

      expect(find.text('Save Details'), findsOneWidget);
      await tester.tap(find.text('Save Details'));
      expect(pressed, isTrue);
    });

    testWidgets('renders AppButton in loading state with spinner', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: AppButton(
              text: 'Save Details',
              loadingText: 'Saving...',
              isLoading: true,
              onPressed: () {},
            ),
          ),
        ),
      );

      expect(find.text('Saving...'), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });
  });

  group('AgriConnectApp Navigation Shell Smoke Test', () {
    testWidgets('renders bottom navigation with tabs', (WidgetTester tester) async {
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider(create: (_) => InspectionProvider()),
          ],
          child: const AgriConnectApp(),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('AgriConnect'), findsOneWidget);
      expect(find.text('Home'), findsOneWidget);
      expect(find.text('My Listings'), findsWidgets);
      expect(find.text('Me'), findsOneWidget);
    });
  });
}

