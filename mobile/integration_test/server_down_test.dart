import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:agriconnect_mobile/main.dart';
import 'package:agriconnect_mobile/models/auth_user.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // MOB-E-03
  testWidgets('unreachable backend shows a friendly marketplace error', (
    tester,
  ) async {
    final offlineUser = AuthUser(
      id: 'offline-test-buyer',
      fullName: 'Offline Test Buyer',
      email: 'buyer@agriconnect.lk',
      role: 'Buyer',
      token: 'offline-test-token',
    );
    SharedPreferences.setMockInitialValues({
      'agriconnect_auth_user': jsonEncode(offlineUser.toJson()),
    });

    await tester.pumpWidget(const AgriConnectApp());
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

    expect(find.text('Marketplace'), findsOneWidget);
    expect(find.textContaining('Error loading listings:'), findsOneWidget);
    expect(find.byTooltip('Refresh listings'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
