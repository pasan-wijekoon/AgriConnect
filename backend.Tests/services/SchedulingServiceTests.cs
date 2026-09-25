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
    private static readonly Guid ActorId = Guid.NewGuid();

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FakeListingAvailabilityPort DefaultListingPort() =>
        new FakeListingAvailabilityPort().Add(new ListingAvailability(ListingId, FarmerId, RegionId, "Published", 100m));

    private static SchedulingService NewService(
        AgriConnectDbContext db, ILogisticsSchedulingPort? port = null, IListingAvailabilityPort? listingPort = null,
        IBuyerFarmerMatchingPort? matchingPort = null) =>
        new(db, listingPort ?? DefaultListingPort(), port ?? new StubbingLogisticsSchedulingPort(),
            matchingPort ?? new FakeBuyerFarmerMatchingPort(result: null),
            new AuditLogService(db), new NotificationService(db));

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
        var result = await NewService(db).ProposeAsync(Guid.NewGuid(), new CreateScheduleRequest(null, null), ActorId);

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
            order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WithValidRequest_CreatesProposedSchedule()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);

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
            order.Id, new CreateScheduleRequest(null, FutureWindow()), ActorId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(centre.Id, result.Value!.CollectionCentreId);
    }

    // ---- Buyer-Farmer Matching Agent integration (Phase 13) ----

    [Fact]
    public async Task ProposeAsync_WithBuyerLocation_UsesTheAgentsMatchedCentre_NotTheRegionFallback()
    {
        await using var db = NewInMemoryDb();
        var (order, fallbackCentre) = await SeedApprovedOrderWithCentreAsync(db);
        // A second centre in the same region — the agent picks this one, proving
        // the result isn't just coincidentally the region-fallback's own choice.
        var matchedCentre = new CollectionCentre
        {
            Id = Guid.NewGuid(), Name = "Agent-Matched Centre", Latitude = 0, Longitude = 0,
            Capacity = 2, RegionId = RegionId
        };
        db.CollectionCentres.Add(matchedCentre);
        await db.SaveChangesAsync();

        var matchingPort = new FakeBuyerFarmerMatchingPort(
            new MatchResult(matchedCentre.Id, MatchConfidence: 0.9, Notes: "Closest with capacity.",
                CandidatesConsidered: 2, Degraded: false));

        var result = await NewService(db, matchingPort: matchingPort).ProposeAsync(
            order.Id,
            new CreateScheduleRequest(null, FutureWindow(), new BuyerLocationDto(7.29m, 80.63m)),
            ActorId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(matchedCentre.Id, result.Value!.CollectionCentreId);
        Assert.NotEqual(fallbackCentre.Id, result.Value.CollectionCentreId);

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.Action == "BuyerFarmerMatch");
        Assert.Equal(ActorId, auditEntry.ActorId);
        Assert.Equal(order.Id, auditEntry.EntityId);
    }

    [Fact]
    public async Task ProposeAsync_WithBuyerLocation_AgentFindsNoCapacity_ReturnsConflict()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = await SeedApprovedOrderWithCentreAsync(db);
        var matchingPort = new FakeBuyerFarmerMatchingPort(
            new MatchResult(MatchedCentreId: null, MatchConfidence: 0.0,
                Notes: "No candidate collection centre currently has capacity for this order.",
                CandidatesConsidered: 1, Degraded: false));

        var result = await NewService(db, matchingPort: matchingPort).ProposeAsync(
            order.Id,
            new CreateScheduleRequest(null, FutureWindow(), new BuyerLocationDto(7.29m, 80.63m)),
            ActorId);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.Conflict, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WithBuyerLocation_AgentCallFails_FallsBackToRegionMatchedCentre()
    {
        await using var db = NewInMemoryDb();
        var (order, fallbackCentre) = await SeedApprovedOrderWithCentreAsync(db);
        // null MatchResult simulates the agent being unreachable/timing out,
        // distinct from a real "no capacity" MatchResult with a null centre id.
        var matchingPort = new FakeBuyerFarmerMatchingPort(result: null);

        var result = await NewService(db, matchingPort: matchingPort).ProposeAsync(
            order.Id,
            new CreateScheduleRequest(null, FutureWindow(), new BuyerLocationDto(7.29m, 80.63m)),
            ActorId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(fallbackCentre.Id, result.Value!.CollectionCentreId);
        Assert.DoesNotContain(await db.AuditLogs.ToListAsync(), a => a.Action == "BuyerFarmerMatch");
    }

    [Fact]
    public async Task ProposeAsync_WithoutBuyerLocation_NeverCallsTheMatchingAgent()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var matchingPort = new FakeBuyerFarmerMatchingPort(result: null);

        var result = await NewService(db, matchingPort: matchingPort).ProposeAsync(
            order.Id, new CreateScheduleRequest(null, FutureWindow()), ActorId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(centre.Id, result.Value!.CollectionCentreId);
        Assert.Null(matchingPort.LastOrderIdPassedIn);
    }

    [Fact]
    public async Task ProposeAsync_WithoutPreferredWindow_DefaultsToTomorrow()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, null), ActorId);

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
            order.Id, new CreateScheduleRequest(centre.Id, new ScheduleWindowDto(past, past.AddHours(1))), ActorId);

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
            order.Id, new CreateScheduleRequest(centre.Id, new ScheduleWindowDto(start, start.AddHours(-1))), ActorId);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_WithUnknownCentreId_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(Guid.NewGuid(), FutureWindow()), ActorId);

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
            order.Id, new CreateScheduleRequest(centre.Id, window), ActorId);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.Conflict, result.Error);
    }

    [Fact]
    public async Task ProposeAsync_CalledTwice_UpdatesTheSameScheduleRow_NotADuplicate()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var first = await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow(1)), ActorId);
        var second = await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow(2)), ActorId);

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
        var proposed = await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);
        Assert.True(proposed.Success);

        var result = await NewService(db).DecideAsync(order.Id, ScheduleDecision.Approve, ActorId);

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
        await NewService(db).ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);

        var result = await NewService(db).DecideAsync(order.Id, ScheduleDecision.Reject, ActorId);

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

        var result = await NewService(db).DecideAsync(order.Id, ScheduleDecision.Approve, ActorId);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WithUnknownOrder_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();

        var result = await NewService(db).GetByOrderIdAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WithNoScheduleYet_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var (order, _) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).GetByOrderIdAsync(order.Id);

        Assert.False(result.Success);
        Assert.Equal(SchedulingOperationError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetByOrderIdAsync_AfterProposing_ReturnsTheSchedule()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var service = NewService(db);
        var proposed = await service.ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);

        var result = await service.GetByOrderIdAsync(order.Id);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(proposed.Value!.Id, result.Value!.Id);
        Assert.Equal(ScheduleStatus.Proposed, result.Value.Status);
    }

    [Fact]
    public async Task ProposeAsync_AfterConfirmed_CannotRePropose()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var service = NewService(db);
        await service.ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);
        await service.DecideAsync(order.Id, ScheduleDecision.Approve, ActorId);

        var result = await service.ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow(2)), ActorId);

        Assert.False(result.Success);
        // Order is now Scheduled, not Approved, so this is rejected as InvalidRequest
        // (wrong order status) before it ever reaches the "already Confirmed" check.
        Assert.Equal(SchedulingOperationError.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task ListByCentreAsync_ReturnsOnlySchedulesAtThatCentre_OrderedBySlotStart()
    {
        await using var db = NewInMemoryDb();
        var (order1, centre1) = await SeedApprovedOrderWithCentreAsync(db);
        var (order2, _) = await SeedApprovedOrderWithCentreAsync(db); // second order, own centre we won't use
        var (order3, centre3) = await SeedApprovedOrderWithCentreAsync(db); // schedule stays at its own centre
        var service = NewService(db);

        var laterAtCentre1 = await service.ProposeAsync(order1.Id, new CreateScheduleRequest(centre1.Id, FutureWindow(3)), ActorId);
        var earlierAtCentre1 = await service.ProposeAsync(order2.Id, new CreateScheduleRequest(centre1.Id, FutureWindow(1)), ActorId);
        await service.ProposeAsync(order3.Id, new CreateScheduleRequest(centre3.Id, FutureWindow(2)), ActorId);

        var results = await service.ListByCentreAsync(centre1.Id);

        Assert.Equal(2, results.Count);
        Assert.Equal(earlierAtCentre1.Value!.Id, results[0].Id); // earlier slot first
        Assert.Equal(laterAtCentre1.Value!.Id, results[1].Id);
        Assert.All(results, r => Assert.Equal(centre1.Id, r.CollectionCentreId));
    }

    [Fact]
    public async Task ListByCentreAsync_WithNoSchedules_ReturnsEmpty()
    {
        await using var db = NewInMemoryDb();
        var (_, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var results = await NewService(db).ListByCentreAsync(centre.Id);

        Assert.Empty(results);
    }

    // ---- Audit & Notifications (FR20/FR22, plan §12/Phase 11) ----

    [Fact]
    public async Task ProposeAsync_WritesAuditLogAndNotifiesBuyerAndFarmer()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);

        var result = await NewService(db).ProposeAsync(
            order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);
        Assert.True(result.Success);

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal(ActorId, entry.ActorId);
        Assert.Equal("SchedulePropose", entry.Action);
        Assert.Equal(result.Value!.Id, entry.EntityId);

        var notifiedUsers = (await db.Notifications.ToListAsync()).Select(n => n.UserId).ToList();
        Assert.Contains(order.BuyerId, notifiedUsers);
        Assert.Contains(FarmerId, notifiedUsers);
    }

    [Fact]
    public async Task DecideAsync_WritesAuditLogWithActorAndDecision()
    {
        await using var db = NewInMemoryDb();
        var (order, centre) = await SeedApprovedOrderWithCentreAsync(db);
        var service = NewService(db);
        await service.ProposeAsync(order.Id, new CreateScheduleRequest(centre.Id, FutureWindow()), ActorId);

        var result = await service.DecideAsync(order.Id, ScheduleDecision.Approve, ActorId);
        Assert.True(result.Success);

        // Propose already wrote its own "SchedulePropose" entry, so filter by
        // action rather than asserting SingleAsync.
        var entry = await db.AuditLogs.SingleAsync(a => a.Action == "ScheduleDecision");
        Assert.Equal(ActorId, entry.ActorId);
        Assert.Equal("ScheduleDecision", entry.Action);
        Assert.Contains("Approve", entry.Details);
    }
}
