using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos;

public record ScheduleWindowDto(DateTimeOffset Start, DateTimeOffset End);

/// <summary>Caller-supplied buyer coordinates for the Buyer-Farmer Matching Agent
/// (plan §8.1) — never stored server-side, same posture as FR21's nearest-centre
/// endpoint. No shared User/Address model exists yet to source this from
/// automatically (plan §16 open question #1), so it's optional: omit it and
/// scheduling falls back to its pre-existing region-based centre selection.</summary>
public record BuyerLocationDto(decimal Lat, decimal Lng);

public record CreateScheduleRequest(
    Guid? CollectionCentreId, ScheduleWindowDto? PreferredWindow, BuyerLocationDto? BuyerLocation = null);

public record ScheduleResponse(
    Guid Id,
    Guid OrderId,
    Guid CollectionCentreId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    ScheduleStatus Status,
    bool ConflictChecked);

/// <summary>The verb an Officer sends (FR19: Approve/Reject) — distinct from
/// ScheduleStatus, which is the persisted state the verb transitions to.</summary>
public enum ScheduleDecision
{
    Approve,
    Reject
}

public record ScheduleDecisionRequest(ScheduleDecision Decision);
