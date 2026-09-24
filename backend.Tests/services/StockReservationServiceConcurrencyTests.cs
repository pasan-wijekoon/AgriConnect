using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.services;

/// <summary>
/// FR9 concurrency suite (plan §7.3) — deliberately its own test class, not folded
/// into general unit tests, since these exercise real Postgres serializable
/// transactions and cannot run against the EF Core InMemory provider.
///
/// Requires a local PostgreSQL matching backend/appsettings.json's "Default"
/// connection string (the same instance the migration was applied to) to be
/// running; these tests will fail with a connection error otherwise, same as
/// `dotnet ef database update` would.
///
/// Each test uses a freshly generated ListingId so tests never interfere with
/// each other or with the seeded fixture data, and each simulated "request" opens
/// its own DbContext/connection — mirroring how separate concurrent HTTP requests
/// would each get their own scoped DbContext in the real app.
/// </summary>
public class StockReservationServiceConcurrencyTests
{
    private const string ConnectionString =
        "Host=localhost;Database=agriconnect;Username=postgres;Password=postgres";

    private static AgriConnectDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AgriConnectDbContext(options);
    }

    private static StockReservationService NewService(AgriConnectDbContext db) =>
        new(db, new ConfigurationBuilder().Build());

    private static Order NewPendingOrder(Guid listingId, decimal quantity) => new()
    {
        Id = Guid.NewGuid(),
        ListingId = listingId,
        BuyerId = Guid.NewGuid(),
        Quantity = quantity,
        Status = OrderStatus.Pending,
        DeliveryPreference = DeliveryPreference.Pickup,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task TwoConcurrentReservations_WhereSumExceedsAvailable_ExactlyOneSucceeds()
    {
        var listingId = Guid.NewGuid();
        const decimal available = 100m;

        var task1 = Task.Run(async () =>
        {
            await using var db = NewContext();
            return await NewService(db).ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 80m), available);
        });
        var task2 = Task.Run(async () =>
        {
            await using var db = NewContext();
            return await NewService(db).ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 50m), available);
        });

        var results = await Task.WhenAll(task1, task2);

        Assert.Single(results, r => r.Success);
        Assert.Single(results, r => !r.Success);

        await using var verifyDb = NewContext();
        var totalReserved = await verifyDb.StockReservations
            .Where(r => r.ListingId == listingId)
            .SumAsync(r => r.ReservedQuantity);
        Assert.True(totalReserved <= available, $"Total reserved {totalReserved} exceeded available {available}.");
    }

    [Fact]
    public async Task TwoConcurrentReservations_WhereSumEqualsAvailable_BothSucceed_ThirdFails()
    {
        var listingId = Guid.NewGuid();
        const decimal available = 100m;

        var task1 = Task.Run(async () =>
        {
            await using var db = NewContext();
            return await NewService(db).ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 60m), available);
        });
        var task2 = Task.Run(async () =>
        {
            await using var db = NewContext();
            return await NewService(db).ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 40m), available);
        });

        var results = await Task.WhenAll(task1, task2);
        Assert.All(results, r => Assert.True(r.Success, r.FailureReason));

        await using var thirdDb = NewContext();
        var thirdResult = await NewService(thirdDb)
            .ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 1m), available);
        Assert.False(thirdResult.Success);

        await using var verifyDb = NewContext();
        var totalReserved = await verifyDb.StockReservations
            .Where(r => r.ListingId == listingId)
            .SumAsync(r => r.ReservedQuantity);
        Assert.Equal(available, totalReserved);
    }

    [Fact]
    public async Task CancellingAPendingOrder_FreesItsStockImmediately()
    {
        var listingId = Guid.NewGuid();
        const decimal available = 50m;

        await using var db1 = NewContext();
        var firstOrder = NewPendingOrder(listingId, 50m);
        var firstResult = await NewService(db1).ReserveAndPlaceOrderAsync(firstOrder, available);
        Assert.True(firstResult.Success);

        // A second request for the same (fully reserved) listing must fail...
        await using var db2 = NewContext();
        var blockedResult = await NewService(db2)
            .ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 10m), available);
        Assert.False(blockedResult.Success);

        // ...until the first order is cancelled, which frees its stock immediately
        // (plan §7.2 — no separate release write is needed; the active-reservation
        // sum already filters out Cancelled orders).
        await using var cancelDb = NewContext();
        var trackedOrder = await cancelDb.Orders.FirstAsync(o => o.Id == firstOrder.Id);
        trackedOrder.Status = OrderStatus.Cancelled;
        await cancelDb.SaveChangesAsync();

        await using var db3 = NewContext();
        var freedResult = await NewService(db3)
            .ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 50m), available);
        Assert.True(freedResult.Success, freedResult.FailureReason);
    }

    [Fact]
    public async Task TenConcurrentReservations_ForCapacityOfOne_ExactlyOneSucceeds()
    {
        var listingId = Guid.NewGuid();
        const decimal available = 1m;

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await using var db = NewContext();
            return await NewService(db).ReserveAndPlaceOrderAsync(NewPendingOrder(listingId, 1m), available);
        }));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => r.Success));
        Assert.Equal(9, results.Count(r => !r.Success));

        await using var verifyDb = NewContext();
        var totalReserved = await verifyDb.StockReservations
            .Where(r => r.ListingId == listingId)
            .SumAsync(r => r.ReservedQuantity);
        Assert.True(totalReserved <= available, $"Total reserved {totalReserved} exceeded available {available}.");
    }
}
