import 'package:flutter/material.dart';

import 'screens/home/home_screen.dart';
import 'screens/price_trends/price_trends_screen.dart';
import 'services/price_trend_service.dart';

void main() {
  runApp(const AgriConnectApp());
}

class AgriConnectApp extends StatelessWidget {
  const AgriConnectApp({super.key, this.priceTrendService});

  /// Injected in tests; the real API client otherwise.
  final PriceTrendService? priceTrendService;

  static const _seed = Color(0xFF2E7D32);

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AgriConnect',
      theme: ThemeData(colorScheme: ColorScheme.fromSeed(seedColor: _seed)),
      darkTheme: ThemeData(colorScheme: ColorScheme.fromSeed(seedColor: _seed, brightness: Brightness.dark)),
      routes: {
        '/': (_) => const HomeScreen(),
        '/price-trends': (_) => PriceTrendsScreen(service: priceTrendService ?? PriceTrendService()),
      },
    );
  }
}
