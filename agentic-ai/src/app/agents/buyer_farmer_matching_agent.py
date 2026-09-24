"""Buyer-Farmer Matching Agent (plan §8.1, Component B's own agent — the
Logistics Scheduling Agent that finds the actual slot is Student 4's, a
different agent this service does not implement).

Contract (plan §8.1):
    in:  { orderId, buyerLocation, listingId, requestedQuantity, candidateCentres }
    out: { matchedCentreId, matchConfidence, notes }

`candidateCentres` is not in the plan's original one-line contract sketch, but
was added deliberately during implementation: the architecture (plan §2) only
has an API -> Agent HTTP arrow, never the reverse, so this agent cannot query
the ASP.NET Core backend's CollectionCentre table itself. The backend already
has that data (CollectionCentreService, SchedulingService) and passes the
relevant candidates in. This is Component B's own agent, so refining its
contract during implementation is within scope; see PROGRESS.md's Decisions
section for the full reasoning.

The actual centre selection is a deterministic scoring function, not an LLM
call, even though LLM_PROVIDER/DEFAULT_MODEL are configured in this service.
Per CLAUDE.md's AI rule ("AI may Suggest/Recommend/Predict/Propose... AI may
NOT independently commit... Matches"), and because plan §8.5 requires this
agent to run under LLM_PROVIDER=mock without needing a live key, the match
itself is always computed the same deterministic way regardless of provider.
A natural-language LLM narration of `notes` is a plausible future extension
(noted, not built) — never the decision itself.
"""

from __future__ import annotations

from dataclasses import dataclass

from src.app.tools.centre_capacity_tool import CentreCapacityTool
from src.app.tools.distance_lookup_tool import DistanceLookupTool
from src.app.validation.matching_validation import validate_match_response

# Confidence decays linearly to 0 at this distance and beyond — a simple,
# explainable heuristic, not a claim of real routing accuracy (plan §8's
# matching agent is explicitly not the Logistics Scheduling Agent's precise
# slot-finding logic).
CONFIDENCE_ZERO_DISTANCE_KM = 50.0


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


class BuyerFarmerMatchingAgent:
    def __init__(self, distance_tool: DistanceLookupTool | None = None) -> None:
        self._distance_tool = distance_tool or DistanceLookupTool()

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

        for centre in candidate_centres:
            capacity_result = CentreCapacityTool.check(centre.capacity, centre.current_confirmed_bookings)
            if not capacity_result.has_capacity:
                continue

            distance_result = self._distance_tool.get_distance(buyer_lat, buyer_lng, centre.lat, centre.lng)
            degraded = degraded or distance_result.degraded
            scored.append((centre, distance_result.distance_km, capacity_result.remaining_slots))

        if not scored:
            result = MatchResult(
                order_id=order_id,
                matched_centre_id=None,
                match_confidence=0.0,
                notes="No candidate collection centre currently has capacity for this order.",
                candidates_considered=len(candidate_centres),
                degraded=degraded,
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
        )
        validate_match_response(result.matched_centre_id, result.match_confidence, result.notes, candidate_ids)
        return result
