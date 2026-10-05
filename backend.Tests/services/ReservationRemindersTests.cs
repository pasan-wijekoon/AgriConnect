using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// FR22 reminders sent by the expiry sweep: the Buyer is warned shortly before a Pending
/// order's reservation ends, the centre's Officers are reminded about orders that wait long.
/// Each reminder is sent once (tracked on the reservation).
/// </summary>
public class ReservationRemindersTests
{
    private static readonly TimeSpan BuyerLead = TimeSpan.FromHours(6);
    private static readonly TimeSpan OfficerAfter = TimeSpan.FromHours(24);

    private sealed record Seed(Guid RegionId, Guid ListingId, Guid CentreId, Guid OfficerId, Guid OtherCentreOfficerId);

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Seed> SeedWorldAsync(AgriConnectDbContext db)
    {
        var region = new Region { Id = Guid.NewGuid(), Name = "Kandy" };
        var crop = new Crop { Id = Guid.NewGuid(), Name = "Carrots", Category = "Vegetables" };
        var listing = new Listing
        {
            Id = Guid.NewGuid(), FarmerId = Guid.NewGuid(), CropId = crop.Id, Crop = crop, RegionId = region.Id,
            Region = region, Quantity = 500, Unit = "kg", ClaimedGrade = "A", Status = "Published"
        };
        var centre = new CollectionCentre
        {
            Id = Guid.NewGuid(), Name = "Kandy Central", Latitude = 7.3m, Longitude = 80.6m, Capacity = 5, RegionId = region.Id
        };
        var otherCentre = new CollectionCentre
        {
            Id = Guid.NewGuid(), Name = "Galle", Latitude = 6.0m, Longitude = 80.2m, Capacity = 5, RegionId = Guid.NewGuid()
        };
        var officer = new User
        {
            Id = Guid.NewGuid(), FullName = "Kandy Officer", Email = "k@example.test", PasswordHash = "x",
            Role = Roles.Officer, CollectionCentreId = centre.Id
        };
        var otherOfficer = new User
        {
            Id = Guid.NewGuid(), FullName = "Galle Officer", Email = "g@example.test", PasswordHash = "x",
            Role = Roles.Officer, CollectionCentreId = otherCentre.Id
        };
        db.AddRange(region, crop, listing, centre, otherCentre, officer, otherOfficer);
        await db.SaveChangesAsync();
        return new Seed(region.Id, listing.Id, centre.Id, officer.Id, otherOfficer.Id);
    }

    private static async Task<(Order Order, StockReservation Reservation)> AddOrderAsync(
        AgriConnectDbContext db, Seed seed, OrderStatus status, TimeSpan age, TimeSpan expiresIn)
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(), ListingId = seed.ListingId, BuyerId = Guid.NewGuid(), Quantity = 100m, Status = status,
            DeliveryPreference = DeliveryPreference.Pickup, CreatedAt = now - age, UpdatedAt = now - age
        };
        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(), ListingId = seed.ListingId, OrderId = order.Id, ReservedQuantity = 100m,
            ExpiresAt = now + expiresIn
        };
        db.Orders.Add(order);
        db.StockReservations.Add(reservation);
        await db.SaveChangesAsync();
        return (order, reservation);
    }

    private static Task<int> RunAsync(AgriConnectDbContext db) =>
        ReservationExpirySweepService.SendRemindersAsync(db, new NotificationService(db), BuyerLead, OfficerAfter);

    [Fact]
    public async Task Buyer_IsWarnedOnce_WhenTheReservationEndsWithinTheLeadTime()
    {
        await using var db = NewInMemoryDb();
        var seed = await SeedWorldAsync(db);
        var (order, reservation) = await AddOrderAsync(db, seed, OrderStatus.Pending, TimeSpan.FromHours(3), TimeSpan.FromHours(5));

        var first = await RunAsync(db);
        var second = await RunAsync(db);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var notification = Assert.Single(await db.Notifications.Where(n => n.UserId == order.BuyerId).ToListAsync());
        Assert.Equal("ReservationExpiring", notification.Type);
        Assert.Contains("100 kg of Carrots", notification.Message);
        Assert.NotNull((await db.StockReservations.FindAsync(reservation.Id))!.BuyerReminderSentAt);
    }

    [Theory]
    [InlineData(OrderStatus.Approved, 5.0)]   // already approved: nothing to warn about
    [InlineData(OrderStatus.Cancelled, 5.0)]
    [InlineData(OrderStatus.Pending, 20.0)]   // far from expiry
    [InlineData(OrderStatus.Pending, -1.0)]   // already expired: the sweep handles it, no reminder
    public async Task Buyer_IsNotWarned_ForOtherSituations(OrderStatus status, double expiresInHours)
    {
        await using var db = NewInMemoryDb();
        var seed = await SeedWorldAsync(db);
        var (order, _) = await AddOrderAsync(db, seed, status, TimeSpan.FromHours(2), TimeSpan.FromHours(expiresInHours));

        await RunAsync(db);

        Assert.Empty(await db.Notifications.Where(n => n.UserId == order.BuyerId).ToListAsync());
    }

    [Fact]
    public async Task Officers_OfTheOrdersCentre_AreRemindedOnce_WhenAnOrderWaitsLong()
    {
        await using var db = NewInMemoryDb();
        var seed = await SeedWorldAsync(db);
        var (order, reservation) = await AddOrderAsync(db, seed, OrderStatus.Pending, TimeSpan.FromHours(25), TimeSpan.FromHours(20));

        var first = await RunAsync(db);
        var second = await RunAsync(db);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var notification = Assert.Single(await db.Notifications.Where(n => n.UserId == seed.OfficerId).ToListAsync());
        Assert.Equal("OrderAwaitingApproval", notification.Type);
        Assert.Contains(order.Id.ToString()[..8], notification.Message);
        Assert.Empty(await db.Notifications.Where(n => n.UserId == seed.OtherCentreOfficerId).ToListAsync());
        Assert.NotNull((await db.StockReservations.FindAsync(reservation.Id))!.OfficerReminderSentAt);
    }

    [Fact]
    public async Task Officers_AreNotReminded_AboutRecentOrders()
    {
        await using var db = NewInMemoryDb();
        var seed = await SeedWorldAsync(db);
        await AddOrderAsync(db, seed, OrderStatus.Pending, TimeSpan.FromHours(3), TimeSpan.FromHours(40));

        var sent = await RunAsync(db);

        Assert.Equal(0, sent);
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task NoPendingOrders_MeansNoReminders()
    {
        await using var db = NewInMemoryDb();

        Assert.Equal(0, await RunAsync(db));
    }
}
