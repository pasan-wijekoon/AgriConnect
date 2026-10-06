import 'package:agriconnect_mobile/screens/create_listing_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../utils/mobile_test_fakes.dart';

Future<void> _pumpForm(WidgetTester tester, TestApiService api) async {
  tester.view.physicalSize = const Size(900, 1800);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(
    MaterialApp(home: CreateListingScreen(apiService: api)),
  );
  await tester.pumpAndSettle();
}

void main() {
  for (final price in ['-5', 'abc']) {
    testWidgets('MOB-W-02 – $price price blocks listing submission', (
      tester,
    ) async {
      final api = TestApiService();
      await _pumpForm(tester, api);

      final priceField = find.byKey(const Key('floorPriceField'));
      await tester.ensureVisible(priceField);
      await tester.enterText(priceField, price);
      await tester.ensureVisible(find.byKey(const Key('submitListingButton')));
      await tester.tap(find.byKey(const Key('submitListingButton')));
      await tester.pump();

      expect(find.text('Enter a valid positive price'), findsOneWidget);
      expect(api.createListingCalls, 0);
    });
  }
}
