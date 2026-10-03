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
    ILogisticsSchedulingPort schedulingPort,
    IBuyerFarmerMatchingPort matchingPort,
    AuditLogService auditLog,
    NotificationService notifications)
{
    public async Task<SchedulingOperationResult<ScheduleResponse>> ProposeAsync(
        Guid orderId, CreateScheduleRequest request, Guid actorId, CancellationToken ct = default)
    {
        var order = await db.Orders
            .Include(o => o.PickupSchedule)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order not found.");
        }

        if (!await (await OfficerScope.ForAsync(db, actorId, ct)).CanAccessAsync(db, orderId, ct))
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

        var now = DateTimeOffset.UtcNow;
        var preferredWindow = request.PreferredWindow is { } w
            ? new SchedulingWindow(w.Start, w.End)
            : new SchedulingWindow(now.AddDays(1), now.AddDays(1).AddHours(1));

        var centreId = request.CollectionCentreId;
        if (centreId is null)
        {
            var listing = await listingPort.GetAvailabilityAsync(order.ListingId, ct);
            if (listing is null)
            {
                return SchedulingOperationResult<ScheduleResponse>.Fail(
                    SchedulingOperationError.NotFound, "Listing not found.");
            }

            var regionCentres = await db.CollectionCentres
                .Where(c => c.RegionId == listing.RegionId)
                .ToListAsync(ct);

            if (regionCentres.Count == 0)
            {
                // No centre serves the listing's region: fall back to every centre so
                // the matching agent can pick the nearest one (FR21). Without buyer
                // coordinates there is nothing to rank by, so an explicit centre is needed.
                regionCentres = await db.CollectionCentres.ToListAsync(ct);
                if (regionCentres.Count == 0 || request.BuyerLocation is null)
                {
                    return SchedulingOperationResult<ScheduleResponse>.Fail(
                        SchedulingOperationError.InvalidRequest,
                        "No collection centre available in the listing's region; specify collectionCentreId explicitly.");
                }
            }

            Guid? matchedCentreId = null;

            // The Buyer-Farmer Matching Agent (plan §8.1/Phase 8) is only tried
            // when the caller supplies buyer coordinates — nothing in this
            // codebase stores a real buyer location yet (no shared User/Address
            // model, plan §16 open question #1), so there is currently no way to
            // source them automatically. When omitted, or when the agent call
            // itself doesn't succeed, this falls back to the original
            // first-centre-in-region selection exactly as before.
            if (request.BuyerLocation is { } buyerLocation)
            {
                var candidates = new List<MatchCandidateCentre>(regionCentres.Count);
                foreach (var c in regionCentres)
                {
                    // Concurrent Confirmed bookings overlapping the requested window (plan §8.3) —
                    // not every Confirmed booking the centre has ever had, which would mark a
                    // centre permanently "full" once it had handled Capacity orders in total.
                    var confirmedCount = await db.PickupSchedules.CountAsync(
                        p => p.CollectionCentreId == c.Id
                             && p.Status == ScheduleStatus.Confirmed
                             && p.OrderId != orderId
                             && p.SlotStart < preferredWindow.End
                             && p.SlotEnd > preferredWindow.Start, ct);
                    candidates.Add(new MatchCandidateCentre(c.Id, c.Name, c.Latitude, c.Longitude, c.Capacity, confirmedCount));
                }

                var match = await matchingPort.MatchAsync(
                    orderId, buyerLocation.Lat, buyerLocation.Lng, order.ListingId, order.Quantity, candidates, ct);

                if (match is not null)
                {
                    if (match.MatchedCentreId is null)
                    {
                        // A real, meaningful "no match" from the agent — distinct
                        // from the agent being unreachable — is not silently
                        // papered over with the region fallback.
                        return SchedulingOperationResult<ScheduleResponse>.Fail(
                            SchedulingOperationError.Conflict,
                            $"The Buyer-Farmer Matching Agent found no collection centre with capacity for this order ({match.Notes}); specify collectionCentreId explicitly.");
                    }

                    matchedCentreId = match.MatchedCentreId;
                    auditLog.Log(actorId, "BuyerFarmerMatch", "Order", orderId,
                        new { match.MatchedCentreId, match.MatchConfidence, match.Notes, match.Degraded });
                }
                // match is null: the agent call itself didn't succeed — fall
                // through to the region-based fallback below, same as if no
                // buyer location had been supplied at all.
            }

            centreId = matchedCentreId ?? regionCentres.OrderBy(c => c.Name).First().Id;
        }

        var centre = await db.CollectionCentres.FirstOrDefaultAsync(c => c.Id == centreId, ct);
        if (centre is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Collection centre not found.");
        }

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

        auditLog.Log(actorId, "SchedulePropose", "PickupSchedule", schedule.Id,
            new { schedule.CollectionCentreId, schedule.SlotStart, schedule.SlotEnd });
        await NotifyOrderStakeholdersAsync(
            order,
            $"A pickup slot ({schedule.SlotStart:d MMM, HH:mm} UTC) has been proposed for your order at {centre.Name}. It is final once an officer confirms it.",
            ct);

        await db.SaveChangesAsync(ct);

        return SchedulingOperationResult<ScheduleResponse>.Ok(ToResponse(schedule, proposal.ConflictChecked));
    }

    /// <summary>Same posture as OrderService's identically-named helper: best-effort,
    /// never blocks the scheduling operation it accompanies.</summary>
    private async Task NotifyOrderStakeholdersAsync(Order order, string message, CancellationToken ct)
    {
        notifications.Notify(order.BuyerId, "ScheduleUpdate", message);

        var listing = await listingPort.GetAvailabilityAsync(order.ListingId, ct);
        if (listing is not null)
        {
            notifications.Notify(listing.FarmerId, "ScheduleUpdate", message);
        }
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
        Guid orderId, ScheduleDecision decision, Guid actorId, CancellationToken ct = default,
        string? revisionReason = null, ScheduleWindowDto? revisionWindow = null)
    {
        var order = await db.Orders
            .Include(o => o.PickupSchedule)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order not found.");
        }

        if (!await (await OfficerScope.ForAsync(db, actorId, ct)).CanAccessAsync(db, orderId, ct))
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.NotFound, "Order not found.");
        }

        if (order.PickupSchedule is not { Status: ScheduleStatus.Proposed } schedule)
        {
            return SchedulingOperationResult<ScheduleResponse>.Fail(
                SchedulingOperationError.InvalidRequest, "Order has no schedule proposal awaiting a decision.");
        }

        if (decision == ScheduleDecision.RequestRevision)
        {
            // FR19 "Request Revision": the Officer sends the proposal back for a new
            // window (their own, or the next day at the same time) at the same centre.
            // Re-proposing upserts the same PickupSchedule row in place, still
            // Proposed - it is never confirmed without another explicit Approve.
            var window = revisionWindow
                ?? new ScheduleWindowDto(schedule.SlotStart.AddDays(1), schedule.SlotEnd.AddDays(1));

            auditLog.Log(actorId, "ScheduleRevisionRequested", "PickupSchedule", schedule.Id,
                new { Reason = revisionReason, Requested = window });

            return await ProposeAsync(
                orderId, new CreateScheduleRequest(schedule.CollectionCentreId, window), actorId, ct);
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

        // "ApprovedBy" (FR19/plan §8.4) is this audit entry's ActorId — there is no
        // AgentWorkflow.ApprovedBy column (Phase 6 decision: no AgentWorkflow table
        // was built), so the audit trail is the record of who decided.
        auditLog.Log(actorId, "ScheduleDecision", "PickupSchedule", schedule.Id, new { Decision = decision });
        await NotifyOrderStakeholdersAsync(
            order,
            decision == ScheduleDecision.Approve
                ? $"Your pickup slot ({schedule.SlotStart:d MMM, HH:mm} UTC) was confirmed."
                : "Your proposed pickup slot was rejected; a new one will be proposed.",
            ct);

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
