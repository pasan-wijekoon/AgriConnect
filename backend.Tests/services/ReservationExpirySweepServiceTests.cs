using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// Tests SweepExpiredReservationsAsync directly against the EF Core InMemory
/// provider — it's a plain conditional update, no serializable-transaction
/// mechanics involved, so InMemory is sufficient (unlike StockReservationService).
/// </summary>
public class ReservationExpirySweepServiceTests
{
    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (Order Order, StockReservation Reservation) AddOrderWithReservation(
        AgriConnectDbContext db, OrderStatus status, DateTimeOffset expiresAt)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ListingId = Guid.NewGuid(),
            BuyerId = Guid.NewGuid(),
            Quantity = 10m,
            Status = status,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            ListingId = order.ListingId,
            OrderId = order.Id,
            ReservedQuantity = order.Quantity,
            ExpiresAt = expiresAt
        };
        db.Orders.Add(order);
        db.StockReservations.Add(reservation);
        return (order, reservation);
    }

    [Fact]
    public async Task SweepExpiredReservationsAsync_CancelsPendingOrderWithExpiredReservation()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = AddOrderWithReservation(db, OrderStatus.Pending, DateTimeOffset.UtcNow.AddMinutes(-1));
        await db.SaveChangesAsync();

        var cancelledCount = await ReservationExpirySweepService.SweepExpiredReservationsAsync(db);

        Assert.Equal(1, cancelledCount);
        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
    }

    [Fact]
    public async Task SweepExpiredReservationsAsync_LeavesPendingOrderWithUnexpiredReservationAlone()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = AddOrderWithReservation(db, OrderStatus.Pending, DateTimeOffset.UtcNow.AddMinutes(30));
        await db.SaveChangesAsync();

        var cancelledCount = await ReservationExpirySweepService.SweepExpiredReservationsAsync(db);

        Assert.Equal(0, cancelledCount);
        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Pending, reloaded.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Approved)]
    [InlineData(OrderStatus.Scheduled)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task SweepExpiredReservationsAsync_NeverTouchesNonPendingOrders_EvenIfReservationExpired(OrderStatus status)
    {
        await using var db = NewInMemoryDb();
        var (order, _) = AddOrderWithReservation(db, status, DateTimeOffset.UtcNow.AddMinutes(-1));
        await db.SaveChangesAsync();

        var cancelledCount = await ReservationExpirySweepService.SweepExpiredReservationsAsync(db);

        Assert.Equal(0, cancelledCount);
        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(status, reloaded.Status);
    }

    [Fact]
    public async Task SweepExpiredReservationsAsync_HandlesMultipleExpiredOrdersInOnePass()
    {
        await using var db = NewInMemoryDb();
        AddOrderWithReservation(db, OrderStatus.Pending, DateTimeOffset.UtcNow.AddMinutes(-5));
        AddOrderWithReservation(db, OrderStatus.Pending, DateTimeOffset.UtcNow.AddMinutes(-1));
        AddOrderWithReservation(db, OrderStatus.Pending, DateTimeOffset.UtcNow.AddMinutes(10)); // not expired
        await db.SaveChangesAsync();

        var cancelledCount = await ReservationExpirySweepService.SweepExpiredReservationsAsync(db);

        Assert.Equal(2, cancelledCount);
        Assert.Equal(1, await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending));
        Assert.Equal(2, await db.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled));
    }

    [Fact]
    public async Task SweepExpiredReservationsAsync_IsIdempotent_SecondRunFindsNothing()
    {
        await using var db = NewInMemoryDb();
        AddOrderWithReservation(db, OrderStatus.Pending, DateTimeOffset.UtcNow.AddMinutes(-1));
        await db.SaveChangesAsync();

        var firstRun = await ReservationExpirySweepService.SweepExpiredReservationsAsync(db);
        var secondRun = await ReservationExpirySweepService.SweepExpiredReservationsAsync(db);

        Assert.Equal(1, firstRun);
        Assert.Equal(0, secondRun);
    }
}
