import 'package:flutter/material.dart';

import '../../models/today_price.dart';
import '../../services/today_prices_service.dart';
import '../../theme/app_colors.dart';

const _categories = ['All', 'Vegetables', 'Fruits', 'Grains', 'Spices', 'Beverages'];

const _regions = [
  'All', 'Colombo', 'Gampaha', 'Kalutara', 'Kandy', 'Matale', 'Nuwara Eliya', 'Galle',
  'Matara', 'Hambantota', 'Jaffna', 'Kilinochchi', 'Mannar', 'Mullaitivu', 'Vavuniya',
  'Batticaloa', 'Ampara', 'Trincomalee', 'Kurunegala', 'Puttalam', 'Anuradhapura',
  'Polonnaruwa', 'Badulla', 'Monaragala', 'Ratnapura', 'Kegalle', 'Dambulla',
];

/// Today's Produce Prices (Component A): the Fair-Price Estimation Agent's price
/// range per crop, filterable by category, region, grade and name. Read-only.
class TodayPricesScreen extends StatefulWidget {
  const TodayPricesScreen({super.key, required this.service});

  final TodayPricesService service;

  @override
  State<TodayPricesScreen> createState() => _TodayPricesScreenState();
}

class _TodayPricesScreenState extends State<TodayPricesScreen> {
  List<TodayPriceItem> _items = [];
  bool _loading = true;
  String? _error;

  String _category = 'All';
  String _region = 'All';
  String _grade = 'A';
  String _search = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await widget.service.fetch(region: _region, grade: _grade);
      if (!mounted) return;
      setState(() {
        _items = result.items;
        _loading = false;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  List<TodayPriceItem> get _visible {
    final q = _search.trim().toLowerCase();
    return _items.where((i) {
      final matchesCategory = _category == 'All' || i.category.toLowerCase() == _category.toLowerCase();
      final matchesSearch = q.isEmpty || i.name.toLowerCase().contains(q) || i.category.toLowerCase().contains(q);
      return matchesCategory && matchesSearch;
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final visible = _visible;
    return Scaffold(
      appBar: AppBar(title: const Text("Today's prices")),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            const Text(
              'AI fair-price ranges for the most traded produce, per kg.',
              style: TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 12),
            _Summary(items: _items),
            const SizedBox(height: 12),
            SizedBox(
              height: 40,
              child: ListView(
                scrollDirection: Axis.horizontal,
                children: [
                  for (final c in _categories)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(c),
                        selected: _category == c,
                        onSelected: (_) => setState(() => _category = c),
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _region,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Region', isDense: true),
                    items: [
                      for (final r in _regions)
                        DropdownMenuItem(value: r, child: Text(r == 'All' ? 'All regions' : r)),
                    ],
                    onChanged: (v) {
                      if (v == null || v == _region) return;
                      setState(() => _region = v);
                      _load();
                    },
                  ),
                ),
                const SizedBox(width: 12),
                SegmentedButton<String>(
                  showSelectedIcon: false,
                  segments: const [
                    ButtonSegment(value: 'A', label: Text('A')),
                    ButtonSegment(value: 'B', label: Text('B')),
                    ButtonSegment(value: 'C', label: Text('C')),
                  ],
                  selected: {_grade},
                  onSelectionChanged: (s) {
                    setState(() => _grade = s.first);
                    _load();
                  },
                ),
              ],
            ),
            const SizedBox(height: 8),
            TextField(
              decoration: const InputDecoration(
                hintText: 'Search crop',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
              onChanged: (v) => setState(() => _search = v),
            ),
            const SizedBox(height: 12),
            if (_loading && _items.isEmpty)
              const Padding(padding: EdgeInsets.all(40), child: Center(child: CircularProgressIndicator()))
            else if (_error != null && _items.isEmpty)
              _Message(icon: Icons.cloud_off, text: _error!, onRetry: _load)
            else if (visible.isEmpty)
              const _Message(icon: Icons.search_off, text: 'No crops match these filters.')
            else ...[
              if (_error != null) _Message(icon: Icons.error_outline, text: _error!, onRetry: _load),
              Opacity(
                opacity: _loading ? 0.5 : 1,
                child: Column(
                  children: [for (final item in visible) _PriceCard(item: item, onTap: () => _showDetail(item))],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  void _showDetail(TodayPriceItem item) {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (context) => Padding(
        padding: const EdgeInsets.fromLTRB(20, 0, 20, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(item.name, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
            const SizedBox(height: 4),
            Text('${item.category} · ${item.region} · Grade ${item.grade}',
                style: const TextStyle(color: AppColors.textSecondary)),
            const SizedBox(height: 12),
            Text('Rs. ${_fmt(item.priceMin)} – ${_fmt(item.priceMax)} per ${item.unit}',
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700, color: AppColors.primary)),
            const SizedBox(height: 4),
            Text('Average Rs. ${_fmt(item.averagePrice)} · ${(item.confidence * 100).round()}% AI confidence'),
            if (item.reasoning.isNotEmpty) ...[
              const SizedBox(height: 12),
              const Text('How the AI got this', style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 4),
              Text(item.reasoning, style: const TextStyle(color: AppColors.textSecondary, height: 1.4)),
            ],
            const SizedBox(height: 12),
            const Text(
              'An estimate to guide pricing, not a fixed price. Final prices are set by farmers and approved by officers.',
              style: TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
          ],
        ),
      ),
    );
  }
}

String _fmt(double v) => v.round().toString();

class _Summary extends StatelessWidget {
  const _Summary({required this.items});

  final List<TodayPriceItem> items;

  @override
  Widget build(BuildContext context) {
    final rising = items.where((i) => i.trend == 'rising').length;
    final falling = items.where((i) => i.trend == 'falling').length;
    final conf = items.isEmpty
        ? 0
        : (items.map((i) => i.confidence).reduce((a, b) => a + b) / items.length * 100).round();

    Widget tile(String label, String value) => Expanded(
          child: Container(
            padding: const EdgeInsets.symmetric(vertical: 10),
            decoration: BoxDecoration(
              color: AppColors.card,
              border: Border.all(color: AppColors.border),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(children: [
              Text(value, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
              Text(label, style: const TextStyle(fontSize: 11, color: AppColors.textSecondary)),
            ]),
          ),
        );

    return Row(children: [
      tile('Tracked', '${items.length}'),
      const SizedBox(width: 8),
      tile('Rising', '$rising'),
      const SizedBox(width: 8),
      tile('Falling', '$falling'),
      const SizedBox(width: 8),
      tile('AI conf.', '$conf%'),
    ]);
  }
}

class _PriceCard extends StatelessWidget {
  const _PriceCard({required this.item, required this.onTap});

  final TodayPriceItem item;
  final VoidCallback onTap;

  static const _placeholder = ColoredBox(color: AppColors.successBg, child: Icon(Icons.eco, color: AppColors.primary));

  @override
  Widget build(BuildContext context) {
    final rising = item.trend == 'rising';
    final falling = item.trend == 'falling';
    final color = rising ? AppColors.success : (falling ? AppColors.error : AppColors.neutral);
    final arrow = rising ? '▲' : (falling ? '▼' : '●');
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(10),
                child: SizedBox(
                  width: 64,
                  height: 64,
                  child: item.imageUrl.isEmpty
                      ? _placeholder
                      : Image.network(
                          item.imageUrl,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => _placeholder,
                        ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(item.name, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15)),
                    Text('${item.category} · ${item.region}',
                        style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                    const SizedBox(height: 4),
                    Text('Rs. ${_fmt(item.priceMin)} – ${_fmt(item.priceMax)} / ${item.unit}',
                        style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.primary)),
                    Text('Avg Rs. ${_fmt(item.averagePrice)} · ${(item.confidence * 100).round()}% conf.',
                        style: const TextStyle(fontSize: 11, color: AppColors.textMuted)),
                  ],
                ),
              ),
              Text('$arrow ${item.change24h.abs().toStringAsFixed(1)}%',
                  style: TextStyle(fontWeight: FontWeight.w700, color: color)),
            ],
          ),
        ),
      ),
    );
  }
}

class _Message extends StatelessWidget {
  const _Message({required this.icon, required this.text, this.onRetry});

  final IconData icon;
  final String text;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          Icon(icon, size: 36, color: AppColors.textMuted),
          const SizedBox(height: 8),
          Text(text, textAlign: TextAlign.center),
          if (onRetry != null) ...[
            const SizedBox(height: 8),
            OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
          ],
        ],
      ),
    );
  }
}
