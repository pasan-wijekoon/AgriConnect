import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:provider/provider.dart';

import 'package:agriconnect_mobile/providers/session_provider.dart';
import 'package:agriconnect_mobile/screens/browse_listings_screen.dart';
import 'package:agriconnect_mobile/screens/listing_detail_screen.dart';
import 'package:agriconnect_mobile/widgets/listing_card.dart';
import '../helpers/mocks.dart';

void main() {
  // MOB-W-04
  testWidgets('tapping a listing opens its detail screen with listing data', (
    tester,
  ) async {
    final api = MockApiService();
    final listing = fakeListing();
    stubBrowseReferenceData(api);
    stubBrowseListings(api, () async => fakePagedListings([listing]));
    when(() => api.getListingById(listing.id)).thenAnswer((_) async => listing);
    when(
      () => api.getListingInspections(listing.id),
    ).thenAnswer((_) async => []);
    final session = SessionProvider();
    addTearDown(session.dispose);

    await tester.pumpWidget(
      ChangeNotifierProvider<SessionProvider>.value(
        value: session,
        child: MaterialApp(home: BrowseListingsScreen(apiService: api)),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(ListingCard), findsOneWidget);
    await tester.tap(find.byType(ListingCard));
    await tester.pumpAndSettle();

    expect(find.byType(ListingDetailScreen), findsOneWidget);
    expect(find.text('Carrot'), findsNWidgets(2));
    expect(
      find.text('Category: Vegetable · Region: Nuwara Eliya'),
      findsOneWidget,
    );
    expect(find.text('Published'), findsOneWidget);
    verify(() => api.getListingById('listing-1')).called(1);
  });
}
