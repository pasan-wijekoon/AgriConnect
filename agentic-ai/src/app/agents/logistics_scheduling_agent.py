"""Logistics Scheduling Agent — finds a conflict-free pickup/delivery slot at a collection centre.

Owned by Component D (Student 4); its proposal feeds Component B's /api/orders/{id}/schedule
and, like every AI proposal, waits for an officer's approval before it takes effect.

Graph:

    START → load_capacity → load_bookings → find_slot ─┬─ slot found ──→ explain → verify → END
                                 ↑                      ├─ day full/no gap, < 3 days ahead → next_day
                                 └──────── next_day ←───┘
                                                        └─ 3 days ahead exhausted → no_slot (raises)

The slot is chosen by deterministic search, never by the LLM: picking a time that does not
clash is arithmetic, and a model can hallucinate one that does. When an LLM is configured it
only writes the officer-facing explanation, and its reply must reproduce the verified slot
exactly or it is rejected with a ValidationError.
"""

import json
import logging
import re
from collections.abc import Mapping
from datetime import date, datetime, timedelta
from typing import Any, Literal, Protocol, TypedDict

from langchain_core.language_models import BaseChatModel
from langchain_core.messages import BaseMessage, HumanMessage, SystemMessage
from langgraph.graph import END, START, StateGraph

from src.app.config import Settings, get_settings
from src.app.orchestration.llm_provider import get_chat_model
from src.app.tools.booking_calendar_tool import CENTRE_TZ, BookingCalendarTool
from src.app.tools.centre_capacity_tool import CentreCapacityTool
from src.app.validation.logistics_schemas import (
    Booking,
    CentreCapacity,
    LogisticsScheduleOutput,
    ScheduleProposal,
    ScheduleRequest,
)

logger = logging.getLogger(__name__)

MAX_DAYS_AHEAD = 3

Slot = tuple[datetime, datetime]


class NoSlotAvailableError(Exception):
    """No free slot in the preferred window on the preferred day or the next MAX_DAYS_AHEAD days."""

    def __init__(self, day_notes: list[str]) -> None:
        self.day_notes = day_notes
        super().__init__("No free slot found. " + " ".join(day_notes))


class CapacityTool(Protocol):
    def run(self, centre_id: str) -> CentreCapacity: ...


class CalendarTool(Protocol):
    def run(self, centre_id: str, day: date) -> list[Booking]: ...


class SchedulingState(TypedDict, total=False):
    request: ScheduleRequest
    capacity: CentreCapacity
    day_offset: int
    window: Slot
    bookings: list[Booking]
    slot: Slot | None
    day_notes: list[str]
    draft: dict[str, Any]
    output: LogisticsScheduleOutput


# ---- Pure scheduling logic -------------------------------------------------------


def centre_day(moment: datetime) -> date:
    return moment.astimezone(CENTRE_TZ).date()


def merge_bookings(*groups: list[Booking]) -> list[Booking]:
    """Combine bookings from the caller and the calendar, dropping duplicates."""
    unique: dict[Slot, Booking] = {}
    for booking in (b for group in groups for b in group):
        unique.setdefault((booking.slotStart, booking.slotEnd), booking)
    return sorted(unique.values(), key=lambda b: b.slotStart)


def find_first_free_slot(
    window_start: datetime, window_end: datetime, duration: timedelta, bookings: list[Booking]
) -> Slot | None:
    """Earliest slot of `duration` inside the window that overlaps no booking.

    On a clash, jumps to the end of the latest clashing booking, so back-to-back bookings
    are skipped in one step. Always terminates: each jump moves strictly forward.
    """
    start = window_start
    while start + duration <= window_end:
        end = start + duration
        clashes = [b for b in bookings if b.overlaps(start, end)]
        if not clashes:
            return start, end
        start = max(b.slotEnd for b in clashes).astimezone(window_start.tzinfo)
    return None


def _fmt(moment: datetime) -> str:
    return moment.strftime("%H:%M")


# ---- Agent -----------------------------------------------------------------------


class LogisticsSchedulingAgent:
    def __init__(self, capacity_tool: CapacityTool, calendar_tool: CalendarTool, llm: BaseChatModel | None = None):
        self._capacity_tool = capacity_tool
        self._calendar_tool = calendar_tool
        self._llm = llm
        self.graph = self._build_graph()

    def schedule(self, payload: Mapping[str, Any] | ScheduleRequest) -> LogisticsScheduleOutput:
        """Propose a slot. Raises pydantic.ValidationError on invalid input or LLM output,
        NoSlotAvailableError when nothing fits, and ToolError when a tool's source fails."""
        request = payload if isinstance(payload, ScheduleRequest) else ScheduleRequest.model_validate(payload)
        logger.info("Scheduling order %s at centre %s", request.orderId, request.centreId)

        final = self.graph.invoke({"request": request, "day_offset": 0, "day_notes": []})
        return final["output"]

    def _build_graph(self):
        graph = StateGraph(SchedulingState)
        graph.add_node("load_capacity", self._load_capacity)
        graph.add_node("load_bookings", self._load_bookings)
        graph.add_node("find_slot", self._find_slot)
        graph.add_node("next_day", self._next_day)
        graph.add_node("no_slot", self._no_slot)
        graph.add_node("explain", self._explain)
        graph.add_node("verify", self._verify)

        graph.add_edge(START, "load_capacity")
        graph.add_edge("load_capacity", "load_bookings")
        graph.add_edge("load_bookings", "find_slot")
        graph.add_conditional_edges(
            "find_slot", self._route, {"explain": "explain", "next_day": "next_day", "no_slot": "no_slot"}
        )
        graph.add_edge("next_day", "load_bookings")
        graph.add_edge("explain", "verify")
        graph.add_edge("verify", END)
        graph.add_edge("no_slot", END)
        return graph.compile()

    # ---- Nodes ---------------------------------------------------------------------

    def _load_capacity(self, state: SchedulingState) -> SchedulingState:
        capacity = self._capacity_tool.run(state["request"].centreId)
        logger.info("Tool %s -> %s", "centre_capacity", capacity.model_dump())
        return {"capacity": capacity}

    def _load_bookings(self, state: SchedulingState) -> SchedulingState:
        request = state["request"]
        shift = timedelta(days=state["day_offset"])
        window = (request.preferredWindow.start + shift, request.preferredWindow.end + shift)
        day = centre_day(window[0])

        from_calendar = self._calendar_tool.run(request.centreId, day)
        logger.info("Tool %s(%s) -> %d bookings", "booking_calendar", day, len(from_calendar))
        return {"window": window, "bookings": merge_bookings(request.existingBookings, from_calendar)}

    def _find_slot(self, state: SchedulingState) -> SchedulingState:
        capacity = state["capacity"]
        window_start, window_end = state["window"]
        day = centre_day(window_start)
        booked_that_day = [b for b in state["bookings"] if centre_day(b.slotStart) == day]

        slot: Slot | None = None
        if len(booked_that_day) >= capacity.maxDailySlots:
            note = f"{day}: centre at capacity ({len(booked_that_day)}/{capacity.maxDailySlots} bookings)."
        else:
            slot = find_first_free_slot(window_start, window_end, capacity.slot_duration, state["bookings"])
            note = (
                f"{day}: free slot {_fmt(slot[0])}-{_fmt(slot[1])}."
                if slot
                else f"{day}: no free {capacity.slotDurationMinutes}-minute gap between "
                f"{_fmt(window_start)} and {_fmt(window_end)}."
            )

        logger.info("find_slot: %s", note)
        return {"slot": slot, "day_notes": [*state["day_notes"], note]}

    @staticmethod
    def _route(state: SchedulingState) -> Literal["explain", "next_day", "no_slot"]:
        if state["slot"]:
            return "explain"
        return "next_day" if state["day_offset"] < MAX_DAYS_AHEAD else "no_slot"

    @staticmethod
    def _next_day(state: SchedulingState) -> SchedulingState:
        return {"day_offset": state["day_offset"] + 1}

    @staticmethod
    def _no_slot(state: SchedulingState) -> SchedulingState:
        raise NoSlotAvailableError(state["day_notes"])

    def _explain(self, state: SchedulingState) -> SchedulingState:
        start, end = state["slot"]
        if self._llm is None:
            return {"draft": {
                "proposedSlotStart": start,
                "proposedSlotEnd": end,
                "conflictChecked": True,
                "reasoning": self._deterministic_reasoning(state),
            }}

        reply = self._llm.invoke([
            SystemMessage(content=_LLM_INSTRUCTIONS),
            HumanMessage(content=json.dumps(self._facts(state), indent=2)),
        ])
        # Malformed or incomplete JSON raises ValidationError here; the raw text goes nowhere else.
        parsed = ScheduleProposal.model_validate_json(_strip_code_fence(_message_text(reply)))
        return {"draft": parsed.model_dump()}

    def _verify(self, state: SchedulingState) -> SchedulingState:
        """Independent check of the draft against the scheduler's facts, for both paths."""
        slot = state["slot"]
        verified = ScheduleProposal.model_validate(
            state["draft"],
            context={"expected_slot": slot, "window": state["window"], "bookings": state["bookings"]},
        )
        # Rebuild from the scheduler's own datetimes so the output keeps the caller's timezone
        # even if an LLM restated the same instant in another offset.
        final = ScheduleProposal(
            proposedSlotStart=slot[0], proposedSlotEnd=slot[1], conflictChecked=True, reasoning=verified.reasoning
        )
        logger.info("Proposed %s-%s for order %s", slot[0].isoformat(), slot[1].isoformat(), state["request"].orderId)
        return {"output": final.to_output()}

    # ---- Explanations ----------------------------------------------------------------

    @staticmethod
    def _deterministic_reasoning(state: SchedulingState) -> str:
        capacity = state["capacity"]
        start, end = state["slot"]
        day = centre_day(start)
        booked = sum(1 for b in state["bookings"] if centre_day(b.slotStart) == day)
        skipped = state["day_notes"][:-1]

        parts = [
            f"Centre {state['request'].centreId} takes {capacity.slotDurationMinutes}-minute slots, "
            f"up to {capacity.maxDailySlots} per day."
        ]
        if skipped:
            parts.append("Earlier days skipped: " + " ".join(skipped))
        parts.append(
            f"Proposed {day} {_fmt(start)}-{_fmt(end)} (UTC{start.strftime('%z')[:3]}:{start.strftime('%z')[3:]}): "
            f"the earliest free slot in the preferred window, checked against {booked} booking(s) that day."
        )
        return " ".join(parts)

    @staticmethod
    def _facts(state: SchedulingState) -> dict[str, Any]:
        start, end = state["slot"]
        return {
            "centreId": state["request"].centreId,
            "orderId": state["request"].orderId,
            "slotDurationMinutes": state["capacity"].slotDurationMinutes,
            "maxDailySlots": state["capacity"].maxDailySlots,
            "preferredWindow": {
                "start": state["request"].preferredWindow.start.isoformat(),
                "end": state["request"].preferredWindow.end.isoformat(),
            },
            "daysChecked": state["day_notes"],
            "proposedSlotStart": start.isoformat(),
            "proposedSlotEnd": end.isoformat(),
        }


_LLM_INSTRUCTIONS = """\
You explain collection-centre scheduling decisions to the officer who must approve them.
The slot in the facts was already chosen and conflict-checked by a deterministic scheduler.
Do not change it.

Reply with ONLY a JSON object with exactly these keys:
  "proposedSlotStart": copied exactly from the facts,
  "proposedSlotEnd":   copied exactly from the facts,
  "conflictChecked":   true,
  "reasoning":         2-3 plain sentences: why this slot, and any days that were skipped and why.
"""


def _message_text(message: BaseMessage) -> str:
    content = message.content
    if isinstance(content, str):
        return content
    # Some providers return a list of content parts.
    return "".join(
        part if isinstance(part, str) else part.get("text", "")
        for part in content
        if isinstance(part, str) or part.get("type") == "text"
    )


_CODE_FENCE = re.compile(r"^\s*```(?:json)?\s*(.*?)\s*```\s*$", re.DOTALL)


def _strip_code_fence(text: str) -> str:
    match = _CODE_FENCE.match(text)
    return match.group(1) if match else text.strip()


def build_logistics_agent(settings: Settings | None = None) -> LogisticsSchedulingAgent:
    """The agent wired to the tools and LLM selected by LLM_PROVIDER."""
    settings = settings or get_settings()
    return LogisticsSchedulingAgent(
        CentreCapacityTool(settings), BookingCalendarTool(settings), get_chat_model(settings)
    )
