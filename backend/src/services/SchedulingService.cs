using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

public enum SchedulingOperationError
{
    None,
    NotFound,
    InvalidRequest,
    Conflict
}

public class SchedulingOperationResult<T>
{
    public bool Success { get; private init; }
    public T? Value { get; private init; }
    public SchedulingOperationError Error { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static SchedulingOperationResult<T> Ok(T value) => new() { Success = true, Value = value };

    public static SchedulingOperationResult<T> Fail(SchedulingOperationError error, string message) =>
        new() { Success = false, Error = error, ErrorMessage = message };
}

/// <summary>
/// FR10 — proposes and confirms pickup/delivery slots. Persists proposals via
/// <see cref="ILogisticsSchedulingPort"/> (Student 4's agent, stubbed for now) but
/// owns and enforces every validation rule in plan §8.3 itself, and never moves a
/// PickupSchedule to Confirmed without an explicit Officer decision (FR19,
/// CLAUDE.md §17/§18 — no auto-confirm path, ever).
///
/// Component B — Order &amp; Collection-Centre Logistics.
/// </summary>
public class SchedulingService(
    AgriConnectDbContext db,
    IListingAvailabilityPort listingPort,
    ILogisticsSchedulingPort schedulingPort)
{
    public async Task<SchedulingOperationResult<ScheduleResponse>> ProposeAsync(
        Guid orderId, CreateScheduleRequest request, CancellationToken ct = default)
    {
        var order = await db.Orders
            .Include(o => o.PickupSchedule)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order not found.");
        }

        // API table (plan §5.2): "order must be Approved". Also covers the §8.3
        // rule "Order is not already Cancelled/Completed" — Approved is the only
        // legal state to schedule from.
        if (order.Status != OrderStatus.Approved)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.InvalidRequest,
                $"Order must be Approved before scheduling (current status: {order.Status}).");
        }

        if (order.PickupSchedule is { Status: ScheduleStatus.Confirmed })
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.Conflict, "Order already has a confirmed schedule.");
        }

        var centreId = request.CollectionCentreId;
        if (centreId is null)
        {
            // No centreId supplied: fall back to the first centre in the listing's
            // own region. This is a stand-in for the Buyer-Farmer Matching Agent
            // (plan §8.1 step 1, Phase 8, not yet built) — the real agent will pick
            // a matched centre by distance/confidence; this fallback just needs a
            // valid, region-appropriate centre so Phase 6 isn't blocked on Phase 8.
            var listing = await listingPort.GetAvailabilityAsync(order.ListingId, ct);
            if (listing is null)
            {
                return SchedulingOperationResult<ScheduleResponse>.Fail(
                    SchedulingOperationError.NotFound, "Listing not found.");
            }

            var fallbackCentre = await db.CollectionCentres
                .Where(c => c.RegionId == listing.RegionId)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(ct);

            if (fallbackCentre is null)
            {
                return SchedulingOperationResult<ScheduleResponse>.Fail(
                    SchedulingOperationError.InvalidRequest,
                    "No collection centre available in the listing's region; specify collectionCentreId explicitly.");
            }

            centreId = fallbackCentre.Id;
        }

        var centre = await db.CollectionCentres.FirstOrDefaultAsync(c => c.Id == centreId, ct);
        if (centre is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Collection centre not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var preferredWindow = request.PreferredWindow is { } w
            ? new SchedulingWindow(w.Start, w.End)
            : new SchedulingWindow(now.AddDays(1), now.AddDays(1).AddHours(1));

        if (preferredWindow.End <= preferredWindow.Start)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.InvalidRequest, "SlotEnd must be after SlotStart.");
        }

        if (preferredWindow.Start < now)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.InvalidRequest, "Proposed window cannot be in the past.");
        }

        var existingBookings = await db.PickupSchedules
            .Where(p => p.CollectionCentreId == centreId
                        && p.Status == ScheduleStatus.Confirmed
                        && p.OrderId != orderId)
            .Select(p => new ExistingBooking(p.SlotStart, p.SlotEnd))
            .ToListAsync(ct);

        var proposal = await schedulingPort.ProposeSlotAsync(
            orderId, centreId.Value, preferredWindow, existingBookings, ct);

        // Capacity check owned by Component B (plan §8.3): count Confirmed bookings
        // whose windows truly overlap the proposed one, not just an exact-match
        // check — a centre with Capacity > 1 can hold several concurrent bookings.
        var overlappingCount = existingBookings.Count(
            b => b.Start < proposal.ProposedSlotEnd && b.End > proposal.ProposedSlotStart);

        if (overlappingCount >= centre.Capacity)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.Conflict,
                $"Collection centre has no capacity in the proposed window ({overlappingCount}/{centre.Capacity} slots already booked).");
        }

        PickupSchedule schedule;
        if (order.PickupSchedule is not null)
        {
            // Upsert: PickupSchedule is 1-1 with Order (unique index on OrderId), so
            // a Proposed or Cancelled schedule from an earlier attempt is updated in
            // place rather than inserted again. This is exactly what "Revise ->
            // re-invoke step 1/2 with adjusted preferredWindow" (plan §8.1) means in
            // practice: calling this endpoint again with a new preferredWindow.
            schedule = order.PickupSchedule;
            schedule.CollectionCentreId = centreId.Value;
            schedule.SlotStart = proposal.ProposedSlotStart;
            schedule.SlotEnd = proposal.ProposedSlotEnd;
            schedule.Status = ScheduleStatus.Proposed;
        }
        else
        {
            schedule = new PickupSchedule
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                CollectionCentreId = centreId.Value,
                SlotStart = proposal.ProposedSlotStart,
                SlotEnd = proposal.ProposedSlotEnd,
                Status = ScheduleStatus.Proposed
            };
            db.PickupSchedules.Add(schedule);
        }

        await db.SaveChangesAsync(ct);

        return SchedulingOperationResult<ScheduleResponse>.Ok(ToResponse(schedule, proposal.ConflictChecked));
    }

    /// <summary>
    /// Read-only lookup of an order's current schedule, if one exists. Added so
    /// a UI can display schedule state (Design.md §31/§34) without needing to
    /// trigger ProposeAsync's side effects just to see what's already there.
    /// </summary>
    public async Task<SchedulingOperationResult<ScheduleResponse>> GetByOrderIdAsync(
        Guid orderId, CancellationToken ct = default)
    {
        var order = await db.Orders
            .Include(o => o.PickupSchedule)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order not found.");
        }

        if (order.PickupSchedule is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order has no schedule yet.");
        }

        // ConflictChecked isn't a persisted field — it's the scheduling agent's
        // informational flag from the moment of proposal (plan §8.2), not part
        // of PickupSchedule's own columns (plan §4.1). Reporting `true` here
        // reflects that this endpoint returns the schedule as it currently
        // stands (already validated by SchedulingService at propose time),
        // not a claim about a specific past agent response.
        return SchedulingOperationResult<ScheduleResponse>.Ok(ToResponse(order.PickupSchedule, conflictChecked: true));
    }

    /// <summary>
    /// All schedules (any status) at a centre, ordered by slot start. Backs the
    /// Officer scheduling calendar (Design.md §33 — "centre bookings calendar
    /// (Proposed/Confirmed/Cancelled), capacity indicator, conflict
    /// highlighting"), which needs every booking at a centre, not one order's.
    /// </summary>
    public async Task<IReadOnlyList<ScheduleResponse>> ListByCentreAsync(
        Guid centreId, CancellationToken ct = default)
    {
        var schedules = await db.PickupSchedules
            .Where(p => p.CollectionCentreId == centreId)
            .OrderBy(p => p.SlotStart)
            .AsNoTracking()
            .ToListAsync(ct);

        return schedules.Select(s => ToResponse(s, conflictChecked: true)).ToList();
    }

    public async Task<SchedulingOperationResult<ScheduleResponse>> DecideAsync(
        Guid orderId, ScheduleDecision decision, CancellationToken ct = default)
    {
        var order = await db.Orders
            .Include(o => o.PickupSchedule)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order not found.");
        }

        if (order.PickupSchedule is not { Status: ScheduleStatus.Proposed } schedule)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.InvalidRequest, "Order has no schedule proposal awaiting a decision.");
        }

        if (decision == ScheduleDecision.Approve)
        {
            schedule.Status = ScheduleStatus.Confirmed;
            order.Status = OrderStatus.Scheduled;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            schedule.Status = ScheduleStatus.Cancelled;
            // Order deliberately stays Approved (plan §8.1) — rejecting a proposal
            // does not cancel the order, it just clears the way to propose again.
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Defense in depth against the DB's partial unique index: another
            // order's schedule could have been confirmed for the identical
            // (centre, slot) window between this proposal and this decision.
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.Conflict,
                "This slot was booked by another order in the meantime; propose a new window.");
        }

        return SchedulingOperationResult<ScheduleResponse>.Ok(ToResponse(schedule, conflictChecked: true));
    }

    private static ScheduleResponse ToResponse(PickupSchedule schedule, bool conflictChecked) => new(
        schedule.Id,
        schedule.OrderId,
        schedule.CollectionCentreId,
        schedule.SlotStart,
        schedule.SlotEnd,
        schedule.Status,
        conflictChecked);
}
