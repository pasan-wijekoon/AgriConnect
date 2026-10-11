import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:agriconnect_mobile/main.dart';

void main() {
  // APP-SMOKE-01
  testWidgets('the app starts on the welcome page when signed out', (
    WidgetTester tester,
  ) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AgriConnectApp());
    await tester.pumpAndSettle();

    expect(find.text('AgriConnect'), findsOneWidget);
    expect(find.text('Create an account'), findsOneWidget);
  });
}
