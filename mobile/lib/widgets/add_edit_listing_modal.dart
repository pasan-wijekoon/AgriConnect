import 'package:flutter/material.dart';

class AddEditListingModal extends StatefulWidget {
  const AddEditListingModal({super.key});

  @override
  State<AddEditListingModal> createState() => _AddEditListingModalState();
}

class _AddEditListingModalState extends State<AddEditListingModal> {
  final _formKey = GlobalKey<FormState>();
  final _priceController = TextEditingController();
  final _priceFocus = FocusNode();

  @override
  void dispose() {
    _priceFocus.dispose();
    _priceController.dispose();
    super.dispose();
  }

  String? _priceValidator(String? value) {
    if (value == null || value.isEmpty) return 'Price is required';
    final parsed = double.tryParse(value);
    if (parsed == null) return 'Enter a number';
    if (parsed <= 0) return 'Price must be positive';
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Add/Edit Listing')),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              TextFormField(
                focusNode: _priceFocus,
                key: const Key('priceField'),
                controller: _priceController,
                decoration: const InputDecoration(labelText: 'Price'),
                keyboardType: TextInputType.number,
                validator: _priceValidator,
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                key: const Key('submitButton'),
                onPressed: () {
                  if (_formKey.currentState?.validate() ?? false) {
                    // Normally submit the form
                  }
                },
                child: const Text('Submit'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
