import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:agriconnect_mobile/screens/price_trends/price_trends_screen.dart';

void main() {
  testWidgets('price trends screen loads without crashing', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: PriceTrendsScreen()));
    expect(find.text('Market prices'), findsOneWidget);
  });
}
