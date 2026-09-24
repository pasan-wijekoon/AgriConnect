namespace AgriConnect.Api.Dtos;

public record NearestCentreResponse(
    Guid CentreId,
    string Name,
    decimal DistanceKm,
    double? EtaMinutes,
    int Capacity,
    bool Degraded);

public record CollectionCentreResponse(
    Guid Id,
    string Name,
    decimal Latitude,
    decimal Longitude,
    int Capacity,
    Guid RegionId);
