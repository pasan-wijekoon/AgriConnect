import '../models/price_trend.dart';
import 'price_trend_service.dart';

/// A minimal dummy implementation of [PriceTrendService] used for testing.
/// It implements the same public interface but returns empty or placeholder data.
class DummyPriceTrendService implements PriceTrendService {
  const DummyPriceTrendService();

  @override
  Future<List<NamedItem>> fetchCrops() async {
    return const [];
  }

  @override
  Future<PriceTrend> fetchTrend({
    required String cropId,
    required DateTime from,
    required DateTime to,
  }) async {
    return PriceTrend(cropId: cropId, points: const []);
  }

  @override
  final String? Function()? getToken = null;
}
