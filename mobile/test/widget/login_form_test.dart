import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:agriconnect_mobile/screens/auth/login_screen.dart';

void main() {
  // MOB-W-01
  testWidgets(
    'empty login submission shows required-field errors without a session',
    (tester) async {
      await tester.pumpWidget(const MaterialApp(home: LoginScreen()));

      await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
      await tester.pump();

      expect(find.text('Email is required'), findsOneWidget);
      expect(find.text('Password is required'), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );
}
