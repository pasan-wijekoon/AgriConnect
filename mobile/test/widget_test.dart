import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:agriconnect_mobile/main.dart';
import 'package:agriconnect_mobile/services/price_trend_service.dart';

void main() {
  testWidgets('App launches on Place Order and navigates the bottom tabs',
      (WidgetTester tester) async {
    final priceTrendService = PriceTrendService(
      client: MockClient((_) async => http.Response('{"crops": [], "regions": []}', 200)),
      baseUrl: 'http://api',
    );
    await tester.pumpWidget(AgriConnectApp(priceTrendService: priceTrendService));
    await tester.pumpAndSettle();

    expect(find.text('Place Order'), findsWidgets);
    expect(find.text('Produce Listing'), findsOneWidget);

    // Tapping "Orders" kicks off a real HTTP fetch (no mock server in this
    // widget test) — pump a single frame rather than pumpAndSettle so the
    // assertion doesn't wait on that network round-trip to resolve.
    await tester.tap(find.text('Orders'));
    await tester.pump();
    expect(find.text('My Orders'), findsOneWidget);

    // Prices (Component D's Price Trends screen, merged in during
    // integration) uses the injected mock service, so this one can safely
    // pumpAndSettle to its loaded empty state.
    await tester.tap(find.text('Prices'));
    await tester.pumpAndSettle();
    expect(find.text('No crops are available yet.'), findsOneWidget);

    await tester.tap(find.text('Me'));
    await tester.pump();
    expect(find.text('Me (Dev Identity)'), findsOneWidget);
  });
}
