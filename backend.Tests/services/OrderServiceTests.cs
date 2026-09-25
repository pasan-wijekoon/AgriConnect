using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// Unit tests for OrderService's own logic (validation, status-transition
/// allow-list, role-scoped visibility, cancellation rules) using the EF Core
/// InMemory provider and fake IStockReservationService/IListingAvailabilityPort.
/// The concurrency-critical reservation algorithm itself is tested separately,
/// against real Postgres, in StockReservationServiceConcurrencyTests.
/// </summary>
public class OrderServiceTests
{
    private static readonly Guid PublishedListingId = Guid.NewGuid();
    private static readonly Guid FarmerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();
    private static readonly Guid OtherBuyerId = Guid.NewGuid();
    private static readonly Guid OfficerId = Guid.NewGuid();

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FakeListingAvailabilityPort DefaultListingPort() =>
        new FakeListingAvailabilityPort().Add(new ListingAvailability(
            PublishedListingId, FarmerId, Guid.NewGuid(), "Published", 100m));

    private static OrderService NewService(
        AgriConnectDbContext db, IListingAvailabilityPort? port = null, IStockReservationService? reservation = null) =>
        new(db, port ?? DefaultListingPort(), reservation ?? new FakeStockReservationService(succeeds: true),
            new AuditLogService(db), new NotificationService(db));

    private static async Task<Order> SeedOrderAsync(AgriConnectDbContext db, OrderStatus status, Guid? buyerId = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ListingId = PublishedListingId,
            BuyerId = buyerId ?? BuyerId,
            Quantity = 10m,
            Status = status,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    // ---- PlaceOrderAsync ----

    [Fact]
    public async Task PlaceOrderAsync_WithNonPositiveQuantity_ReturnsInvalidRequest()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db).PlaceOrderAsync(
            BuyerId, new CreateOrderRequest(PublishedListingId, 0m, DeliveryPreference.Pickup));

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithUnknownListing_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db, new FakeListingAvailabilityPort()).PlaceOrderAsync(
            BuyerId, new CreateOrderRequest(Guid.NewGuid(), 10m, DeliveryPreference.Pickup));

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithUnpublishedListing_ReturnsInvalidRequest()
    {
        var unpublished = Guid.NewGuid();
        var port = new FakeListingAvailabilityPort()
            .Add(new ListingAvailability(unpublished, FarmerId, Guid.NewGuid(), "Draft", 100m));

        await using var db = NewInMemoryDb();
        var result = await NewService(db, port).PlaceOrderAsync(
            BuyerId, new CreateOrderRequest(unpublished, 10m, DeliveryPreference.Pickup));

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenReservationFails_ReturnsConflict()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db, reservation: new FakeStockReservationService(succeeds: false))
            .PlaceOrderAsync(BuyerId, new CreateOrderRequest(PublishedListingId, 10m, DeliveryPreference.Pickup));

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.Conflict, result.Error);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithValidRequest_ReturnsPendingOrder()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db).PlaceOrderAsync(
            BuyerId, new CreateOrderRequest(PublishedListingId, 10m, DeliveryPreference.Delivery));

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Pending, result.Value!.Status);
        Assert.Equal(BuyerId, result.Value.BuyerId);
        Assert.Equal(DeliveryPreference.Delivery, result.Value.DeliveryPreference);
        Assert.NotNull(result.Value.ReservationExpiresAt);
    }

    // ---- UpdateStatusAsync (transition allow-list) ----

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Approved, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Approved, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Scheduled, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Scheduled, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Approved, OrderStatus.Scheduled, false)] // driven by SchedulingService, not this endpoint
    [InlineData(OrderStatus.Approved, OrderStatus.Completed, false)]
    [InlineData(OrderStatus.Completed, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Pending, OrderStatus.Scheduled, false)]
    public async Task UpdateStatusAsync_EnforcesAllowList(OrderStatus from, OrderStatus to, bool shouldSucceed)
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, from);

        var result = await NewService(db).UpdateStatusAsync(order.Id, to, OfficerId);

        Assert.Equal(shouldSucceed, result.Success);
        if (!shouldSucceed)
        {
            Assert.Equal(OrderOperationError.InvalidRequest, result.Error);
        }
    }

    [Fact]
    public async Task UpdateStatusAsync_WithUnknownOrderId_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db).UpdateStatusAsync(Guid.NewGuid(), OrderStatus.Approved, OfficerId);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.NotFound, result.Error);
    }

    // ---- CancelAsync ----

    [Fact]
    public async Task CancelAsync_BuyerCancelsOwnPendingOrder_Succeeds()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).CancelAsync(order.Id, BuyerId, Roles.Buyer, reason: null);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, result.Value!.Status);
    }

    [Fact]
    public async Task CancelAsync_BuyerCancelsAnotherBuyersOrder_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending, buyerId: OtherBuyerId);

        var result = await NewService(db).CancelAsync(order.Id, BuyerId, Roles.Buyer, reason: null);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.NotFound, result.Error); // IDOR: 404, not 403
    }

    [Fact]
    public async Task CancelAsync_BuyerCancelsScheduledOrder_ReturnsConflict()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Scheduled);

        var result = await NewService(db).CancelAsync(order.Id, BuyerId, Roles.Buyer, reason: null);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.Conflict, result.Error);
    }

    [Fact]
    public async Task CancelAsync_OfficerCancelsScheduledOrder_Succeeds()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Scheduled);

        var result = await NewService(db).CancelAsync(order.Id, OfficerId, Roles.Officer, reason: null);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CancelAsync_OfficerCancelsCompletedOrder_ReturnsConflict()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Completed);

        var result = await NewService(db).CancelAsync(order.Id, OfficerId, Roles.Officer, reason: null);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.Conflict, result.Error);
    }

    // ---- GetByIdAsync (role-scoped visibility / IDOR) ----

    [Fact]
    public async Task GetByIdAsync_BuyerViewsOwnOrder_Succeeds()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).GetByIdAsync(order.Id, BuyerId, Roles.Buyer);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetByIdAsync_BuyerViewsAnotherBuyersOrder_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending, buyerId: OtherBuyerId);

        var result = await NewService(db).GetByIdAsync(order.Id, BuyerId, Roles.Buyer);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_FarmerWhoOwnsTheListing_Succeeds()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).GetByIdAsync(order.Id, FarmerId, Roles.Farmer);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetByIdAsync_FarmerWhoDoesNotOwnTheListing_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).GetByIdAsync(order.Id, Guid.NewGuid(), Roles.Farmer);

        Assert.False(result.Success);
        Assert.Equal(OrderOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_Officer_CanViewAnyOrder()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).GetByIdAsync(order.Id, OfficerId, Roles.Officer);

        Assert.True(result.Success);
    }

    // ---- ListAsync (role-scoped filtering) ----

    [Fact]
    public async Task ListAsync_Buyer_OnlySeesOwnOrders()
    {
        await using var db = NewInMemoryDb();
        await SeedOrderAsync(db, OrderStatus.Pending, buyerId: BuyerId);
        await SeedOrderAsync(db, OrderStatus.Pending, buyerId: OtherBuyerId);

        var result = await NewService(db).ListAsync(BuyerId, Roles.Buyer, status: null, page: 1, size: 20);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, o => Assert.Equal(BuyerId, o.BuyerId));
    }

    [Fact]
    public async Task ListAsync_Officer_SeesAllOrders()
    {
        await using var db = NewInMemoryDb();
        await SeedOrderAsync(db, OrderStatus.Pending, buyerId: BuyerId);
        await SeedOrderAsync(db, OrderStatus.Pending, buyerId: OtherBuyerId);

        var result = await NewService(db).ListAsync(OfficerId, Roles.Officer, status: null, page: 1, size: 20);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_SizeIsClampedTo100()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db).ListAsync(OfficerId, Roles.Officer, status: null, page: 1, size: 500);

        Assert.Equal(100, result.Size);
    }

    // ---- Audit & Notifications (FR20/FR22, plan §12/Phase 11) ----

    [Fact]
    public async Task PlaceOrderAsync_WritesAuditLogAndNotifiesBuyer()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db).PlaceOrderAsync(
            BuyerId, new CreateOrderRequest(PublishedListingId, 10m, DeliveryPreference.Pickup));

        Assert.True(result.Success);

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal(BuyerId, entry.ActorId);
        Assert.Equal("OrderCreated", entry.Action);
        Assert.Equal("Order", entry.EntityType);
        Assert.Equal(result.Value!.Id, entry.EntityId);

        var notification = await db.Notifications.SingleAsync();
        Assert.Equal(BuyerId, notification.UserId);
        Assert.Equal("OrderPlaced", notification.Type);
    }

    [Fact]
    public async Task UpdateStatusAsync_WritesAuditLogWithActorAndNotifiesBuyerAndFarmer()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).UpdateStatusAsync(order.Id, OrderStatus.Approved, OfficerId);
        Assert.True(result.Success);

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal(OfficerId, entry.ActorId);
        Assert.Equal("OrderStatusChanged", entry.Action);
        Assert.Contains("Approved", entry.Details);

        // Buyer (order.BuyerId) and the listing's farmer (FarmerId, from
        // DefaultListingPort) both track order status per FR11.
        var notifiedUsers = (await db.Notifications.ToListAsync()).Select(n => n.UserId).ToList();
        Assert.Contains(BuyerId, notifiedUsers);
        Assert.Contains(FarmerId, notifiedUsers);
    }

    [Fact]
    public async Task CancelAsync_PersistsReasonInAuditLogDetails()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrderAsync(db, OrderStatus.Pending);

        var result = await NewService(db).CancelAsync(order.Id, BuyerId, Roles.Buyer, reason: "Changed my mind");
        Assert.True(result.Success);

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal("OrderCancelled", entry.Action);
        Assert.Contains("Changed my mind", entry.Details);
    }
}
