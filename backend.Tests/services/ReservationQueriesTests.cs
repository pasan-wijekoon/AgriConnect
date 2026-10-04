using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// "Which reservations currently hold stock" (FR9) — the one rule shared by the ordering
/// check and the "available now" figure on listings. Plain filter + group, so the EF Core
/// InMemory provider is enough (the serializable-transaction behaviour is covered by
/// StockReservationServiceConcurrencyTests).
/// </summary>
public class ReservationQueriesTests
{
    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void Add(AgriConnectDbContext db, Guid listingId, decimal quantity, OrderStatus status, DateTimeOffset expiresAt)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            BuyerId = Guid.NewGuid(),
            Quantity = quantity,
            Status = status,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);
        db.StockReservations.Add(new StockReservation
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            OrderId = order.Id,
            ReservedQuantity = quantity,
            ExpiresAt = expiresAt
        });
    }

    [Fact]
    public async Task ActiveReserved_CountsLivePendingAndApprovedScheduledCompleted_ButNotExpiredPendingOrCancelled()
    {
        await using var db = NewInMemoryDb();
        var now = DateTimeOffset.UtcNow;
        var listing = Guid.NewGuid();

        Add(db, listing, 10m, OrderStatus.Pending, now.AddHours(5));      // live pending: held
        Add(db, listing, 20m, OrderStatus.Pending, now.AddHours(-1));     // expired pending: released
        Add(db, listing, 30m, OrderStatus.Approved, now.AddHours(-10));   // approved: held even past ExpiresAt
        Add(db, listing, 40m, OrderStatus.Scheduled, now.AddHours(-10));  // scheduled: held
        Add(db, listing, 50m, OrderStatus.Completed, now.AddHours(-10));  // completed: stays committed
        Add(db, listing, 60m, OrderStatus.Cancelled, now.AddHours(5));    // cancelled: released
        await db.SaveChangesAsync();

        var reserved = await ReservationQueries.ActiveReservedByListingAsync(db, [listing], now);

        Assert.Equal(10m + 30m + 40m + 50m, reserved[listing]);
    }

    [Fact]
    public async Task ActiveReserved_IsGroupedPerListing_AndOmitsListingsWithNothingHeld()
    {
        await using var db = NewInMemoryDb();
        var now = DateTimeOffset.UtcNow;
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var untouched = Guid.NewGuid();

        Add(db, a, 5m, OrderStatus.Approved, now);
        Add(db, a, 7m, OrderStatus.Approved, now);
        Add(db, b, 1m, OrderStatus.Approved, now);
        await db.SaveChangesAsync();

        var reserved = await ReservationQueries.ActiveReservedByListingAsync(db, [a, b, untouched], now);

        Assert.Equal(12m, reserved[a]);
        Assert.Equal(1m, reserved[b]);
        Assert.False(reserved.ContainsKey(untouched));
    }

    [Fact]
    public async Task ActiveReserved_WithNoListingIds_ReturnsEmptyWithoutQuerying()
    {
        await using var db = NewInMemoryDb();

        var reserved = await ReservationQueries.ActiveReservedByListingAsync(db, [], DateTimeOffset.UtcNow);

        Assert.Empty(reserved);
    }
}
