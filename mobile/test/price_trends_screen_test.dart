import 'dart:convert';

import 'package:agriconnect_mobile/models/price_trend.dart';
import 'package:agriconnect_mobile/screens/price_trends/price_trends_screen.dart';
import 'package:agriconnect_mobile/services/price_trend_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

const tomato = '3f2a0002-0000-0000-0000-000000000002';
const carrot = '3f2a0001-0000-0000-0000-000000000001';

final filters = {
  'crops': [
    {'id': tomato, 'name': 'Tomato'},
    {'id': carrot, 'name': 'Carrot'},
  ],
  'regions': <Object>[],
};

Map<String, Object> point(String period, double avg, {int samples = 20}) =>
    {'period': period, 'avgPrice': avg, 'minPrice': avg - 10, 'maxPrice': avg + 10, 'sampleCount': samples};

// Week of 14 Sep is missing on purpose: a gap, not a zero.
final tomatoTrend = {
  'cropId': tomato,
  'regionId': null,
  'bucket': 'week',
  'points': [
    point('2026-08-24', 118.84),
    point('2026-08-31', 119.08),
    point('2026-09-07', 120.52),
    point('2026-09-21', 125.94, samples: 9),
  ],
};

final emptyTrend = {'cropId': carrot, 'regionId': null, 'bucket': 'week', 'points': <Object>[]};

http.Response json(Object body, [int status = 200]) =>
    http.Response(jsonEncode(body), status, headers: {'content-type': 'application/json'});

Future<void> pumpScreen(WidgetTester tester, MockClient client) async {
  await tester.pumpWidget(MaterialApp(
    home: PriceTrendsScreen(
      service: PriceTrendService(client: client, baseUrl: 'http://api', getToken: () => 'test-jwt'),
      clock: () => DateTime(2026, 9, 27, 14, 30),
    ),
  ));
  await tester.pumpAndSettle();
}

MockClient apiWith({required Map<String, Object?> Function(String cropId) trendFor, List<http.Request>? log}) =>
    MockClient((request) async {
      log?.add(request);
      if (request.url.path == '/api/analytics/filters') return json(filters);
      return json(trendFor(request.url.queryParameters['cropId']!));
    });

void main() {
  testWidgets('shows the current average and change for the first crop', (tester) async {
    final requests = <http.Request>[];
    await pumpScreen(tester, apiWith(trendFor: (_) => tomatoTrend, log: requests));

    expect(find.textContaining('LKR 125.94', findRichText: true), findsOneWidget);
    expect(find.text('Week of 21 Sep · from 9 listings'), findsOneWidget);
    // (125.94 - 118.84) / 118.84 = +5.97%
    expect(find.text('Up 6.0% vs 4 weeks ago'), findsOneWidget);

    final trendRequest = requests.last;
    expect(trendRequest.url.queryParameters,
        {'cropId': tomato, 'from': '2026-06-07', 'to': '2026-09-27', 'bucket': 'week'});
    expect(trendRequest.headers['Authorization'], 'Bearer test-jwt');
  });

  testWidgets('opens on the first crop that has prices, not simply the first crop', (tester) async {
    final requests = <http.Request>[];
    final client = MockClient((request) async {
      requests.add(request);
      if (request.url.path == '/api/analytics/filters') {
        return json({
          'crops': [
            {'id': carrot, 'name': 'Banana', 'hasPriceHistory': false},
            {'id': tomato, 'name': 'Tomato', 'hasPriceHistory': true},
          ],
          'regions': <Object>[],
        });
      }
      return json(tomatoTrend);
    });
    await pumpScreen(tester, client);

    expect(requests.last.url.queryParameters['cropId'], tomato);
    expect(find.textContaining('LKR 125.94', findRichText: true), findsOneWidget);
  });

  testWidgets('switching crop shows an empty state when there is no data', (tester) async {
    await pumpScreen(tester, apiWith(trendFor: (id) => id == tomato ? tomatoTrend : emptyTrend));

    await tester.tap(find.text('Carrot'));
    await tester.pumpAndSettle();

    expect(find.text('No prices recorded for Carrot in the last 16 weeks.'), findsOneWidget);
    expect(find.textContaining('LKR', findRichText: true), findsNothing);
  });

  testWidgets('shows the API error message and recovers on retry', (tester) async {
    var fail = true;
    final client = MockClient((request) async {
      if (fail) return json({'title': 'Forbidden', 'status': 403, 'detail': 'You cannot view prices.'}, 403);
      return request.url.path.endsWith('filters') ? json(filters) : json(tomatoTrend);
    });
    await pumpScreen(tester, client);

    expect(find.text('You cannot view prices.'), findsOneWidget);

    fail = false;
    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();

    expect(find.textContaining('LKR 125.94', findRichText: true), findsOneWidget);
  });

  testWidgets('explains a network failure in plain words', (tester) async {
    await pumpScreen(tester, MockClient((_) async => throw http.ClientException('refused')));

    expect(find.text("Can't reach the AgriConnect server. Check your connection."), findsOneWidget);
  });

  testWidgets('weekly prices table lists every week, newest first', (tester) async {
    // Tall enough that the whole expanded table is on screen.
    tester.view.physicalSize = const Size(800, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);
    await pumpScreen(tester, apiWith(trendFor: (_) => tomatoTrend));

    await tester.tap(find.text('Weekly prices'));
    await tester.pumpAndSettle();

    final weeks = tester.widgetList<Text>(find.textContaining('Week of ')).map((t) => t.data).toList();
    expect(weeks, containsAllInOrder(['Week of 21 Sep', 'Week of 7 Sep', 'Week of 31 Aug', 'Week of 24 Aug']));
  });

  group('PriceTrend', () {
    final trend = PriceTrend.fromJson(tomatoTrend);

    test('weekly series keeps missing weeks as gaps', () {
      final series = trend.weeklySeries;

      expect(series, hasLength(5));
      expect(series[3], isNull);
      expect(series.last!.avgPrice, 125.94);
    });

    test('change is null when the comparison week has no data', () {
      expect(trend.changePercent(weeksBack: 1), isNull);
    });
  });

  test('formatLkr groups thousands', () {
    expect(formatLkr(1234.5), 'LKR 1,234.50');
    expect(formatLkr(85), 'LKR 85.00');
  });
}
