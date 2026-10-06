import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:agriconnect_mobile/main.dart';
import 'package:agriconnect_mobile/models/auth_user.dart';
import 'package:agriconnect_mobile/services/price_trend_service.dart';

PriceTrendService _priceService() => PriceTrendService(
      client: MockClient((_) async => http.Response('{"crops": [], "regions": []}', 200)),
      baseUrl: 'http://api',
    );

void _signedInAs(String role, String name) {
  final user = AuthUser(
      id: 'test-user-id', fullName: name, email: '$role@agriconnect.lk', role: role, token: 'test-token');
  SharedPreferences.setMockInitialValues({'agriconnect_auth_user': jsonEncode(user.toJson())});
}

Future<void> _launch(WidgetTester tester) async {
  // A phone-sized, tall viewport so nothing under test is scrolled off-screen.
  tester.view.physicalSize = const Size(600, 1600);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(AgriConnectApp(priceTrendService: _priceService()));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('signed-out users see the landing page with sign-in and register', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await _launch(tester);

    expect(find.text('AgriConnect'), findsOneWidget);
    expect(find.text('Sign in'), findsOneWidget);
    expect(find.text('Create an account'), findsOneWidget);
    expect(find.text('Track every step'), findsOneWidget);

    await tester.tap(find.text('Create an account'));
    await tester.pumpAndSettle();
    expect(find.text('Create account'), findsWidgets);
    expect(find.text('Buyer'), findsOneWidget);
    expect(find.text('Farmer'), findsOneWidget);
  });

  testWidgets('a farmer lands on Home and gets five role-specific tabs', (tester) async {
    _signedInAs('Farmer', 'Kamal Perera');
    await _launch(tester);

    // Landing dashboard, not a form.
    expect(find.textContaining('Kamal'), findsWidgets);
    expect(find.text('Quick actions'), findsOneWidget);

    // Farmer tabs; no dev-identity "Me" tab, no buyer-only Market/Centres.
    for (final label in ['Home', 'Listings', 'Orders', 'Prices', 'Account']) {
      expect(find.text(label), findsWidgets, reason: label);
    }
    expect(find.text('Me'), findsNothing);
    expect(find.text('Market'), findsNothing);

    await tester.tap(find.text('Orders').last);
    await tester.pump();
    expect(find.text('Incoming Orders'), findsOneWidget);

    await tester.tap(find.text('Prices').last);
    await tester.pumpAndSettle();
    expect(find.text('No crops are available yet.'), findsOneWidget);

    await tester.tap(find.text('Account').last);
    await tester.pumpAndSettle();
    expect(find.text('Kamal Perera'), findsOneWidget);
    expect(find.text('Farmer'), findsWidgets);
  });

  testWidgets('a buyer gets Market and Centres tabs and can sign out', (tester) async {
    _signedInAs('Buyer', 'Nihal Fernando');
    await _launch(tester);

    for (final label in ['Home', 'Market', 'Orders', 'Centres', 'Account']) {
      expect(find.text(label), findsWidgets, reason: label);
    }
    expect(find.text('Listings'), findsNothing);

    await tester.tap(find.text('Orders').last);
    await tester.pump();
    expect(find.text('My Orders'), findsOneWidget);

    await tester.tap(find.text('Account').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Sign out').first);
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'Sign out'));
    await tester.pumpAndSettle();

    expect(find.text('Create an account'), findsOneWidget);
  });

  testWidgets('officers are pointed to the web console instead of the mobile app', (tester) async {
    _signedInAs('Officer', 'Kandy Centre Officer');
    await _launch(tester);

    expect(find.textContaining('web console'), findsWidgets);
    expect(find.text('Sign out'), findsOneWidget);
  });
}
