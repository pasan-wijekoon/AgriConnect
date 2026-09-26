import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:provider/provider.dart';
import 'providers/inspection_provider.dart';
import 'screens/main_shell.dart';
import 'theme/app_theme.dart';

// ─────────────────────────────────────────────────────────────────────────────
// AgriConnect Mobile — Component C Quality Grading & Inspection
// Entry point configuring Provider state management and AgriConnect theme
// ─────────────────────────────────────────────────────────────────────────────

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    await dotenv.load(fileName: ".env");
  } catch (e) {
    debugPrint("Note: .env file not found or failed to load: $e");
  }

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => InspectionProvider()),
      ],
      child: const AgriConnectApp(),
    ),
  );
}

class AgriConnectApp extends StatelessWidget {
  const AgriConnectApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AgriConnect',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(),
      home: const MainShell(),
    );
  }
}
