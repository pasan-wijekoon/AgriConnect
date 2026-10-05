import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

import 'package:agriconnect_mobile/screens/prices/today_prices_screen.dart';
import 'package:agriconnect_mobile/services/backend_client.dart';
import 'package:agriconnect_mobile/services/today_prices_service.dart';

Map<String, dynamic> _item(String name, String category, String trend, double change, {String region = 'Kandy'}) => {
      'cropId': name,
      'name': name,
      'category': category,
      'unit': 'kg',
      'region': region,
      'grade': 'A',
      'suggestedPriceMin': 100.0,
      'suggestedPriceMax': 140.0,
      'averagePrice': 120.0,
      'confidence': 0.9,
      'change24h': change,
      'trend': trend,
      'imageUrl': '',
      'reasoning': 'Anchored on the Pettah wholesale benchmark.',
      'benchmarkWholesale': 118.0,
    };

Map<String, dynamic> _response(List<Map<String, dynamic>> items) => {
      'date': 'Today',
      'totalCrops': items.length,
      'selectedGrade': 'A',
      'selectedRegion': 'All Regions',
      'marketStatus': 'Active Trading',
      'items': items,
    };

Future<void> _pump(WidgetTester tester, MockClient client) async {
  final service = TodayPricesService(BackendClient(client: client, getToken: () => 'jwt'));
  await tester.pumpWidget(MaterialApp(home: TodayPricesScreen(service: service)));
  await tester.pumpAndSettle();
}

void main() {
  final tomatoes = _item('Tomatoes', 'Vegetables', 'rising', 31.5);
  final banana = _item('Banana', 'Fruits', 'falling', -23.0);

  testWidgets('shows a card per crop with the price range and a summary', (tester) async {
    final requests = <http.Request>[];
    await _pump(tester, MockClient((request) async {
      requests.add(request);
      return http.Response(jsonEncode(_response([tomatoes, banana])), 200);
    }));

    expect(find.text('Tomatoes'), findsOneWidget);
    expect(find.text('Banana'), findsOneWidget);
    expect(find.text('Rs. 100 – 140 / kg'), findsNWidgets(2));
    expect(find.text('▲ 31.5%'), findsOneWidget);
    expect(find.text('▼ 23.0%'), findsOneWidget);
    expect(requests.single.url.path, '/api/prices/today');
    expect(requests.single.url.queryParameters, {'grade': 'A'});
    expect(requests.single.headers['Authorization'], 'Bearer jwt');
  });

  testWidgets('category chip filters the list without another request', (tester) async {
    var calls = 0;
    await _pump(tester, MockClient((request) async {
      calls++;
      return http.Response(jsonEncode(_response([tomatoes, banana])), 200);
    }));

    await tester.tap(find.widgetWithText(ChoiceChip, 'Fruits'));
    await tester.pumpAndSettle();

    expect(find.text('Banana'), findsOneWidget);
    expect(find.text('Tomatoes'), findsNothing);
    expect(calls, 1);
  });

  testWidgets('grade B refetches with the grade query', (tester) async {
    final requests = <http.Request>[];
    await _pump(tester, MockClient((request) async {
      requests.add(request);
      return http.Response(jsonEncode(_response([tomatoes])), 200);
    }));

    await tester.tap(find.text('B'));
    await tester.pumpAndSettle();

    expect(requests.last.url.queryParameters['grade'], 'B');
  });

  testWidgets('shows the API problem and retries', (tester) async {
    var fail = true;
    await _pump(tester, MockClient((request) async {
      if (fail) {
        return http.Response(jsonEncode({'detail': 'Prices are unavailable.'}), 503);
      }
      return http.Response(jsonEncode(_response([tomatoes])), 200);
    }));

    expect(find.text('Prices are unavailable.'), findsOneWidget);

    fail = false;
    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();

    expect(find.text('Tomatoes'), findsOneWidget);
  });

  testWidgets('tapping a card opens the reasoning', (tester) async {
    await _pump(tester, MockClient((request) async => http.Response(jsonEncode(_response([tomatoes])), 200)));

    await tester.tap(find.text('Tomatoes'));
    await tester.pumpAndSettle();

    expect(find.text('How the AI got this'), findsOneWidget);
    expect(find.text('Anchored on the Pettah wholesale benchmark.'), findsOneWidget);
  });
}
