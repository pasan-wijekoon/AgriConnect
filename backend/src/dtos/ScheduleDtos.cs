using AgriConnect.Api.Models;

namespace AgriConnect.Api.Dtos;

public record ScheduleWindowDto(DateTimeOffset Start, DateTimeOffset End);

public record CreateScheduleRequest(Guid? CollectionCentreId, ScheduleWindowDto? PreferredWindow);

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
