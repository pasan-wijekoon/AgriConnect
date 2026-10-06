"""Input/output contracts for the Logistics Scheduling Agent.

Everything that crosses a trust boundary — the caller's request, tool responses and LLM
output — is parsed through these models, so malformed data raises pydantic.ValidationError
instead of flowing on as raw text.
"""

from datetime import datetime, timedelta
from typing import Self, TypedDict

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, ValidationInfo, model_validator


# ---- Contracts as documented (DFD §4.4) -------------------------------------


class PreferredWindowDict(TypedDict):
    start: str
    end: str


class BookingDict(TypedDict):
    slotStart: str
    slotEnd: str


class LogisticsScheduleInput(TypedDict):
    orderId: str
    centreId: str
    preferredWindow: PreferredWindowDict
    existingBookings: list[BookingDict]


class LogisticsScheduleOutput(TypedDict):
    proposedSlotStart: str
    proposedSlotEnd: str
    conflictChecked: bool
    reasoning: str


# ---- Validating models --------------------------------------------------------


class TimeWindow(BaseModel):
    model_config = ConfigDict(extra="forbid")

    # AwareDatetime rejects timestamps without an offset: "08:00" is ambiguous across timezones.
    start: AwareDatetime
    end: AwareDatetime

    @model_validator(mode="after")
    def _end_after_start(self) -> Self:
        if self.end <= self.start:
            raise ValueError("end must be after start")
        return self


class Booking(BaseModel):
    model_config = ConfigDict(extra="forbid")

    slotStart: AwareDatetime
    slotEnd: AwareDatetime

    @model_validator(mode="after")
    def _end_after_start(self) -> Self:
        if self.slotEnd <= self.slotStart:
            raise ValueError("slotEnd must be after slotStart")
        return self

    def overlaps(self, start: datetime, end: datetime) -> bool:
        return self.slotStart < end and self.slotEnd > start


class ScheduleRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    orderId: str = Field(min_length=1)
    centreId: str = Field(min_length=1)
    preferredWindow: TimeWindow
    existingBookings: list[Booking] = Field(default_factory=list)


class CentreCapacity(BaseModel):
    maxDailySlots: int = Field(ge=1)
    slotDurationMinutes: int = Field(ge=5, le=24 * 60)

    @property
    def slot_duration(self) -> timedelta:
        return timedelta(minutes=self.slotDurationMinutes)


class ScheduleProposal(BaseModel):
    """The agent's answer. Checked against the scheduler's facts before it is returned.

    Pass ``context={"expected_slot": (start, end), "bookings": [...], "window": (start, end)}``
    to model_validate to enforce that the proposal is exactly the conflict-checked slot.
    """

    model_config = ConfigDict(extra="forbid")

    proposedSlotStart: AwareDatetime
    proposedSlotEnd: AwareDatetime
    conflictChecked: bool
    reasoning: str = Field(min_length=1, max_length=2000)

    @model_validator(mode="after")
    def _matches_verified_slot(self, info: ValidationInfo) -> Self:
        if self.proposedSlotEnd <= self.proposedSlotStart:
            raise ValueError("proposedSlotEnd must be after proposedSlotStart")
        if not self.conflictChecked:
            raise ValueError("conflictChecked must be true; unchecked slots are never proposed")

        context = info.context or {}
        expected = context.get("expected_slot")
        if expected and (self.proposedSlotStart, self.proposedSlotEnd) != expected:
            raise ValueError("proposed slot differs from the conflict-checked slot")

        window = context.get("window")
        if window and not (window[0] <= self.proposedSlotStart and self.proposedSlotEnd <= window[1]):
            raise ValueError("proposed slot falls outside the preferred window")

        clashes = [b for b in context.get("bookings", []) if b.overlaps(self.proposedSlotStart, self.proposedSlotEnd)]
        if clashes:
            raise ValueError(f"proposed slot overlaps {len(clashes)} existing booking(s)")
        return self

    def to_output(self) -> LogisticsScheduleOutput:
        return LogisticsScheduleOutput(**self.model_dump(mode="json"))
