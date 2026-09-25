import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/listing_fixtures.dart';
import '../../models/order.dart';
import '../../providers/dev_identity_provider.dart';
import '../../providers/order_provider.dart';
import '../../theme/app_colors.dart';

/// Place-order screen (FR8, plan §11). Short form, clear validation, and
/// loading/error/success states throughout (CLAUDE.md §22).
class PlaceOrderScreen extends StatefulWidget {
  const PlaceOrderScreen({super.key});

  @override
  State<PlaceOrderScreen> createState() => _PlaceOrderScreenState();
}

class _PlaceOrderScreenState extends State<PlaceOrderScreen> {
  final _formKey = GlobalKey<FormState>();
  final _quantityController = TextEditingController();

  ListingFixture _listing = listingFixtures.first;
  DeliveryPreference _deliveryPreference = DeliveryPreference.pickup;
  String? _submitError;

  @override
  void dispose() {
    _quantityController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final identity = context.read<DevIdentityProvider>();
    final orders = context.read<OrderProvider>();
    setState(() => _submitError = null);

    final order = await orders.placeOrder(
      devRoleToHeader(identity.role),
      identity.userId,
      listingId: _listing.id,
      quantity: double.parse(_quantityController.text),
      deliveryPreference: _deliveryPreference,
    );

    if (!mounted) return;

    if (order == null) {
      setState(() => _submitError = orders.error ?? 'Failed to place order.');
      return;
    }

    _quantityController.clear();
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Order placed successfully.')),
    );
  }

  @override
  Widget build(BuildContext context) {
    final submitting = context.watch<OrderProvider>().submitting;

    return Scaffold(
      appBar: AppBar(title: const Text('Place Order')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Produce Listing',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<ListingFixture>(
                initialValue: _listing,
                decoration: const InputDecoration(border: OutlineInputBorder()),
                items: listingFixtures
                    .map((l) => DropdownMenuItem(
                          value: l,
                          child: Text(
                              '${l.cropName} (${l.availableQuantity.toStringAsFixed(0)} available)'),
                        ))
                    .toList(),
                onChanged: (value) => setState(() => _listing = value!),
              ),
              const SizedBox(height: 20),
              const Text(
                'Quantity',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 8),
              TextFormField(
                controller: _quantityController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  border: OutlineInputBorder(),
                  suffixText: 'kg',
                ),
                validator: (value) {
                  final parsed = double.tryParse(value ?? '');
                  if (parsed == null) return 'Enter a valid quantity.';
                  if (parsed <= 0) return 'Quantity must be greater than 0.';
                  return null;
                },
              ),
              const SizedBox(height: 20),
              const Text(
                'Delivery Preference',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
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
                onSelectionChanged: (selection) =>
                    setState(() => _deliveryPreference = selection.first),
              ),
              const SizedBox(height: 24),
              if (_submitError != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppColors.errorBg,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    _submitError!,
                    style: const TextStyle(color: AppColors.error),
                  ),
                ),
                const SizedBox(height: 16),
              ],
              ElevatedButton(
                onPressed: submitting ? null : _submit,
                child: submitting
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white),
                      )
                    : const Text('Place Order'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
