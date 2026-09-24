namespace AgriConnect.Api.Services;

public record SchedulingWindow(DateTimeOffset Start, DateTimeOffset End);

public record ExistingBooking(DateTimeOffset Start, DateTimeOffset End);

public record SchedulingProposal(DateTimeOffset ProposedSlotStart, DateTimeOffset ProposedSlotEnd, bool ConflictChecked);

/// <summary>
/// Seam for the Logistics Scheduling Agent — Student 4 / Component D's individual
/// contribution (plan §1's ownership table). Component B defines and exposes this
/// contract (input it needs, where its output lands) but does not implement the
/// agent's actual slot-finding logic; that boundary is explicit in the plan and in
/// CLAUDE.md §7. Swap <see cref="StubLogisticsSchedulingPort"/> for an HTTP client
/// calling the real agent once it exists, matching the documented contract
/// (plan §8.2) exactly.
/// </summary>
public interface ILogisticsSchedulingPort
{
    Task<SchedulingProposal> ProposeSlotAsync(
        Guid orderId,
        Guid centreId,
        SchedulingWindow preferredWindow,
        IReadOnlyList<ExistingBooking> existingBookings,
        CancellationToken cancellationToken = default);
}
