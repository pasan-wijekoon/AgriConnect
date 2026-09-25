namespace AgriConnect.Api.Services;

/// <summary>One collection centre SchedulingService offers the matching agent
/// to choose from — the agent has no way to query the database itself (plan §2's
/// architecture is a one-way API→Agent HTTP arrow), so the backend supplies
/// exactly the data the agent needs to score each candidate.</summary>
public record MatchCandidateCentre(
    Guid CentreId, string Name, decimal Lat, decimal Lng, int Capacity, int CurrentConfirmedBookings);

/// <summary>The agent's match decision (plan §8.1/§8.5). <see cref="MatchedCentreId"/>
/// is null when the agent considered every candidate and found none with capacity —
/// a real, meaningful "no match" signal, distinct from the call itself failing
/// (which <see cref="IBuyerFarmerMatchingPort.MatchAsync"/> reports by returning
/// null rather than a <see cref="MatchResult"/> at all).</summary>
public record MatchResult(
    Guid? MatchedCentreId, double MatchConfidence, string Notes, int CandidatesConsidered, bool Degraded);

/// <summary>
/// FR10/plan §8.1 — Component B's own Buyer-Farmer Matching Agent
/// (<c>agentic-ai/</c>, built and independently tested in Phase 8; this is the
/// first thing in the ASP.NET Core backend that actually calls it over HTTP).
/// </summary>
public interface IBuyerFarmerMatchingPort
{
    /// <summary>
    /// Returns null if the call itself could not be completed (network/timeout/
    /// malformed response) — callers fall back to their own stand-in logic in
    /// that case, the same "AI service unreachable, degrade gracefully" posture
    /// already used for the Maps/Distance integration (plan §9), rather than
    /// failing the whole scheduling operation because an optional AI suggestion
    /// couldn't be fetched.
    /// </summary>
    Task<MatchResult?> MatchAsync(
        Guid orderId,
        decimal buyerLat,
        decimal buyerLng,
        Guid listingId,
        decimal requestedQuantity,
        IReadOnlyList<MatchCandidateCentre> candidateCentres,
        CancellationToken cancellationToken = default);
}
