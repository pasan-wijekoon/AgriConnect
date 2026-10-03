/// A crop or region as offered by the analytics filters.
class NamedItem {
  const NamedItem({required this.id, required this.name, this.hasPriceHistory = true});

  factory NamedItem.fromJson(Map<String, dynamic> json) => NamedItem(
        id: json['id'] as String,
        name: json['name'] as String,
        hasPriceHistory: json['hasPriceHistory'] as bool? ?? true,
      );

  final String id;
  final String name;

  /// False when the API has no price trend for this crop yet.
  final bool hasPriceHistory;
}

/// One week of prices (LKR per kg) for a crop.
class PriceTrendPoint {
  const PriceTrendPoint({
    required this.period,
    required this.avgPrice,
    required this.minPrice,
    required this.maxPrice,
    required this.sampleCount,
  });

  factory PriceTrendPoint.fromJson(Map<String, dynamic> json) => PriceTrendPoint(
        period: _calendarDay(json['period'] as String),
        avgPrice: (json['avgPrice'] as num).toDouble(),
        minPrice: (json['minPrice'] as num).toDouble(),
        maxPrice: (json['maxPrice'] as num).toDouble(),
        sampleCount: json['sampleCount'] as int,
      );

  /// Monday of the week.
  final DateTime period;
  final double avgPrice;
  final double minPrice;
  final double maxPrice;

  /// How many listings the week's prices come from.
  final int sampleCount;

  // UTC so adding 7-day steps never drifts an hour across a daylight-saving change.
  static DateTime _calendarDay(String isoDate) {
    final d = DateTime.parse(isoDate);
    return DateTime.utc(d.year, d.month, d.day);
  }
}

class PriceTrend {
  const PriceTrend({required this.cropId, required this.points});

  factory PriceTrend.fromJson(Map<String, dynamic> json) => PriceTrend(
        cropId: json['cropId'] as String,
        points: (json['points'] as List<dynamic>)
            .map((p) => PriceTrendPoint.fromJson(p as Map<String, dynamic>))
            .toList()
          ..sort((a, b) => a.period.compareTo(b.period)),
      );

  final String cropId;

  /// Oldest first. Weeks with no listings are missing, never zero.
  final List<PriceTrendPoint> points;

  PriceTrendPoint? get latest => points.isEmpty ? null : points.last;

  /// Percentage change of the latest week's average against the week [weeksBack] earlier,
  /// or null if that week has no data.
  double? changePercent({int weeksBack = 4}) {
    final now = latest;
    if (now == null) return null;
    final target = now.period.subtract(Duration(days: 7 * weeksBack));
    final then = points.where((p) => _dayKey(p.period) == _dayKey(target)).firstOrNull;
    if (then == null || then.avgPrice == 0) return null;
    return (now.avgPrice - then.avgPrice) / then.avgPrice * 100;
  }

  /// One slot per week from the first to the last point; missing weeks are null so a chart
  /// can show a gap instead of drawing a line across it.
  List<PriceTrendPoint?> get weeklySeries {
    if (points.isEmpty) return const [];
    final byWeek = {for (final p in points) _dayKey(p.period): p};
    final weeks = points.last.period.difference(points.first.period).inDays ~/ 7;
    return [
      for (var i = 0; i <= weeks; i++) byWeek[_dayKey(points.first.period.add(Duration(days: 7 * i)))],
    ];
  }

  static String _dayKey(DateTime d) => '${d.year}-${d.month}-${d.day}';
}
