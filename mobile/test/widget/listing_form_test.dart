import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:agriconnect_mobile/screens/create_listing_screen.dart';
import '../helpers/mocks.dart';

void main() {
  late MockApiService api;

  setUp(() {
    api = MockApiService();
    stubBrowseReferenceData(api);
    when(
      () => api.getQuickPriceEstimate(
        cropId: any(named: 'cropId'),
        regionId: any(named: 'regionId'),
        cropName: any(named: 'cropName'),
        regionName: any(named: 'regionName'),
        grade: any(named: 'grade'),
        quantity: any(named: 'quantity'),
      ),
    ).thenAnswer(
      (_) async => {'suggestedPriceMin': 200, 'suggestedPriceMax': 250},
    );
  });

  // MOB-W-02
  testWidgets(
    'negative and non-numeric floor prices show errors and block listing submission',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(home: CreateListingScreen(apiService: api)),
      );
      await tester.pumpAndSettle();

      final priceField = find.byKey(const Key('floorPriceField'));
      await tester.ensureVisible(priceField);
      await tester.enterText(priceField, '-5');
      await tester.ensureVisible(find.byKey(const Key('submitListingButton')));
      await tester.tap(find.byKey(const Key('submitListingButton')));
      await tester.pump();

      expect(find.text('Enter a valid positive price'), findsOneWidget);

      await tester.ensureVisible(priceField);
      await tester.enterText(priceField, 'not-a-number');
      await tester.ensureVisible(find.byKey(const Key('submitListingButton')));
      await tester.tap(find.byKey(const Key('submitListingButton')));
      await tester.pump();

      expect(find.text('Enter a valid positive price'), findsOneWidget);
      verifyNever(
        () => api.createListing(
          cropId: any(named: 'cropId'),
          regionId: any(named: 'regionId'),
          quantity: any(named: 'quantity'),
          unit: any(named: 'unit'),
          claimedGrade: any(named: 'claimedGrade'),
          pickupWindowStart: any(named: 'pickupWindowStart'),
          pickupWindowEnd: any(named: 'pickupWindowEnd'),
          minPrice: any(named: 'minPrice'),
          photoUrls: any(named: 'photoUrls'),
        ),
      );
    },
  );
}
