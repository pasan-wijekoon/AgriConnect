import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';

import '../../models/listing.dart';
import '../../models/order.dart';
import '../../providers/order_provider.dart';
import '../../theme/app_colors.dart';
import '../../widgets/state_views.dart';
import 'order_detail_screen.dart';

/// Place an order (FR8) against a real, published marketplace listing. The
/// listing comes from the marketplace (its detail screen's "Order now"), so
/// there is no dummy dropdown any more. Stock is reserved atomically by the
/// backend (FR9); a 409 "insufficient stock" comes back as a readable message.
class PlaceOrderScreen extends StatefulWidget {
  final Listing listing;

  const PlaceOrderScreen({super.key, required this.listing});

  @override
  State<PlaceOrderScreen> createState() => _PlaceOrderScreenState();
}

class _PlaceOrderScreenState extends State<PlaceOrderScreen> {
  final _formKey = GlobalKey<FormState>();
  final _quantityController = TextEditingController();

  DeliveryPreference _deliveryPreference = DeliveryPreference.pickup;
  String? _submitError;

  // Optional, approximate (2 decimal places, about 1 km) location so the system can
  // suggest the nearest collection centre when the order is approved.
  double? _buyerLat;
  double? _buyerLng;
  bool _locating = false;
  String? _locationNote;

  Listing get listing => widget.listing;

  @override
  void dispose() {
    _quantityController.dispose();
    super.dispose();
  }

  double? get _quantity => double.tryParse(_quantityController.text.trim());

  double _round2(double value) => (value * 100).roundToDouble() / 100;

  Future<void> _shareLocation() async {
    setState(() {
      _locating = true;
      _locationNote = null;
    });
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        throw Exception('Location services are turned off.');
      }
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        throw Exception('Location permission was denied.');
      }
      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.low,
          timeLimit: Duration(seconds: 10),
        ),
      );
      if (!mounted) return;
      setState(() {
        _buyerLat = _round2(position.latitude);
        _buyerLng = _round2(position.longitude);
        _locating = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _locating = false;
        _locationNote = 'We could not get your location. You can still place the order.';
      });
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final orders = context.read<OrderProvider>();
    setState(() => _submitError = null);

    final order = await orders.placeOrder(
      listingId: listing.id,
      quantity: _quantity!,
      deliveryPreference: _deliveryPreference,
      buyerLat: _buyerLat,
      buyerLng: _buyerLng,
    );

    if (!mounted) return;

    if (order == null) {
      setState(() => _submitError = orders.error ?? 'Failed to place order.');
      return;
    }

    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => OrderDetailScreen(orderId: order.id, justPlaced: true),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final submitting = context.watch<OrderProvider>().submitting;

    return Scaffold(
      appBar: AppBar(title: const Text('Place order')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: AppColors.successBg,
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: const Icon(Icons.eco, color: AppColors.primary),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(listing.cropName,
                                style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18)),
                            const SizedBox(height: 2),
                            Text(
                              '${listing.regionName} · Grade ${listing.claimedGrade}',
                              style: const TextStyle(color: AppColors.textSecondary),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              '${listing.availableQuantity.toStringAsFixed(0)} ${listing.unit} available now',
                              style: const TextStyle(fontSize: 13),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 20),
              Text('Quantity (${listing.unit})',
                  style: const TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              TextFormField(
                key: const ValueKey('order_quantity_field'),
                controller: _quantityController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(
                  border: const OutlineInputBorder(),
                  suffixText: listing.unit,
                  helperText: 'Max ${listing.availableQuantity.toStringAsFixed(0)} ${listing.unit}',
                ),
                validator: (value) {
                  final parsed = double.tryParse(value?.trim() ?? '');
                  if (parsed == null) return 'Enter a valid quantity.';
                  if (parsed <= 0) return 'Quantity must be greater than 0.';
                  if (parsed > listing.availableQuantity) {
                    return 'Only ${listing.availableQuantity.toStringAsFixed(0)} ${listing.unit} available right now.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                children: [
                  for (final fraction in const [0.25, 0.5, 1.0])
                    ActionChip(
                      label: Text(fraction == 1.0 ? 'All' : '${(fraction * 100).round()}%'),
                      onPressed: () => setState(() {
                        final value = listing.availableQuantity * fraction;
                        _quantityController.text =
                            value % 1 == 0 ? value.toStringAsFixed(0) : value.toStringAsFixed(1);
                      }),
                    ),
                ],
              ),
              const SizedBox(height: 20),
              const Text('Collection', style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              SegmentedButton<DeliveryPreference>(
                segments: const [
                  ButtonSegment(
                    value: DeliveryPreference.pickup,
                    label: Text('Pickup'),
                    icon: Icon(Icons.local_shipping_outlined),
                  ),
                  ButtonSegment(
                    value: DeliveryPreference.delivery,
                    label: Text('Delivery'),
                    icon: Icon(Icons.home_outlined),
                  ),
                ],
                selected: {_deliveryPreference},
                onSelectionChanged: (s) => setState(() => _deliveryPreference = s.first),
              ),
              const SizedBox(height: 16),
              if (_buyerLat != null)
                Row(
                  children: [
                    const Icon(Icons.place_outlined, size: 18, color: AppColors.primary),
                    const SizedBox(width: 6),
                    const Expanded(
                      child: Text(
                        'Approximate location shared. It helps suggest the nearest collection centre.',
                        style: TextStyle(fontSize: 13),
                      ),
                    ),
                    TextButton(
                      onPressed: () => setState(() {
                        _buyerLat = null;
                        _buyerLng = null;
                      }),
                      child: const Text('Remove'),
                    ),
                  ],
                )
              else
                Align(
                  alignment: Alignment.centerLeft,
                  child: OutlinedButton.icon(
                    onPressed: _locating ? null : _shareLocation,
                    icon: const Icon(Icons.my_location, size: 18),
                    label: Text(_locating ? 'Locating…' : 'Use my approximate location (optional)'),
                  ),
                ),
              if (_locationNote != null) ...[
                const SizedBox(height: 6),
                Text(_locationNote!, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
              ],
              const SizedBox(height: 16),
              InfoBanner.info(
                message: 'A collection-centre officer will review your order and confirm a pickup slot at the '
                    'centre serving ${listing.regionName}. Your quantity is reserved as soon as you order.',
              ),
              if (_submitError != null) ...[
                const SizedBox(height: 16),
                InfoBanner(message: _submitError!),
              ],
              const SizedBox(height: 20),
              ElevatedButton(
                key: const ValueKey('place_order_button'),
                onPressed: submitting ? null : _submit,
                child: submitting
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                      )
                    : const Text('Place order'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
