import 'package:flutter_test/flutter_test.dart';

import 'package:agriconnect_mobile/main.dart';

void main() {
  testWidgets('App launches on Place Order and navigates the bottom tabs',
      (WidgetTester tester) async {
    await tester.pumpWidget(const AgriConnectApp());
    await tester.pumpAndSettle();

    expect(find.text('Place Order'), findsWidgets);
    expect(find.text('Produce Listing'), findsOneWidget);

    // Tapping "Orders" kicks off a real HTTP fetch (no mock server in this
    // widget test) — pump a single frame rather than pumpAndSettle so the
    // assertion doesn't wait on that network round-trip to resolve.
    await tester.tap(find.text('Orders'));
    await tester.pump();
    expect(find.text('My Orders'), findsOneWidget);

    await tester.tap(find.text('Me'));
    await tester.pump();
    expect(find.text('Me (Dev Identity)'), findsOneWidget);
  });
}
