import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:agriconnect_mobile/main.dart';

const _farmerEmail = 'farmer@agriconnect.lk';
const _testPassword = String.fromEnvironment('TEST_ACCOUNT_PASSWORD');

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // MOB-E-01
  testWidgets('Farmer signs in and creates a produce listing', (tester) async {
    expect(
      _testPassword,
      isNotEmpty,
      reason: 'Pass --dart-define=TEST_ACCOUNT_PASSWORD=<test-password>.',
    );
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const AgriConnectApp());
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );

    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );
    await tester.enterText(find.byType(TextFormField).at(0), _farmerEmail);
    await tester.enterText(find.byType(TextFormField).at(1), _testPassword);
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );

    expect(find.textContaining('Good '), findsOneWidget);
    await tester.tap(find.text('Listings').last);
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );
    await tester.tap(find.text('New listing'));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );

    expect(find.text('New Produce Listing (FR3)'), findsOneWidget);
    await tester.enterText(find.byType(TextFormField).first, '5');
    await tester.ensureVisible(find.byKey(const Key('submitListingButton')));
    await tester.tap(find.byKey(const Key('submitListingButton')));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 45),
    );

    expect(find.text('Listing Specifications'), findsOneWidget);
    expect(find.text('PendingApproval'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );
    expect(find.text('My Produce Listings'), findsOneWidget);
    expect(find.byType(ListView), findsWidgets);
  });
}
