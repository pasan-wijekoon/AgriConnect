using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

internal class FakeDistanceService(Func<decimal, decimal, DistanceResult> resolve) : IDistanceService
{
    public Task<DistanceResult> GetDistanceAsync(
        decimal originLat, decimal originLng, decimal destLat, decimal destLng,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(resolve(destLat, destLng));
}

public class CollectionCentreServiceTests
{
    private static readonly Guid RegionA = Guid.NewGuid();
    private static readonly Guid RegionB = Guid.NewGuid();

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CollectionCentre MakeCentre(Guid regionId, decimal lat, decimal lng, string name = "Centre") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Latitude = lat,
        Longitude = lng,
        Capacity = 3,
        RegionId = regionId
    };

    [Fact]
    public async Task FindNearestAsync_SortsByDistanceAscending()
    {
        await using var db = NewInMemoryDb();
        var near = MakeCentre(RegionA, 1, 1, "Near");
        var far = MakeCentre(RegionA, 10, 10, "Far");
        db.CollectionCentres.AddRange(near, far);
        await db.SaveChangesAsync();

        // Distance service reports "distance" as a simple function of latitude, so
        // the ordering is deterministic and independent of the real haversine math.
        var distanceService = new FakeDistanceService((lat, _) => new DistanceResult(lat, null, false));
        var service = new CollectionCentreService(db, distanceService);

        var results = await service.FindNearestAsync(lat: 0, lng: 0, regionId: null);

        Assert.Equal(["Near", "Far"], results.Select(r => r.Name));
    }

    [Fact]
    public async Task FindNearestAsync_WithRegionId_OnlyReturnsCentresInThatRegion()
    {
        await using var db = NewInMemoryDb();
        db.CollectionCentres.AddRange(
            MakeCentre(RegionA, 1, 1, "InRegionA"),
            MakeCentre(RegionB, 2, 2, "InRegionB"));
        await db.SaveChangesAsync();

        var distanceService = new FakeDistanceService((lat, _) => new DistanceResult(lat, null, false));
        var service = new CollectionCentreService(db, distanceService);

        var results = await service.FindNearestAsync(lat: 0, lng: 0, regionId: RegionA);

        Assert.Single(results);
        Assert.Equal("InRegionA", results[0].Name);
    }

    [Fact]
    public async Task FindNearestAsync_IncludesFullCapacityCentresRatherThanHidingThem()
    {
        await using var db = NewInMemoryDb();
        var fullCentre = MakeCentre(RegionA, 1, 1, "FullCentre");
        fullCentre.Capacity = 1;
        db.CollectionCentres.Add(fullCentre);
        await db.SaveChangesAsync();

        var distanceService = new FakeDistanceService((lat, _) => new DistanceResult(lat, null, false));
        var service = new CollectionCentreService(db, distanceService);

        var results = await service.FindNearestAsync(lat: 0, lng: 0, regionId: null);

        Assert.Single(results);
        Assert.Equal(1, results[0].Capacity);
    }

    [Fact]
    public async Task FindNearestAsync_PropagatesDegradedFlagFromDistanceService()
    {
        await using var db = NewInMemoryDb();
        db.CollectionCentres.Add(MakeCentre(RegionA, 1, 1));
        await db.SaveChangesAsync();

        var distanceService = new FakeDistanceService((lat, _) => new DistanceResult(lat, null, Degraded: true));
        var service = new CollectionCentreService(db, distanceService);

        var results = await service.FindNearestAsync(lat: 0, lng: 0, regionId: null);

        Assert.True(results[0].Degraded);
    }

    [Fact]
    public async Task ListAllAsync_ReturnsEveryCentreOrderedByName()
    {
        await using var db = NewInMemoryDb();
        db.CollectionCentres.AddRange(MakeCentre(RegionA, 1, 1, "Zebra"), MakeCentre(RegionB, 2, 2, "Alpha"));
        await db.SaveChangesAsync();

        var service = new CollectionCentreService(db, new FakeDistanceService((lat, _) => new DistanceResult(lat, null, false)));

        var results = await service.ListAllAsync();

        Assert.Equal(["Alpha", "Zebra"], results.Select(r => r.Name));
    }
}
