namespace AgriConnect.Api.Services;

/// <summary>
/// Stand-in for Student 4's not-yet-built Logistics Scheduling Agent. Echoes the
/// preferred window back and reports whether it overlaps any of the existing
/// bookings passed in, matching the documented contract's output shape exactly
/// (plan §8.2). Deliberately does not attempt to find an alternative slot on
/// conflict — inventing that search algorithm would be overstepping into
/// Component D's individual contribution. SchedulingService (which Component B
/// does own) is what actually enforces "conflict-free" before anything is
/// persisted; this stub only tells it whether the requested window was clean.
/// </summary>
public class StubLogisticsSchedulingPort : ILogisticsSchedulingPort
{
    public Task<SchedulingProposal> ProposeSlotAsync(
        Guid orderId,
        Guid centreId,
        SchedulingWindow preferredWindow,
        IReadOnlyList<ExistingBooking> existingBookings,
        CancellationToken cancellationToken = default)
    {
        var conflicts = existingBookings.Any(b => b.Start < preferredWindow.End && b.End > preferredWindow.Start);
        var proposal = new SchedulingProposal(preferredWindow.Start, preferredWindow.End, ConflictChecked: !conflicts);
        return Task.FromResult(proposal);
    }
}
