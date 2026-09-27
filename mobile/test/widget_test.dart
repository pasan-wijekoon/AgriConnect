import 'package:agriconnect_mobile/main.dart';
import 'package:agriconnect_mobile/services/price_trend_service.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  testWidgets('home links to the market prices screen', (tester) async {
    final client = MockClient((_) async => http.Response('{"crops": [], "regions": []}', 200));
    await tester.pumpWidget(AgriConnectApp(priceTrendService: PriceTrendService(client: client, baseUrl: 'http://api')));

    expect(find.text('AgriConnect'), findsOneWidget);

    await tester.tap(find.text('Market prices'));
    await tester.pumpAndSettle();

    expect(find.text('No crops are available yet.'), findsOneWidget);
  });
}
