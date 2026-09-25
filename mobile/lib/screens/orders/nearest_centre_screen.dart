import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';

import '../../models/collection_centre.dart';
import '../../providers/dev_identity_provider.dart';
import '../../services/order_service.dart';
import '../../theme/app_colors.dart';

/// Nearest Collection Centre screen (FR21, plan §11 / Design.md §35).
/// Never calls a Maps/Distance API directly (CLAUDE.md §19/§22) — only ever
/// calls the backend's `GET /api/collection-centres/nearest`, which does
/// that server-side and reports `degraded: true` if it had to fall back to
/// a straight-line estimate.
class NearestCentreScreen extends StatefulWidget {
  const NearestCentreScreen({super.key});

  @override
  State<NearestCentreScreen> createState() => _NearestCentreScreenState();
}

enum _LoadState { idle, locating, loading, loaded, error }

class _NearestCentreScreenState extends State<NearestCentreScreen> {
  final _service = OrderService();

  _LoadState _state = _LoadState.idle;
  String? _error;
  List<NearestCentre> _centres = [];

  Future<void> _findNearest() async {
    setState(() {
      _state = _LoadState.locating;
      _error = null;
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
          accuracy: LocationAccuracy.medium,
          timeLimit: Duration(seconds: 10),
        ),
      );

      if (!mounted) return;
      setState(() => _state = _LoadState.loading);

      final identity = context.read<DevIdentityProvider>();
      final centres = await _service.nearestCentres(
        devRoleToHeader(identity.role),
        identity.userId,
        lat: position.latitude,
        lng: position.longitude,
      );

      if (!mounted) return;
      setState(() {
        _centres = centres;
        _state = _LoadState.loaded;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString().replaceFirst('Exception: ', '');
        _state = _LoadState.error;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Find Collection Centre')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text('Use your current location to find nearby centres.'),
            const SizedBox(height: 16),
            ElevatedButton.icon(
              onPressed: _state == _LoadState.locating || _state == _LoadState.loading
                  ? null
                  : _findNearest,
              icon: _state == _LoadState.locating || _state == _LoadState.loading
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: Colors.white),
                    )
                  : const Icon(Icons.my_location),
              label: Text(_state == _LoadState.locating
                  ? 'Getting location…'
                  : _state == _LoadState.loading
                      ? 'Searching…'
                      : 'Use My Location'),
            ),
            const SizedBox(height: 24),
            if (_state == _LoadState.error)
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.errorBg,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(_error ?? 'Something went wrong.',
                    style: const TextStyle(color: AppColors.error)),
              ),
            if (_state == _LoadState.loaded) _buildResults(),
          ],
        ),
      ),
    );
  }

  Widget _buildResults() {
    if (_centres.isEmpty) {
      return const Text(
        'No collection centres were found.',
        style: TextStyle(color: AppColors.textSecondary),
      );
    }

    final anyDegraded = _centres.any((c) => c.degraded);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (anyDegraded)
          Container(
            margin: const EdgeInsets.only(bottom: 16),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.pendingBg,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Approximate distance',
                    style: TextStyle(
                        fontWeight: FontWeight.w600, color: AppColors.pending)),
                SizedBox(height: 4),
                Text(
                  'Distance is estimated using the available location data.',
                  style: TextStyle(color: AppColors.pending, fontSize: 13),
                ),
              ],
            ),
          ),
        const Text('Nearby Centres', style: TextStyle(fontWeight: FontWeight.w700)),
        const SizedBox(height: 12),
        for (final centre in _centres) ...[
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(centre.name,
                            style: const TextStyle(fontWeight: FontWeight.w600)),
                        const SizedBox(height: 6),
                        Text('${centre.distanceKm.toStringAsFixed(1)} km'
                            '${centre.etaMinutes != null ? ' · ~${centre.etaMinutes!.round()} min' : ''}'),
                        if (centre.degraded)
                          const Padding(
                            padding: EdgeInsets.only(top: 4),
                            child: Text('Approximate',
                                style: TextStyle(
                                    color: AppColors.pending, fontSize: 12)),
                          ),
                      ],
                    ),
                  ),
                  Text('Cap. ${centre.capacity}',
                      style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
        ],
      ],
    );
  }
}
