import 'package:flutter/foundation.dart';

import '../models/price_trend.dart';
import '../services/price_trend_service.dart';

/// State for the read-only price trend screen.
class PriceTrendProvider extends ChangeNotifier {
  PriceTrendProvider(this._service, {DateTime Function()? clock}) : _clock = clock ?? DateTime.now;

  static const weeks = 16;

  final PriceTrendService _service;
  final DateTime Function() _clock;

  List<NamedItem> crops = const [];
  NamedItem? selectedCrop;
  PriceTrend? trend;
  bool loading = false;
  String? error;

  // Ignores responses that arrive after the farmer has already picked another crop.
  int _request = 0;

  Future<void> load() async {
    await _run(() async {
      if (crops.isEmpty) {
        crops = await _service.fetchCrops();
        // Open on a crop that has prices; the first alphabetically may have none yet.
        selectedCrop ??= crops.where((c) => c.hasPriceHistory).firstOrNull ?? crops.firstOrNull;
      }
      final crop = selectedCrop;
      final result = crop == null ? null : await _fetchTrend(crop);
      return () => trend = result;
    });
  }

  Future<void> selectCrop(NamedItem crop) async {
    if (crop.id == selectedCrop?.id) return;
    selectedCrop = crop;
    await _run(() async {
      final result = await _fetchTrend(crop);
      return () => trend = result;
    });
  }

  Future<PriceTrend> _fetchTrend(NamedItem crop) {
    final today = _clock();
    final to = DateTime(today.year, today.month, today.day);
    return _service.fetchTrend(cropId: crop.id, from: to.subtract(const Duration(days: 7 * weeks)), to: to);
  }

  /// Runs a fetch; its returned callback applies the result only if no newer request started.
  Future<void> _run(Future<void Function()> Function() fetch) async {
    final request = ++_request;
    loading = true;
    error = null;
    notifyListeners();
    try {
      final apply = await fetch();
      if (request == _request) apply();
    } on PriceTrendException catch (e) {
      if (request == _request) error = e.message;
    } finally {
      if (request == _request) {
        loading = false;
        notifyListeners();
      }
    }
  }
}
