using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// The order activity timeline (FR11/FR20): friendly summaries built from audit rows, actor
/// names, order, and the same visibility rules as viewing the order itself.
/// </summary>
public class OrderActivityTests
{
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly Guid FarmerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();
    private static readonly Guid OtherBuyerId = Guid.NewGuid();
    private static readonly Guid OfficerUserId = Guid.NewGuid();

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static OrderService NewService(AgriConnectDbContext db) =>
        new(db,
            new FakeListingAvailabilityPort().Add(new ListingAvailability(ListingId, FarmerId, Guid.NewGuid(), "Published", 100m)),
            new FakeStockReservationService(succeeds: true),
            new AuditLogService(db), new NotificationService(db));

    private static async Task<Order> SeedOrderAsync(AgriConnectDbContext db)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(), ListingId = ListingId, BuyerId = BuyerId, Quantity = 10m,
            Status = OrderStatus.Approved, DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);
        db.Users.Add(new User
        {
            Id = OfficerUserId, FullName = "Officer Demo", Email = "o@example.test", PasswordHash = "x", Role = Roles.Officer
        });
        await db.SaveChangesAsync();
        return order;
    }

    private static AuditLog Entry(Guid actor, string action, string entityType, Guid entityId, DateTimeOffset at, string? details = null) =>
        new() { Id = Guid.NewGuid(), ActorId = actor, Action = action, EntityType = entityType, EntityId = entityId, Timestamp = at, Details = details };

    [Fact]
    public async Task Activity_ListsOrderAndScheduleEntriesOldestFirst_WithFriendlySummariesAndActorNames()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db);
        var schedule = new PickupSchedule
        {
            Id = Guid.NewGuid(), OrderId = order.Id, CollectionCentreId = Guid.NewGuid(),
            SlotStart = DateTimeOffset.UtcNow.AddDays(1), SlotEnd = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
        };
        db.PickupSchedules.Add(schedule);
        var t0 = DateTimeOffset.UtcNow.AddMinutes(-10);
        db.AuditLogs.AddRange(
            Entry(OfficerUserId, "ScheduleDecision", "PickupSchedule", schedule.Id, t0.AddMinutes(4), "{\"Decision\":\"Approve\"}"),
            Entry(BuyerId, "OrderCreated", "Order", order.Id, t0, "{\"Quantity\":10}"),
            Entry(OfficerUserId, "OrderStatusChanged", "Order", order.Id, t0.AddMinutes(2), "{\"From\":\"Pending\",\"To\":\"Approved\"}"),
            Entry(OfficerUserId, "SchedulePropose", "PickupSchedule", schedule.Id, t0.AddMinutes(3),
                "{\"SlotStart\":\"2026-10-05T20:47:00+00:00\"}"),
            Entry(OfficerUserId, "OrderStatusChanged", "Order", Guid.NewGuid(), t0, "{\"From\":\"Pending\",\"To\":\"Approved\"}")); // another order
        await db.SaveChangesAsync();

        var result = await NewService(db).GetActivityAsync(order.Id, BuyerId, Roles.Buyer);

        Assert.True(result.Success, result.ErrorMessage);
        var items = result.Value!;
        Assert.Equal(
            ["Order placed.", "Order approved.", "Pickup slot proposed for 5 Oct, 20:47 UTC.", "Pickup slot confirmed."],
            items.Select(i => i.Summary));
        Assert.Equal("Unknown user", items[0].ActorName);   // the buyer has no User row in this test
        Assert.Equal("Officer Demo", items[1].ActorName);
    }

    [Fact]
    public async Task Activity_ForTheSystemActor_SaysSystem_AndExplainsAutomaticCancellation()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db);
        db.AuditLogs.Add(Entry(AuditLogService.SystemActorId, "OrderCancelled", "Order", order.Id, DateTimeOffset.UtcNow,
            "{\"Role\":\"System\",\"Reason\":\"Reservation expired\"}"));
        await db.SaveChangesAsync();

        var result = await NewService(db).GetActivityAsync(order.Id, BuyerId, Roles.Buyer);

        var item = Assert.Single(result.Value!);
        Assert.Equal("System", item.ActorName);
        Assert.Contains("reservation expired", item.Summary);
    }

    [Fact]
    public async Task Activity_ForTheMatchEntry_CarriesTheExplanation_ElseTheDeterministicNotes()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db);
        var t0 = DateTimeOffset.UtcNow;
        db.AuditLogs.AddRange(
            Entry(OfficerUserId, "BuyerFarmerMatch", "Order", order.Id, t0,
                "{\"Notes\":\"Matched to A - 2.0 km away.\",\"Explanation\":\"A is the closest centre with free slots.\"}"),
            Entry(OfficerUserId, "BuyerFarmerMatch", "Order", order.Id, t0.AddSeconds(1),
                "{\"Notes\":\"Matched to B - 3.0 km away.\"}"));
        await db.SaveChangesAsync();

        var items = (await NewService(db).GetActivityAsync(order.Id, BuyerId, Roles.Buyer)).Value!;

        Assert.Equal("A is the closest centre with free slots.", items[0].Explanation);
        Assert.Equal("Matched to B - 3.0 km away.", items[1].Explanation);
    }

    [Fact]
    public async Task Activity_NeverExposesRawDetailsOrIds()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db);
        var secretCentre = Guid.NewGuid();
        db.AuditLogs.Add(Entry(OfficerUserId, "SchedulePropose", "Order", order.Id, DateTimeOffset.UtcNow,
            $"{{\"CollectionCentreId\":\"{secretCentre}\",\"Internal\":\"do-not-leak\"}}"));
        await db.SaveChangesAsync();

        var item = Assert.Single((await NewService(db).GetActivityAsync(order.Id, BuyerId, Roles.Buyer)).Value!);

        Assert.DoesNotContain(secretCentre.ToString(), item.Summary);
        Assert.DoesNotContain("do-not-leak", item.Summary);
        Assert.DoesNotContain(item.GetType().GetProperties(), p => p.Name == "Details");
    }

    [Fact]
    public async Task Activity_ForAnotherBuyersOrder_IsNotFound()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db);

        var result = await NewService(db).GetActivityAsync(order.Id, OtherBuyerId, Roles.Buyer);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task Activity_ForAnUnknownOrder_IsNotFound()
    {
        await using var db = NewInMemoryDb();

        var result = await NewService(db).GetActivityAsync(Guid.NewGuid(), BuyerId, Roles.Buyer);

        Assert.Equal(OrderOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task Activity_ForTheListingsFarmer_IsVisible()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db);

        var result = await NewService(db).GetActivityAsync(order.Id, FarmerId, Roles.Farmer);

        Assert.True(result.Success);
    }

    [Fact]
    public void Mapper_UnknownAction_IsHumanised_AndMalformedDetailsAreIgnored()
    {
        var item = OrderActivityMapper.ToItem(
            Entry(Guid.NewGuid(), "SomeNewAction", "Order", Guid.NewGuid(), DateTimeOffset.UtcNow, "{not json"), "X");

        Assert.Equal("Some new action.", item.Summary);
    }
}
