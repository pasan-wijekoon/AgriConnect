/// One crop's AI fair-price estimate on the Today's Prices page
/// (`GET /api/prices/today`, Component A's Fair-Price Estimation Agent).
class TodayPriceItem {
  final String cropId;
  final String name;
  final String category;
  final String unit;
  final String region;
  final String grade;
  final double priceMin;
  final double priceMax;
  final double averagePrice;
  final double confidence; // 0..1
  final double change24h; // percent
  final String trend; // rising | falling | stable
  final String imageUrl;
  final String reasoning;

  const TodayPriceItem({
    required this.cropId,
    required this.name,
    required this.category,
    required this.unit,
    required this.region,
    required this.grade,
    required this.priceMin,
    required this.priceMax,
    required this.averagePrice,
    required this.confidence,
    required this.change24h,
    required this.trend,
    required this.imageUrl,
    required this.reasoning,
  });

  factory TodayPriceItem.fromJson(Map<String, dynamic> json) => TodayPriceItem(
        cropId: json['cropId'] as String,
        name: json['name'] as String,
        category: json['category'] as String,
        unit: (json['unit'] as String?) ?? 'kg',
        region: (json['region'] as String?) ?? '',
        grade: (json['grade'] as String?) ?? 'A',
        priceMin: (json['suggestedPriceMin'] as num).toDouble(),
        priceMax: (json['suggestedPriceMax'] as num).toDouble(),
        averagePrice: (json['averagePrice'] as num).toDouble(),
        confidence: (json['confidence'] as num).toDouble(),
        change24h: (json['change24h'] as num?)?.toDouble() ?? 0,
        trend: (json['trend'] as String?) ?? 'stable',
        imageUrl: (json['imageUrl'] as String?) ?? '',
        reasoning: (json['reasoning'] as String?) ?? '',
      );
}

class TodayPrices {
  final String marketStatus;
  final List<TodayPriceItem> items;

  const TodayPrices({required this.marketStatus, required this.items});

  factory TodayPrices.fromJson(Map<String, dynamic> json) => TodayPrices(
        marketStatus: (json['marketStatus'] as String?) ?? '',
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => TodayPriceItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
