using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

internal class StubbingLogisticsSchedulingPort(bool reportConflict = false) : ILogisticsSchedulingPort
{
    public Task<SchedulingProposal> ProposeSlotAsync(
        Guid orderId, Guid centreId, SchedulingWindow preferredWindow,
        IReadOnlyList<ExistingBooking> existingBookings, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchedulingProposal(preferredWindow.Start, preferredWindow.End, ConflictChecked: !reportConflict));
}

public class SchedulingServiceTests
{
    private static readonly Guid RegionId = Guid.NewGuid();
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly Guid FarmerId = Guid.NewGuid();

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FakeListingAvailabilityPort DefaultListingPort() =>
        new FakeListingAvailabilityPort().Add(new ListingAvailability(ListingId, FarmerId, RegionId, "Published", 100m));

    private static SchedulingService NewService(
        AgriConnectDbContext db, ILogisticsSchedulingPort? port = null, IListingAvailabilityPort? listingPort = null) =>
        new(db, listingPort ?? DefaultListingPort(), port ?? new StubbingLogisticsSchedulingPort());

    private static async Task<(Order Order, CollectionCentre Centre)> SeedApprovedOrderWithCentreAsync(
        AgriConnectDbContext db, int capacity = 2)
    {
        var centre = new CollectionCentre
        {
            Id = Guid.NewGuid(),
            Name = "Test Centre",
            Latitude = 0,
            Longitude = 0,
            Capacity = capacity,
            RegionId = RegionId
        };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ListingId = ListingId,
            BuyerId = Guid.NewGuid(),
            Quantity = 10m,
            Status = OrderStatus.Approved,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.CollectionCentres.Add(centre);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return (order, centre);
    }

    private static ScheduleWindowDto FutureWindow(int daysFromNow = 1, int durationHours = 1)
    {
        var start = DateTimeOffset.UtcNow.AddDays(daysFromNow);
        return new ScheduleWindowDto(start, start.AddHours(durationHours));
    }

    [Fact]
    public async Task ProposeAsync_WithUnknownOrder_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var result = await NewService(db).ProposeAsync(Guid.NewGuid(), new CreateScheduleRequest(null, null));

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.NotFound, result.Error);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Scheduled)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task ProposeAsync_WhenOrderIsNotApproved_ReturnsInvalidRequest(OrderStatus status)
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        order.Status = status;
        await db.SaveChangesAsync();

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()));

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WithValidRequest_CreatesProposedSchedule()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()));

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(ScheduleStatus.Proposed, result.Value!.Status);
        Assert.Equal(centre.Id, result.Value.CollectionCentreId);
        Assert.True(result.Value.ConflictChecked);
    }

    [Fact]
    public async Task ProposeAsync_WithoutCentreId_FallsBackToRegionMatchedCentre()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(null, FutureWindow()));

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(centre.Id, result.Value!.CollectionCentreId);
    }

    [Fact]
    public async Task ProposeAsync_WithoutPreferredWindow_DefaultsToTomorrow()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, null));

        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(result.Value!.SlotStart > DateTimeOffset.UtcNow);
        Assert.True(result.Value.SlotEnd > result.Value.SlotStart);
    }

    [Fact]
    public async Task ProposeAsync_WithWindowInThePast_ReturnsInvalidRequest()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var past = DateTimeOffset.UtcNow.AddDays(-1);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, new ScheduleWindowDto(past, past.AddHours(1))));

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WithSlotEndBeforeSlotStart_ReturnsInvalidRequest()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, new ScheduleWindowDto(start, start.AddHours(-1))));

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WithUnknownCentreId_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(Guid.NewGuid(), FutureWindow()));

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WhenCentreAtCapacity_ReturnsConflict()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db, capacity: 1);
        var window = FutureWindow();

        // A different, already-Confirmed booking occupies the centre's only slot.
        var otherOrder = new Order
        {
            Id = Guid.NewGuid(),
            ListingId = ListingId,
            BuyerId = Guid.NewGuid(),
            Quantity = 5m,
            Status = OrderStatus.Scheduled,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Orders.Add(otherOrder);
        db.PickupSchedules.Add(new PickupSchedule
        {
            Id = Guid.NewGuid(),
            OrderId = otherOrder.Id,
            CollectionCentreId = centre.Id,
            SlotStart = window.Start,
            SlotEnd = window.End,
            Status = ScheduleStatus.Confirmed
        });
        await db.SaveChangesAsync();

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, window));

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.Conflict, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_CalledTwice_UpdatesTheSameScheduleRow_NotADuplicate()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var first = await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow(1)));
        var second = await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow(2)));

        Assert.True(first.Success);
        Assert.True(second.Success, second.ErrorMessage);
        Assert.Equal(first.Value!.Id, second.Value!.Id); // same row, revised in place
        Assert.Equal(1, await db.PickupSchedules.CountAsync(p => p.OrderId == order.Id));
    }

    [Fact]
    public async Task DecideAsync_Approve_ConfirmsScheduleAndMarksOrderScheduled()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var proposed = await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()));
        Assert.True(proposed.Success);

        var result = await NewService(db).DecideAsync(order.Id, ScheduleDecision.Approve);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(ScheduleStatus.Confirmed, result.Value!.Status);
        var reloadedOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Scheduled, reloadedOrder.Status);
    }

    [Fact]
    public async Task DecideAsync_Reject_CancelsScheduleButOrderStaysApproved()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()));

        var result = await NewService(db).DecideAsync(order.Id, ScheduleDecision.Reject);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(ScheduleStatus.Cancelled, result.Value!.Status);
        var reloadedOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Approved, reloadedOrder.Status);
    }

    [Fact]
    public async Task DecideAsync_WithNoProposalPending_ReturnsInvalidRequest()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).DecideAsync(order.Id, ScheduleDecision.Approve);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_AfterConfirmed_CannotRePropose()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var service = NewService(db);
        await service.ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()));
        await service.DecideAsync(order.Id, ScheduleDecision.Approve);

        var result = await service.ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow(2)));

        Assert.False(result.Success);
        // Order is now Scheduled, not Approved, so this is rejected as InvalidRequest
        // (wrong order status) before it ever reaches the "already Confirmed" check.
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }
}
