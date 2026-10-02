using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.services;

/// <summary>
/// Rules added in the order-flow overhaul: every region has a collection centre,
/// nearest-centre lookups never hang on a slow Maps provider, approved orders keep
/// holding stock after their Pending reservation window passes.
/// </summary>
public class LogisticsRulesTests
{
    private const string ConnectionString =
        "Host=localhost;Database=agriconnect;Username=postgres;Password=postgres";

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public void Fixtures_GiveEveryRegionExactlyOneCentre()
    {
        var centres = OrderLogisticsFixtures.GetCollectionCentres();

        // Component A seeds regions b1000000-...-000000000001 .. 00000000000f (15).
        var expected = Enumerable.Range(1, 15)
            .Select(n => Guid.Parse($"b1000000-0000-0000-0000-{n:x12}"))
            .ToHashSet();

        Assert.Equal(15, centres.Count);
        Assert.Equal(expected, centres.Select(c => c.RegionId).ToHashSet());
        Assert.Equal(centres.Count, centres.Select(c => c.Id).Distinct().Count());
    }

    private sealed class HangingDistanceService : IDistanceService
    {
        public async Task<DistanceResult> GetDistanceAsync(
            decimal originLat, decimal originLng, decimal destLat, decimal destLng,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public async Task FindNearest_WhenMapsProviderHangs_FallsBackToDegradedEstimateWithinBudget()
    {
        await using var db = NewInMemoryDb();
        var region = Guid.NewGuid();
        db.CollectionCentres.AddRange(
            new CollectionCentre { Id = Guid.NewGuid(), Name = "Far", Latitude = 10, Longitude = 10, Capacity = 1, RegionId = region },
            new CollectionCentre { Id = Guid.NewGuid(), Name = "Near", Latitude = 1, Longitude = 1, Capacity = 1, RegionId = region });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new("MapsApi:TotalBudgetSeconds", "1")])
            .Build();
        var service = new CollectionCentreService(db, new HangingDistanceService(), config);

        var started = DateTimeOffset.UtcNow;
        var results = await service.FindNearestAsync(0, 0, regionId: null);

        Assert.True(DateTimeOffset.UtcNow - started < TimeSpan.FromSeconds(5), "lookup did not respect the time budget");
        Assert.All(results, r => Assert.True(r.Degraded));
        Assert.Equal(["Near", "Far"], results.Select(r => r.Name));
    }

    [Fact]
    public async Task FindNearest_WhenRegionHasNoCentre_RanksEveryCentreInstead()
    {
        await using var db = NewInMemoryDb();
        db.CollectionCentres.Add(new CollectionCentre
        {
            Id = Guid.NewGuid(), Name = "Elsewhere", Latitude = 1, Longitude = 1, Capacity = 1, RegionId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var service = new CollectionCentreService(db, new FakeDistanceService((lat, _) => new DistanceResult(lat, null, false)));

        var results = await service.FindNearestAsync(0, 0, regionId: Guid.NewGuid());

        Assert.Single(results);
    }

    [Fact]
    public async Task ApprovedOrder_KeepsHoldingStock_EvenAfterItsReservationWindowHasPassed()
    {
        var listingId = Guid.NewGuid();
        const decimal available = 100m;

        await using (var db = new AgriConnectDbContext(new DbContextOptionsBuilder<AgriConnectDbContext>().UseNpgsql(ConnectionString).Options))
        {
            var order = new Order
            {
                Id = Guid.NewGuid(), ListingId = listingId, BuyerId = Guid.NewGuid(), Quantity = 80m,
                Status = OrderStatus.Approved, DeliveryPreference = DeliveryPreference.Pickup,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Orders.Add(order);
            db.StockReservations.Add(new StockReservation
            {
                Id = Guid.NewGuid(), ListingId = listingId, OrderId = order.Id, ReservedQuantity = 80m,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) // window long gone, but the order is Approved
            });
            await db.SaveChangesAsync();
        }

        await using var db2 = new AgriConnectDbContext(new DbContextOptionsBuilder<AgriConnectDbContext>().UseNpgsql(ConnectionString).Options);
        var service = new StockReservationService(db2, new ConfigurationBuilder().Build());

        var tooMuch = await service.ReserveAndPlaceOrderAsync(
            new Order
            {
                Id = Guid.NewGuid(), ListingId = listingId, BuyerId = Guid.NewGuid(), Quantity = 30m,
                Status = OrderStatus.Pending, DeliveryPreference = DeliveryPreference.Pickup,
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            }, available);

        Assert.False(tooMuch.Success);
    }
}
