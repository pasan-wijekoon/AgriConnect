import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:agriconnect_mobile/main.dart';
import 'package:agriconnect_mobile/widgets/listing_card.dart';

const _buyerEmail = 'buyer@agriconnect.lk';
const _testPassword = String.fromEnvironment('TEST_ACCOUNT_PASSWORD');

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // MOB-E-02
  testWidgets('Buyer places an order and sees it in order history', (
    tester,
  ) async {
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
    await tester.enterText(find.byType(TextFormField).at(0), _buyerEmail);
    await tester.enterText(find.byType(TextFormField).at(1), _testPassword);
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );

    expect(find.textContaining('Good '), findsOneWidget);
    await tester.tap(find.text('Market').last);
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );
    expect(
      find.byType(ListingCard),
      findsWidgets,
      reason: 'A Published listing must be seeded for the Buyer account.',
    );
    await tester.tap(find.byType(ListingCard).first);
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );

    final cropName = tester.widget<AppBar>(find.byType(AppBar)).title;
    expect(cropName, isA<Text>());
    final orderedCrop = (cropName as Text).data!;
    await tester.tap(find.text('Order $orderedCrop'));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );

    await tester.enterText(find.byType(TextFormField).first, '1');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Place order'));
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 45),
    );

    expect(
      find.text(
        'Order placed! Your stock is reserved while an officer reviews it.',
      ),
      findsOneWidget,
    );
    await tester.pageBack();
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );
    await tester.pageBack();
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 30),
    );
    await tester.tap(find.text('Orders').last);
    await tester.pumpAndSettle(
      const Duration(milliseconds: 250),
      EnginePhase.sendSemanticsUpdate,
      const Duration(seconds: 45),
    );

    expect(find.text('My Orders'), findsOneWidget);
    expect(find.text(orderedCrop), findsWidgets);
    expect(find.text('Pending'), findsWidgets);
  });
}
