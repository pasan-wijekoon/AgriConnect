"""HTTP surface for the Buyer-Farmer Matching Agent (plan §2: "Agentic AI
Service (internal HTTP call only)" — the ASP.NET Core backend is the only
caller, never a client app directly). Protected by a shared secret
(INTERNAL_API_SECRET, already provisioned in docker/.env.example and wired to
the backend as AgenticAi:ApiKey), matching the pattern Component D's plan
independently arrived at for the same internal-only boundary.
"""

from __future__ import annotations

import os

from fastapi import APIRouter, Header, HTTPException, status
from pydantic import BaseModel, Field

from src.app.agents.buyer_farmer_matching_agent import BuyerFarmerMatchingAgent, CandidateCentre
from src.app.validation.matching_validation import MatchValidationError

router = APIRouter(prefix="/agents/buyer-farmer-matching", tags=["matching"])


class CandidateCentreDto(BaseModel):
    centre_id: str
    name: str
    lat: float
    lng: float
    capacity: int
    current_confirmed_bookings: int


class BuyerLocationDto(BaseModel):
    lat: float
    lng: float


class MatchRequest(BaseModel):
    order_id: str
    buyer_location: BuyerLocationDto
    listing_id: str
    requested_quantity: float = Field(gt=0)
    candidate_centres: list[CandidateCentreDto]


class MatchResponse(BaseModel):
    order_id: str
    matched_centre_id: str | None
    match_confidence: float
    notes: str
    candidates_considered: int
    degraded: bool


def _check_internal_api_key(x_internal_api_key: str | None) -> None:
    expected = os.getenv("INTERNAL_API_SECRET", "")
    if not expected:
        # No secret configured — dev-mode, matches how the rest of this
        # service's env-driven config behaves when a value is left blank.
        return
    if x_internal_api_key != expected:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid or missing internal API key.")


@router.post("", response_model=MatchResponse)
def match_buyer_to_centre(
    request: MatchRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> MatchResponse:
    _check_internal_api_key(x_internal_api_key)

    agent = BuyerFarmerMatchingAgent()
    candidates = [
        CandidateCentre(
            centre_id=c.centre_id,
            name=c.name,
            lat=c.lat,
            lng=c.lng,
            capacity=c.capacity,
            current_confirmed_bookings=c.current_confirmed_bookings,
        )
        for c in request.candidate_centres
    ]

    try:
        result = agent.match(
            order_id=request.order_id,
            buyer_lat=request.buyer_location.lat,
            buyer_lng=request.buyer_location.lng,
            candidate_centres=candidates,
        )
    except MatchValidationError as exc:
        # The agent's own output failed validation — never forward a malformed
        # or hallucinated match to the caller (plan §8.5).
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail=str(exc)) from exc

    return MatchResponse(
        order_id=result.order_id,
        matched_centre_id=result.matched_centre_id,
        match_confidence=result.match_confidence,
        notes=result.notes,
        candidates_considered=result.candidates_considered,
        degraded=result.degraded,
    )
