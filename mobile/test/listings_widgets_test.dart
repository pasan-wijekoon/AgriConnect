import 'dart:async';

import 'package:agriconnect_mobile/models/listing.dart';
import 'package:agriconnect_mobile/providers/session_provider.dart';
import 'package:agriconnect_mobile/screens/browse_listings_screen.dart';
import 'package:agriconnect_mobile/widgets/listing_card.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'utils/mobile_test_fakes.dart';

Future<void> _pumpBrowse(WidgetTester tester, TestApiService api) async {
  tester.view.physicalSize = const Size(900, 1800);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final session = SessionProvider();
  addTearDown(session.dispose);
  await tester.pumpWidget(
    ChangeNotifierProvider<SessionProvider>.value(
      value: session,
      child: MaterialApp(home: BrowseListingsScreen(apiService: api)),
    ),
  );
}

PagedListings _page(List<Listing> listings) => PagedListings(
  items: listings,
  totalCount: listings.length,
  page: 1,
  pageSize: 10,
);

void main() {
  testWidgets(
    'MOB-W-03 – listing card shows crop, price, grade, and quantity',
    (tester) async {
      final listing = makeTestListing();
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ListingCard(listing: listing, onTap: () {}),
          ),
        ),
      );

      expect(find.text('Tomato'), findsWidgets);
      expect(find.text('LKR 150 / kg'), findsOneWidget);
      expect(find.text('Grade A'), findsOneWidget);
      expect(find.text('25 kg'), findsOneWidget);
    },
  );

  testWidgets('MOB-W-04 – tapping a listing opens its detail screen', (
    tester,
  ) async {
    final listing = makeTestListing();
    final api = TestApiService(listings: [listing], listingDetails: listing);
    await _pumpBrowse(tester, api);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Tomato').first);
    await tester.pumpAndSettle();

    expect(find.text('Listing Specifications'), findsOneWidget);
    expect(find.text('Tomato'), findsWidgets);
  });

  testWidgets('MOB-W-05 – loading completes into the empty state', (
    tester,
  ) async {
    final result = Completer<PagedListings>();
    final api = TestApiService()..loadListings = () => result.future;
    await _pumpBrowse(tester, api);
    await tester.pump();

    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    result.complete(_page([]));
    await tester.pumpAndSettle();

    expect(find.text('No listings found'), findsOneWidget);
  });

  testWidgets('MOB-W-05 – an empty response displays the empty state', (
    tester,
  ) async {
    await _pumpBrowse(tester, TestApiService());
    await tester.pumpAndSettle();

    expect(find.text('No listings found'), findsOneWidget);
  });

  testWidgets('MOB-W-05 – API failure shows an error and refresh retries', (
    tester,
  ) async {
    var shouldFail = true;
    final listing = makeTestListing();
    final api = TestApiService()
      ..loadListings = () async {
        if (shouldFail) throw StateError('offline');
        return _page([listing]);
      };
    await _pumpBrowse(tester, api);
    await tester.pumpAndSettle();

    expect(find.textContaining('Error loading listings'), findsOneWidget);

    shouldFail = false;
    await tester.tap(find.byTooltip('Refresh listings'));
    await tester.pumpAndSettle();

    expect(find.byType(ListingCard), findsOneWidget);
  });
}
