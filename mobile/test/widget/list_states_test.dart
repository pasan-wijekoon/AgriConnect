import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:agriconnect_mobile/screens/browse_listings_screen.dart';
import '../helpers/mocks.dart';

void main() {
  // MOB-W-05
  testWidgets('marketplace shows loading, empty, error, and can retry', (
    tester,
  ) async {
    final api = MockApiService();
    final firstLoad = Completer();
    var listingRequest = 0;
    stubBrowseReferenceData(api);
    stubBrowseListings(api, () {
      listingRequest++;
      if (listingRequest == 1) {
        return firstLoad.future.then((_) => fakePagedListings());
      }
      if (listingRequest == 2) {
        return Future.error(Exception('service unavailable'));
      }
      return Future.value(fakePagedListings([fakeListing()]));
    });

    await tester.pumpWidget(
      MaterialApp(home: BrowseListingsScreen(apiService: api)),
    );
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    firstLoad.complete();
    await tester.pumpAndSettle();

    expect(find.text('No listings found'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsNothing);

    await tester.tap(find.byTooltip('Refresh listings'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.textContaining('Error loading listings:'), findsOneWidget);
    expect(find.byTooltip('Refresh listings'), findsOneWidget);

    await tester.tap(find.byTooltip('Refresh listings'));
    await tester.pumpAndSettle();

    expect(find.text('Carrot'), findsNWidgets(2));
    expect(listingRequest, 3);
  });
}
