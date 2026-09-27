import 'dart:math' as math;

import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../models/price_trend.dart';
import '../../providers/price_trend_provider.dart';
import '../../services/price_trend_service.dart';

/// Read-only weekly price trend for farmers, so they can judge whether to list now.
/// Deliberately minimal: the React dashboard is the full analytics surface.
class PriceTrendsScreen extends StatefulWidget {
  const PriceTrendsScreen({super.key, required this.service, this.clock});

  final PriceTrendService service;
  final DateTime Function()? clock;

  @override
  State<PriceTrendsScreen> createState() => _PriceTrendsScreenState();
}

class _PriceTrendsScreenState extends State<PriceTrendsScreen> {
  late final PriceTrendProvider _prices = PriceTrendProvider(widget.service, clock: widget.clock)..load();

  @override
  void dispose() {
    _prices.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Market prices')),
      body: ListenableBuilder(
        listenable: _prices,
        builder: (context, _) {
          final p = _prices;
          if (p.crops.isEmpty && p.loading) {
            return const Center(child: CircularProgressIndicator());
          }
          if (p.crops.isEmpty && p.error != null) {
            return _Message(icon: Icons.cloud_off, text: p.error!, onRetry: p.load);
          }
          if (p.crops.isEmpty) {
            return const _Message(icon: Icons.info_outline, text: 'No crops are available yet.');
          }

          return RefreshIndicator(
            onRefresh: p.load,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(
                  'Weekly average across all regions · last ${PriceTrendProvider.weeks} weeks',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final crop in p.crops)
                      ChoiceChip(
                        label: Text(crop.name),
                        selected: crop.id == p.selectedCrop?.id,
                        onSelected: (_) => p.selectCrop(crop),
                      ),
                  ],
                ),
                const SizedBox(height: 16),
                if (p.error != null) ...[
                  _Message(icon: Icons.error_outline, text: p.error!, onRetry: p.load, compact: true),
                  const SizedBox(height: 16),
                ],
                // Keep the previous result visible (dimmed) while a new crop loads.
                AnimatedOpacity(
                  opacity: p.loading ? 0.5 : 1,
                  duration: const Duration(milliseconds: 150),
                  child: _TrendContent(crop: p.selectedCrop, trend: p.trend, loading: p.loading),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _TrendContent extends StatelessWidget {
  const _TrendContent({required this.crop, required this.trend, required this.loading});

  final NamedItem? crop;
  final PriceTrend? trend;
  final bool loading;

  @override
  Widget build(BuildContext context) {
    final t = trend;
    if (t == null) {
      return loading ? const SizedBox(height: 200) : const SizedBox.shrink();
    }
    if (t.points.isEmpty) {
      return _Message(
        icon: Icons.bar_chart,
        text: 'No prices recorded for ${crop?.name ?? 'this crop'} in the last ${PriceTrendProvider.weeks} weeks.',
        compact: true,
      );
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Headline(trend: t),
        const SizedBox(height: 16),
        _PriceChart(trend: t),
        const SizedBox(height: 16),
        _WeeklyTable(trend: t),
      ],
    );
  }
}

class _Headline extends StatelessWidget {
  const _Headline({required this.trend});

  final PriceTrend trend;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final latest = trend.latest!;
    final change = trend.changePercent();
    final muted = theme.colorScheme.onSurfaceVariant;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Current average', style: theme.textTheme.labelLarge?.copyWith(color: muted)),
            const SizedBox(height: 4),
            Text.rich(
              TextSpan(children: [
                TextSpan(text: formatLkr(latest.avgPrice), style: theme.textTheme.headlineMedium),
                TextSpan(text: ' /kg', style: theme.textTheme.titleMedium?.copyWith(color: muted)),
              ]),
            ),
            const SizedBox(height: 4),
            Text(
              'Week of ${formatDay(latest.period)} · from ${latest.sampleCount} listings',
              style: theme.textTheme.bodySmall?.copyWith(color: muted),
            ),
            if (change != null) ...[
              const SizedBox(height: 8),
              // An icon, not ▲/▼ text: those glyphs are missing from some device fonts.
              Row(children: [
                Icon(change >= 0 ? Icons.arrow_upward : Icons.arrow_downward, size: 16),
                const SizedBox(width: 4),
                Text(
                  '${change >= 0 ? 'Up' : 'Down'} ${change.abs().toStringAsFixed(1)}% vs 4 weeks ago',
                  style: theme.textTheme.bodyMedium,
                ),
              ]),
            ],
          ],
        ),
      ),
    );
  }
}

class _PriceChart extends StatelessWidget {
  const _PriceChart({required this.trend});

  final PriceTrend trend;

  // Reference data-viz palette: series slot 1 and chart chrome, per brightness.
  static const _lineLight = Color(0xFF2A78D6);
  static const _lineDark = Color(0xFF3987E5);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final dark = theme.brightness == Brightness.dark;
    final line = dark ? _lineDark : _lineLight;
    final grid = dark ? const Color(0xFF2C2C2A) : const Color(0xFFE1E0D9);
    final baseline = dark ? const Color(0xFF383835) : const Color(0xFFC3C2B7);
    const muted = Color(0xFF898781);

    final series = trend.weeklySeries;
    FlSpot spot(int i, double? v) => v == null ? FlSpot.nullSpot : FlSpot(i.toDouble(), v);

    final low = trend.points.map((p) => p.minPrice).reduce(math.min);
    final high = trend.points.map((p) => p.maxPrice).reduce(math.max);
    final step = _niceStep((high - low) / 4);
    final minY = (low / step).floor() * step;
    final maxY = (high / step).ceil() * step;
    final lastX = (series.length - 1).toDouble();

    LineChartBarData bar(List<FlSpot> spots, {Color? color, double width = 0}) => LineChartBarData(
          spots: spots,
          color: color ?? Colors.transparent,
          barWidth: width,
          isCurved: false,
          dotData: const FlDotData(show: false),
        );

    final lowBar = bar([for (var i = 0; i < series.length; i++) spot(i, series[i]?.minPrice)]);
    final highBar = bar([for (var i = 0; i < series.length; i++) spot(i, series[i]?.maxPrice)]);
    final avgBar = bar([for (var i = 0; i < series.length; i++) spot(i, series[i]?.avgPrice)], color: line, width: 2)
        .copyWith(
      dotData: FlDotData(
        show: true,
        checkToShowDot: (s, _) => s.x == lastX,
        getDotPainter: (_, _, _, _) => FlDotCirclePainter(
          radius: 4,
          color: line,
          strokeWidth: 2,
          strokeColor: theme.colorScheme.surface,
        ),
      ),
    );

    Widget axisLabel(String text, TitleMeta meta) => SideTitleWidget(
          meta: meta,
          child: Text(text, style: theme.textTheme.labelSmall?.copyWith(color: muted)),
        );

    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 16, 16, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.only(left: 4),
              child: Text('Price per kg (LKR)', style: theme.textTheme.titleSmall),
            ),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.only(left: 4),
              child: Wrap(spacing: 16, children: [
                _LegendKey(color: line, label: 'Average', isLine: true),
                _LegendKey(color: line.withValues(alpha: 0.18), label: 'Lowest–highest'),
              ]),
            ),
            const SizedBox(height: 12),
            Semantics(
              label: 'Line chart of the weekly average price. The weekly prices table below lists every value.',
              child: SizedBox(
                height: 220,
                child: LineChart(
                  LineChartData(
                    minX: 0,
                    maxX: lastX,
                    minY: minY,
                    maxY: maxY,
                    lineBarsData: [lowBar, highBar, avgBar],
                    betweenBarsData: [BetweenBarsData(fromIndex: 0, toIndex: 1, color: line.withValues(alpha: 0.18))],
                    gridData: FlGridData(
                      drawVerticalLine: false,
                      horizontalInterval: step,
                      getDrawingHorizontalLine: (_) => FlLine(color: grid, strokeWidth: 1),
                    ),
                    borderData: FlBorderData(show: true, border: Border(bottom: BorderSide(color: baseline))),
                    titlesData: FlTitlesData(
                      topTitles: const AxisTitles(),
                      rightTitles: const AxisTitles(),
                      leftTitles: AxisTitles(
                        sideTitles: SideTitles(
                          showTitles: true,
                          reservedSize: 40,
                          interval: step,
                          getTitlesWidget: (v, meta) => axisLabel(v.toStringAsFixed(0), meta),
                        ),
                      ),
                      bottomTitles: AxisTitles(
                        sideTitles: SideTitles(
                          showTitles: true,
                          reservedSize: 28,
                          interval: 1,
                          getTitlesWidget: (v, meta) {
                            final i = v.round();
                            // A label every 4 weeks, counted back from the latest week.
                            if (v != i || (series.length - 1 - i) % 4 != 0) return const SizedBox.shrink();
                            return axisLabel(formatDay(trend.points.first.period.add(Duration(days: 7 * i))), meta);
                          },
                        ),
                      ),
                    ),
                    lineTouchData: LineTouchData(
                      getTouchedSpotIndicator: (bar, indexes) => [
                        for (final _ in indexes)
                          identical(bar, avgBar)
                              ? TouchedSpotIndicatorData(
                                  FlLine(color: baseline, strokeWidth: 1),
                                  FlDotData(
                                    getDotPainter: (_, _, _, _) =>
                                        FlDotCirclePainter(radius: 4, color: line, strokeWidth: 0),
                                  ),
                                )
                              : null,
                      ],
                      touchTooltipData: LineTouchTooltipData(
                        getTooltipColor: (_) => theme.colorScheme.inverseSurface,
                        fitInsideHorizontally: true,
                        fitInsideVertically: true,
                        getTooltipItems: (spots) => [
                          for (final s in spots)
                            s.barIndex == 2 ? _tooltip(series[s.x.round()]!, theme) : null,
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  static LineTooltipItem _tooltip(PriceTrendPoint p, ThemeData theme) {
    final ink = theme.colorScheme.onInverseSurface;
    return LineTooltipItem(
      formatLkr(p.avgPrice),
      theme.textTheme.titleSmall!.copyWith(color: ink, fontWeight: FontWeight.bold),
      textAlign: TextAlign.left,
      children: [
        TextSpan(
          text: '\nWeek of ${formatDay(p.period)}\n'
              '${p.minPrice.toStringAsFixed(0)}–${p.maxPrice.toStringAsFixed(0)} · ${p.sampleCount} listings',
          style: theme.textTheme.bodySmall!.copyWith(color: ink.withValues(alpha: 0.8)),
        ),
      ],
    );
  }

  static double _niceStep(double raw) {
    if (raw <= 0) return 10;
    final magnitude = math.pow(10, (math.log(raw) / math.ln10).floor()).toDouble();
    for (final m in const [1, 2, 5, 10]) {
      if (raw <= m * magnitude) return m * magnitude;
    }
    return 10 * magnitude;
  }
}

class _LegendKey extends StatelessWidget {
  const _LegendKey({required this.color, required this.label, this.isLine = false});

  final Color color;
  final String label;
  final bool isLine;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 14,
          height: isLine ? 2 : 10,
          decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(2)),
        ),
        const SizedBox(width: 6),
        Text(label, style: Theme.of(context).textTheme.labelMedium),
      ],
    );
  }
}

/// The chart's values as text, newest first, so nothing depends on reading the chart.
class _WeeklyTable extends StatelessWidget {
  const _WeeklyTable({required this.trend});

  final PriceTrend trend;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final muted = theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant);
    const figures = TextStyle(fontFeatures: [FontFeature.tabularFigures()]);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        title: const Text('Weekly prices'),
        children: [
          for (final p in trend.points.reversed)
            ListTile(
              dense: true,
              title: Text('Week of ${formatDay(p.period)}'),
              subtitle: Text(
                'Range ${p.minPrice.toStringAsFixed(0)}–${p.maxPrice.toStringAsFixed(0)} · ${p.sampleCount} listings',
                style: muted,
              ),
              trailing: Text(formatLkr(p.avgPrice), style: theme.textTheme.bodyLarge?.merge(figures)),
            ),
        ],
      ),
    );
  }
}

class _Message extends StatelessWidget {
  const _Message({required this.icon, required this.text, this.onRetry, this.compact = false});

  final IconData icon;
  final String text;
  final Future<void> Function()? onRetry;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final content = Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: compact ? 28 : 40, color: theme.colorScheme.onSurfaceVariant),
        const SizedBox(height: 8),
        Text(text, textAlign: TextAlign.center, style: theme.textTheme.bodyMedium),
        if (onRetry != null) ...[
          const SizedBox(height: 8),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ],
    );
    return compact
        ? Card(child: Padding(padding: const EdgeInsets.all(16), child: content))
        : Center(child: Padding(padding: const EdgeInsets.all(24), child: content));
  }
}

const _months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

String formatDay(DateTime d) => '${d.day} ${_months[d.month - 1]}';

String formatLkr(double value) {
  final fixed = value.toStringAsFixed(2);
  final whole = fixed.split('.').first;
  final grouped = whole.replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => ',');
  return 'LKR $grouped.${fixed.split('.').last}';
}
