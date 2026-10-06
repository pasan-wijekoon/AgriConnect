"""Buyer-Farmer Matching Agent (plan §8.1, Component B's own agent — the
Logistics Scheduling Agent that finds the actual slot is Student 4's, a
different agent this service does not implement).

Contract (plan §8.1):
    in:  { orderId, buyerLocation, listingId, requestedQuantity, candidateCentres }
    out: { matchedCentreId, matchConfidence, notes, explanation }

`candidateCentres` is not in the plan's original one-line contract sketch, but
was added deliberately during implementation: the architecture (plan §2) only
has an API -> Agent HTTP arrow, never the reverse, so this agent cannot query
the ASP.NET Core backend's CollectionCentre table itself. The backend already
has that data (CollectionCentreService, SchedulingService) and passes the
relevant candidates in. This is Component B's own agent, so refining its
contract during implementation is within scope; see PROGRESS.md's Decisions
section for the full reasoning.

The actual centre selection is a deterministic scoring function, never an LLM
call, whichever LLM_PROVIDER is configured. Per CLAUDE.md's AI rule ("AI may
Suggest/Recommend/Predict/Propose... AI may NOT independently commit...
Matches"), and because plan §8.5 requires this agent to run under
LLM_PROVIDER=mock without needing a live key, the match itself is always
computed the same deterministic way.

When an LLM is configured it is used only to *narrate* the match that was already
made: a short plain-language `explanation` for the Officer reviewing the schedule
proposal, written from the computed facts. That text is checked before it is used
(it must name the chosen centre and must not introduce a centre that was never a
candidate); if the LLM is unavailable or its text fails the check, `explanation`
falls back to the deterministic `notes`. The LLM never changes the decision.
"""

from __future__ import annotations

import json
import logging
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass

from langchain_core.language_models import BaseChatModel
from langchain_core.messages import BaseMessage, HumanMessage, SystemMessage

from src.app.config import Settings, get_settings
from src.app.orchestration.llm_provider import LlmConfigurationError, get_chat_model
from src.app.tools.centre_capacity_tool import CentreCapacityTool
from src.app.tools.distance_lookup_tool import DistanceLookupTool
from src.app.validation.matching_validation import (
    MatchValidationError,
    validate_explanation,
    validate_match_response,
)

logger = logging.getLogger(__name__)

# Confidence decays linearly to 0 at this distance and beyond — a simple,
# explainable heuristic, not a claim of real routing accuracy (plan §8's
# matching agent is explicitly not the Logistics Scheduling Agent's precise
# slot-finding logic).
CONFIDENCE_ZERO_DISTANCE_KM = 50.0

# How many next-best centres are offered to the narrator as context.
MAX_RUNNERS_UP = 3

# Road distances are looked up for every candidate centre; doing them one after another
# made a match take as long as the sum of all the Maps calls.
MAX_PARALLEL_DISTANCE_LOOKUPS = 8

_LLM_INSTRUCTIONS = """\
You explain to a collection-centre officer why a collection centre was suggested for a buyer's order.
The centre in the facts was already chosen by a deterministic rule (nearest centre that still has free
slots). Do not change or question the choice, and do not claim anything is confirmed: it is only a
suggestion that the officer reviews.

Write 1-2 plain sentences. Use ONLY the facts provided: mention the suggested centre by its exact name,
its distance in km and how many slots are free. Do not invent numbers or centres. If other centres are
listed, you may say they were further away. Reply with the sentences only, no markdown.
"""


@dataclass(frozen=True)
class CandidateCentre:
    centre_id: str
    name: str
    lat: float
    lng: float
    capacity: int
    current_confirmed_bookings: int


@dataclass(frozen=True)
class MatchResult:
    order_id: str
    matched_centre_id: str | None
    match_confidence: float
    notes: str
    candidates_considered: int
    degraded: bool
    # Plain-language narration of the match; equals `notes` when no LLM is available.
    explanation: str = ""


class BuyerFarmerMatchingAgent:
    def __init__(self, distance_tool: DistanceLookupTool | None = None, llm: BaseChatModel | None = None) -> None:
        self._distance_tool = distance_tool or DistanceLookupTool()
        self._llm = llm

    def match(
        self,
        order_id: str,
        buyer_lat: float,
        buyer_lng: float,
        candidate_centres: list[CandidateCentre],
    ) -> MatchResult:
        candidate_ids = {c.centre_id for c in candidate_centres}
        degraded = False
        scored: list[tuple[CandidateCentre, float, int]] = []

        with_capacity = []
        for centre in candidate_centres:
            capacity_result = CentreCapacityTool.check(centre.capacity, centre.current_confirmed_bookings)
            if capacity_result.has_capacity:
                with_capacity.append((centre, capacity_result.remaining_slots))

        # Distances are independent network calls: run them in parallel (results keep the
        # candidates' order, so the outcome is the same as a sequential loop).
        if with_capacity:
            with ThreadPoolExecutor(max_workers=min(MAX_PARALLEL_DISTANCE_LOOKUPS, len(with_capacity))) as pool:
                distances = list(
                    pool.map(
                        lambda item: self._distance_tool.get_distance(buyer_lat, buyer_lng, item[0].lat, item[0].lng),
                        with_capacity,
                    )
                )
            for (centre, remaining_slots), distance_result in zip(with_capacity, distances):
                degraded = degraded or distance_result.degraded
                scored.append((centre, distance_result.distance_km, remaining_slots))

        if not scored:
            notes = "No candidate collection centre currently has capacity for this order."
            result = MatchResult(
                order_id=order_id,
                matched_centre_id=None,
                match_confidence=0.0,
                notes=notes,
                candidates_considered=len(candidate_centres),
                degraded=degraded,
                explanation=notes,
            )
            validate_match_response(result.matched_centre_id, result.match_confidence, result.notes, candidate_ids)
            return result

        # Closest distance wins; ties broken by more remaining capacity headroom.
        scored.sort(key=lambda item: (item[1], -item[2]))
        best_centre, best_distance_km, remaining_slots = scored[0]

        confidence = max(0.0, 1.0 - (best_distance_km / CONFIDENCE_ZERO_DISTANCE_KM))
        confidence = min(confidence, 1.0)

        degraded_note = " (distance is an approximate straight-line estimate)" if degraded else ""
        notes = (
            f"Matched to {best_centre.name} - {best_distance_km:.1f} km away, "
            f"{remaining_slots} of {best_centre.capacity} slots free{degraded_note}."
        )

        result = MatchResult(
            order_id=order_id,
            matched_centre_id=best_centre.centre_id,
            match_confidence=round(confidence, 4),
            notes=notes,
            candidates_considered=len(candidate_centres),
            degraded=degraded,
            explanation=self._explain(notes, scored, degraded),
        )
        validate_match_response(result.matched_centre_id, result.match_confidence, result.notes, candidate_ids)
        return result

    def _explain(
        self, notes: str, scored: list[tuple[CandidateCentre, float, int]], degraded: bool
    ) -> str:
        """The LLM's wording of an already-made decision, or `notes` when that isn't available/valid."""
        if self._llm is None:
            return notes

        best, best_km, remaining_slots = scored[0]
        facts = {
            "suggestedCentre": {
                "name": best.name,
                "distanceKm": round(best_km, 1),
                "freeSlots": remaining_slots,
                "totalSlots": best.capacity,
            },
            "otherCentresWithFreeSlots": [
                {"name": c.name, "distanceKm": round(km, 1)} for c, km, _ in scored[1 : 1 + MAX_RUNNERS_UP]
            ],
            "distanceIsApproximate": degraded,
        }

        try:
            reply = self._llm.invoke(
                [SystemMessage(content=_LLM_INSTRUCTIONS), HumanMessage(content=json.dumps(facts, indent=2))]
            )
            text = _message_text(reply).strip()
            validate_explanation(text, best.name, {c.name for c, _, _ in scored})
            return text
        except MatchValidationError as exc:
            logger.warning("Discarding LLM explanation for the centre match: %s", exc)
        except Exception:  # noqa: BLE001 - any LLM/provider failure must degrade, never block a match
            logger.warning("LLM explanation for the centre match failed; using the deterministic notes.", exc_info=True)
        return notes


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


def build_matching_agent(settings: Settings | None = None) -> BuyerFarmerMatchingAgent:
    """The agent with the LLM selected by LLM_PROVIDER (none in mock mode or when no key is set)."""
    settings = settings or get_settings()
    try:
        llm = get_chat_model(settings)
    except LlmConfigurationError as exc:
        logger.warning("Matching agent running without an LLM: %s", exc)
        llm = None
    return BuyerFarmerMatchingAgent(llm=llm)
