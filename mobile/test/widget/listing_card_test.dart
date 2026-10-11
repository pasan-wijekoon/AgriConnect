import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:agriconnect_mobile/widgets/listing_card.dart';
import '../helpers/mocks.dart';

void main() {
  // MOB-W-03
  testWidgets('listing card displays crop, floor price, grade, and quantity', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ListingCard(listing: fakeListing(), onTap: () {}),
        ),
      ),
    );

    expect(find.text('Carrot'), findsNWidgets(2));
    expect(find.text('LKR 240 / kg'), findsOneWidget);
    expect(find.text('Grade A'), findsOneWidget);
    expect(find.text('12 kg'), findsOneWidget);
  });
}
