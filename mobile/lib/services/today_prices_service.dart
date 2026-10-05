import '../models/today_price.dart';
import 'backend_client.dart';

export 'backend_client.dart' show ApiException;

/// Reads the Today's Prices page data (Component A) through the authenticated
/// backend client — the Flutter counterpart to `api.getTodayPrices` on the web.
class TodayPricesService {
  final BackendClient _api;

  TodayPricesService(this._api);

  /// [region] null/'All' means every crop in its own default region.
  Future<TodayPrices> fetch({String? region, String grade = 'A'}) {
    final query = {
      if (region != null && region != 'All') 'region': region,
      'grade': grade,
    };
    return _api.request(
      'GET',
      '/api/prices/today?${Uri(queryParameters: query).query}',
      decode: (json) => TodayPrices.fromJson(json as Map<String, dynamic>),
    );
  }
}
