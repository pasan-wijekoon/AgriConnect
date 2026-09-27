import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import '../models/listing.dart';
import '../services/api_service.dart';
import 'listing_detail_screen.dart';

class CreateListingScreen extends StatefulWidget {
  final ApiService apiService;
  final VoidCallback? onListingCreated;

  const CreateListingScreen({super.key, required this.apiService, this.onListingCreated});

  @override
  State<CreateListingScreen> createState() => _CreateListingScreenState();
}

class _CreateListingScreenState extends State<CreateListingScreen> {
  final _formKey = GlobalKey<FormState>();
  final TextEditingController _quantityController = TextEditingController();
  final TextEditingController _minPriceController = TextEditingController();
  final TextEditingController _photoUrlController = TextEditingController();
  final ImagePicker _picker = ImagePicker();

  List<Crop> _crops = [];
  List<Region> _regions = [];
  String? _selectedCropId;
  String? _selectedRegionId;
  String _selectedUnit = 'kg';
  String _selectedGrade = 'A';

  DateTime _pickupStart = DateTime.now().add(const Duration(days: 1));
  DateTime _pickupEnd = DateTime.now().add(const Duration(days: 3));

  final List<String> _photoUrls = [
    'https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=800',
  ];

  bool _isLoadingReference = true;
  bool _isSubmitting = false;
  bool _isUploadingPhoto = false;
  Map<String, dynamic>? _priceEstimate;
  bool _isLoadingPriceEstimate = false;

  @override
  void initState() {
    super.initState();
    _loadReferenceData();
  }

  Future<void> _loadReferenceData() async {
    final crops = await widget.apiService.getCrops();
    final regions = await widget.apiService.getRegions();
    if (mounted) {
      setState(() {
        _crops = crops;
        _regions = regions;
        if (_crops.isNotEmpty) _selectedCropId = _crops.first.id;
        if (_regions.isNotEmpty) _selectedRegionId = _regions.first.id;
        _isLoadingReference = false;
      });
      _fetchPriceEstimate();
    }
  }

  Future<void> _fetchPriceEstimate() async {
    if (_selectedCropId == null || _selectedRegionId == null) return;
    final crop = _crops.firstWhere((c) => c.id == _selectedCropId, orElse: () => _crops.first);
    final region = _regions.firstWhere((r) => r.id == _selectedRegionId, orElse: () => _regions.first);
    final qty = double.tryParse(_quantityController.text.trim()) ?? 100.0;

    setState(() => _isLoadingPriceEstimate = true);
    final est = await widget.apiService.getQuickPriceEstimate(
      cropId: _selectedCropId!,
      regionId: _selectedRegionId!,
      cropName: crop.name,
      regionName: region.name,
      grade: _selectedGrade,
      quantity: qty,
    );
    if (mounted) {
      setState(() {
        _priceEstimate = est;
        _isLoadingPriceEstimate = false;
      });
    }
  }

  Future<void> _pickPickupWindow(BuildContext context, bool isStart) async {
    final initialDate = isStart ? _pickupStart : _pickupEnd;
    final pickedDate = await showDatePicker(
      context: context,
      initialDate: initialDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 60)),
    );

    if (pickedDate != null && mounted) {
      setState(() {
        if (isStart) {
          _pickupStart = pickedDate;
          if (_pickupEnd.isBefore(_pickupStart)) {
            _pickupEnd = _pickupStart.add(const Duration(days: 2));
          }
        } else {
          _pickupEnd = pickedDate;
        }
      });
    }
  }

  void _addPhotoUrl() {
    final url = _photoUrlController.text.trim();
    if (url.isNotEmpty && !_photoUrls.contains(url)) {
      setState(() {
        _photoUrls.add(url);
        _photoUrlController.clear();
      });
    }
  }

  Future<void> _pickFromGallery() async {
    final List<XFile> images = await _picker.pickMultiImage(
      imageQuality: 85,
      maxWidth: 1200,
    );
    if (images.isEmpty) return;
    setState(() => _isUploadingPhoto = true);
    try {
      for (final image in images) {
        final url = await widget.apiService.uploadPhoto(image.path);
        if (mounted) {
          setState(() => _photoUrls.add(url));
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Photo upload failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isUploadingPhoto = false);
    }
  }

  Future<void> _takePhoto() async {
    final XFile? image = await _picker.pickImage(
      source: ImageSource.camera,
      imageQuality: 85,
      maxWidth: 1200,
    );
    if (image == null) return;
    setState(() => _isUploadingPhoto = true);
    try {
      final url = await widget.apiService.uploadPhoto(image.path);
      if (mounted) {
        setState(() => _photoUrls.add(url));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Photo capture failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isUploadingPhoto = false);
    }
  }

  void _showPhotoOptions() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 12),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 40,
                height: 4,
                margin: const EdgeInsets.only(bottom: 12),
                decoration: BoxDecoration(
                  color: Colors.grey.shade300,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                child: Text(
                  'Add Produce Photo',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
              ),
              const SizedBox(height: 8),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: Colors.green.shade50,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(Icons.camera_alt, color: Colors.green.shade700),
                ),
                title: const Text('Take Photo', style: TextStyle(fontWeight: FontWeight.w600)),
                subtitle: const Text('Use your camera to capture produce'),
                onTap: () {
                  Navigator.pop(ctx);
                  _takePhoto();
                },
              ),
              ListTile(
                leading: Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: Colors.blue.shade50,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(Icons.photo_library, color: Colors.blue.shade700),
                ),
                title: const Text('Choose from Gallery', style: TextStyle(fontWeight: FontWeight.w600)),
                subtitle: const Text('Select photos from your device'),
                onTap: () {
                  Navigator.pop(ctx);
                  _pickFromGallery();
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _submitListing() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedCropId == null || _selectedRegionId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select crop and region')),
      );
      return;
    }

    if (_pickupEnd.isBefore(_pickupStart)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Pickup End date must be after Start date')),
      );
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      final quantity = double.parse(_quantityController.text.trim());
      final minPrice = _minPriceController.text.trim().isNotEmpty
          ? double.tryParse(_minPriceController.text.trim())
          : null;

      final created = await widget.apiService.createListing(
        cropId: _selectedCropId!,
        regionId: _selectedRegionId!,
        quantity: quantity,
        unit: _selectedUnit,
        claimedGrade: _selectedGrade,
        pickupWindowStart: _pickupStart,
        pickupWindowEnd: _pickupEnd,
        minPrice: minPrice,
        photoUrls: _photoUrls,
      );

      if (mounted) {
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Listing created! AI Price Discovery generated.'),
            backgroundColor: Colors.green,
          ),
        );

        widget.onListingCreated?.call();

        // Navigate to detail screen to see the AI Price Discovery
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (context) => ListingDetailScreen(
              listingId: created.id,
              apiService: widget.apiService,
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to submit listing: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('yyyy-MM-dd');

    if (_isLoadingReference) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('New Produce Listing (FR3)'),
        elevation: 1,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Info Banner
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.green.shade200),
                ),
                child: Row(
                  children: [
                    Icon(Icons.auto_awesome, color: Colors.green.shade700),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'AgriConnect AI Fair-Price Advisor will automatically generate a suggested price based on historical market trends.',
                        style: TextStyle(color: Colors.green.shade900, fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // Crop Dropdown
              const Text('Select Crop', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 6),
              DropdownButtonFormField<String>(
                value: _selectedCropId,
                decoration: InputDecoration(
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                  contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                ),
                items: _crops.map((c) {
                  return DropdownMenuItem(
                    value: c.id,
                    child: Text('${c.name} (${c.category})'),
                  );
                }).toList(),
                onChanged: (val) {
                  setState(() => _selectedCropId = val);
                  _fetchPriceEstimate();
                },
              ),
              const SizedBox(height: 16),

              // Region Dropdown
              const Text('Farming Region', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 6),
              DropdownButtonFormField<String>(
                value: _selectedRegionId,
                decoration: InputDecoration(
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                  contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                ),
                items: _regions.map((r) {
                  return DropdownMenuItem(
                    value: r.id,
                    child: Text(r.name),
                  );
                }).toList(),
                onChanged: (val) {
                  setState(() => _selectedRegionId = val);
                  _fetchPriceEstimate();
                },
              ),
              const SizedBox(height: 16),

              // Quantity & Unit Row
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 3,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Quantity', style: TextStyle(fontWeight: FontWeight.bold)),
                        const SizedBox(height: 6),
                        TextFormField(
                          controller: _quantityController,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          decoration: InputDecoration(
                            hintText: 'e.g. 500',
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                          ),
                          onChanged: (_) => _fetchPriceEstimate(),
                          validator: (val) {
                            if (val == null || val.trim().isEmpty) return 'Enter quantity';
                            final num = double.tryParse(val.trim());
                            if (num == null || num <= 0) return 'Invalid amount';
                            return null;
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 2,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Unit', style: TextStyle(fontWeight: FontWeight.bold)),
                        const SizedBox(height: 6),
                        DropdownButtonFormField<String>(
                          value: _selectedUnit,
                          decoration: InputDecoration(
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                          ),
                          items: const [
                            DropdownMenuItem(value: 'kg', child: Text('kg')),
                            DropdownMenuItem(value: 'bag', child: Text('bag')),
                            DropdownMenuItem(value: 'bunch', child: Text('bunch')),
                          ],
                          onChanged: (val) {
                            if (val != null) setState(() => _selectedUnit = val);
                          },
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // AI Live Fair Price Discovery Card
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: Colors.green.shade50.withOpacity(0.7),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.green.shade200),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Icon(Icons.auto_awesome, color: Colors.green.shade700, size: 16),
                            const SizedBox(width: 6),
                            Text(
                              'LIVE AI FAIR PRICE',
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                                color: Colors.green.shade800,
                                letterSpacing: 0.5,
                              ),
                            ),
                          ],
                        ),
                        if (_priceEstimate != null)
                          Text(
                            '${(((_priceEstimate!['confidence'] as num?)?.toDouble() ?? 0.90) * 100).toInt()}% Conf.',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.bold,
                              color: Colors.purple.shade700,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    if (_isLoadingPriceEstimate)
                      Row(
                        children: [
                          SizedBox(
                            width: 14,
                            height: 14,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.green.shade700),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Synthesizing wholesale market auctions...',
                            style: TextStyle(fontSize: 12, color: Colors.grey.shade700),
                          ),
                        ],
                      )
                    else if (_priceEstimate != null)
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                'LKR ${(_priceEstimate!['suggestedPriceMin'] as num?)?.toDouble().toStringAsFixed(0)} – ${(_priceEstimate!['suggestedPriceMax'] as num?)?.toDouble().toStringAsFixed(0)} / $_selectedUnit',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.green.shade900,
                                ),
                              ),
                              TextButton(
                                onPressed: () {
                                  final minP = (_priceEstimate!['suggestedPriceMin'] as num?)?.toDouble() ?? 0;
                                  _minPriceController.text = minP.toStringAsFixed(0);
                                },
                                style: TextButton.styleFrom(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                  minimumSize: Size.zero,
                                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                ),
                                child: const Text(
                                  'Apply Floor',
                                  style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Text(
                            _priceEstimate!['reasoningSummary'] as String? ?? 'Based on recent regional terminal auctions.',
                            style: TextStyle(fontSize: 11, color: Colors.grey.shade800),
                          ),
                        ],
                      ),
                  ],
                ),
              ),

              // Grade & Floor Price Row
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 2,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Claimed Grade', style: TextStyle(fontWeight: FontWeight.bold)),
                        const SizedBox(height: 6),
                        DropdownButtonFormField<String>(
                          value: _selectedGrade,
                          decoration: InputDecoration(
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                          ),
                          items: const [
                            DropdownMenuItem(value: 'A', child: Text('Grade A')),
                            DropdownMenuItem(value: 'B', child: Text('Grade B')),
                            DropdownMenuItem(value: 'C', child: Text('Grade C')),
                          ],
                          onChanged: (val) {
                            if (val != null) {
                              setState(() => _selectedGrade = val);
                              _fetchPriceEstimate();
                            }
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 3,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Floor Price (LKR)', style: TextStyle(fontWeight: FontWeight.bold)),
                        const SizedBox(height: 6),
                        TextFormField(
                          controller: _minPriceController,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          decoration: InputDecoration(
                            hintText: 'Optional floor price',
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // Pickup Window
              const Text('Pickup Availability Window', style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 6),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text('From: ${dateFormat.format(_pickupStart)}'),
                      onPressed: () => _pickPickupWindow(context, true),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text('To: ${dateFormat.format(_pickupEnd)}'),
                      onPressed: () => _pickPickupWindow(context, false),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 20),

              // Photos Section
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Produce Photos', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                  Text(
                    '${_photoUrls.length} selected',
                    style: TextStyle(fontSize: 12, color: Colors.grey.shade600),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              
              // Photo Upload Action Buttons (Camera & Device)
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.camera_alt, color: Colors.green),
                      label: const Text('Take Photo', style: TextStyle(color: Colors.green, fontWeight: FontWeight.w600)),
                      onPressed: _isUploadingPhoto ? null : _takePhoto,
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        side: BorderSide(color: Colors.green.shade400),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.photo_library, color: Colors.blue),
                      label: const Text('From Device', style: TextStyle(color: Colors.blue, fontWeight: FontWeight.w600)),
                      onPressed: _isUploadingPhoto ? null : _pickFromGallery,
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        side: BorderSide(color: Colors.blue.shade400),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),

              // Uploading indicator
              if (_isUploadingPhoto)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 10),
                  decoration: BoxDecoration(
                    color: Colors.green.shade50,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: Colors.green.shade200),
                  ),
                  child: Row(
                    children: [
                      SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.green.shade700),
                      ),
                      const SizedBox(width: 12),
                      Text(
                        'Uploading selected photo...',
                        style: TextStyle(fontSize: 13, color: Colors.green.shade800, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                ),

              // Or Paste Photo URL
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _photoUrlController,
                      decoration: InputDecoration(
                        hintText: 'Or paste image URL...',
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton(
                    onPressed: _addPhotoUrl,
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                    child: const Text('Add URL'),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Photo previews
              if (_photoUrls.isNotEmpty)
                SizedBox(
                  height: 90,
                  child: ListView.builder(
                    scrollDirection: Axis.horizontal,
                    itemCount: _photoUrls.length,
                    itemBuilder: (context, index) {
                      final url = _photoUrls[index];
                      final isNetwork = url.startsWith('http://') || url.startsWith('https://');
                      return Stack(
                        children: [
                          Container(
                            width: 90,
                            height: 90,
                            margin: const EdgeInsets.only(right: 10),
                            decoration: BoxDecoration(
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(color: Colors.grey.shade300),
                              color: Colors.grey.shade100,
                            ),
                            clipBehavior: Clip.antiAlias,
                            child: isNetwork
                                ? Image.network(
                                    url,
                                    fit: BoxFit.cover,
                                    errorBuilder: (ctx, err, stack) => const Center(
                                      child: Icon(Icons.broken_image, color: Colors.grey),
                                    ),
                                  )
                                : Image.file(
                                    File(url),
                                    fit: BoxFit.cover,
                                    errorBuilder: (ctx, err, stack) => const Center(
                                      child: Icon(Icons.broken_image, color: Colors.grey),
                                    ),
                                  ),
                          ),
                          Positioned(
                            top: 4,
                            right: 14,
                            child: InkWell(
                              onTap: () => setState(() => _photoUrls.removeAt(index)),
                              child: Container(
                                padding: const EdgeInsets.all(3),
                                decoration: const BoxDecoration(
                                  color: Colors.black54,
                                  shape: BoxShape.circle,
                                ),
                                child: const Icon(Icons.close, size: 14, color: Colors.white),
                              ),
                            ),
                          ),
                        ],
                      );
                    },
                  ),
                ),
              const SizedBox(height: 30),

              // Submit Button
              SizedBox(
                width: double.infinity,
                height: 52,
                child: ElevatedButton.icon(
                  icon: _isSubmitting
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                        )
                      : const Icon(Icons.cloud_upload_outlined),
                  label: Text(
                    _isSubmitting ? 'Publishing Listing...' : 'Submit Listing & Discover Price',
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.green.shade700,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  onPressed: _isSubmitting ? null : _submitListing,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
