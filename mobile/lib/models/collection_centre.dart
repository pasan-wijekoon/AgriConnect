/// Mirrors `backend/src/dtos/CollectionCentreDtos.cs` (plan §11).
library;

class NearestCentre {
  final String centreId;
  final String name;
  final double distanceKm;
  final double? etaMinutes;
  final int capacity;
  final bool degraded;

  NearestCentre({
    required this.centreId,
    required this.name,
    required this.distanceKm,
    this.etaMinutes,
    required this.capacity,
    required this.degraded,
  });

  factory NearestCentre.fromJson(Map<String, dynamic> json) => NearestCentre(
        centreId: json['centreId'] as String,
        name: json['name'] as String,
        distanceKm: (json['distanceKm'] as num).toDouble(),
        etaMinutes: json['etaMinutes'] == null
            ? null
            : (json['etaMinutes'] as num).toDouble(),
        capacity: json['capacity'] as int,
        degraded: json['degraded'] as bool,
      );
}

class CollectionCentre {
  final String id;
  final String name;
  final double latitude;
  final double longitude;
  final int capacity;
  final String regionId;

  CollectionCentre({
    required this.id,
    required this.name,
    required this.latitude,
    required this.longitude,
    required this.capacity,
    required this.regionId,
  });

  factory CollectionCentre.fromJson(Map<String, dynamic> json) =>
      CollectionCentre(
        id: json['id'] as String,
        name: json['name'] as String,
        latitude: (json['latitude'] as num).toDouble(),
        longitude: (json['longitude'] as num).toDouble(),
        capacity: json['capacity'] as int,
        regionId: json['regionId'] as String,
      );
}
