"""
FastAPI REST routes for Component C Agentic AI: Quality & Compliance Validation Agent.
Called internally by the ASP.NET Core Monolith API.
"""
from typing import List, Optional
from fastapi import APIRouter, HTTPException, Depends
from pydantic import BaseModel, Field

from app.orchestration.quality_graph import quality_graph
from app.config import settings

router = APIRouter(prefix="/api/v1/quality-agent", tags=["Quality & Compliance Agent"])


class EvaluateListingRequest(BaseModel):
    listing_id: str
    crop_name: str
    quantity: float
    unit: str = "kg"
    claimed_grade: str
    confirmed_grade: Optional[str] = None
    inspector_notes: Optional[str] = None
    photo_urls: List[str] = Field(default_factory=list)
    min_price: Optional[float] = None
    proposed_price: Optional[float] = None


class GradeAssessmentRequest(BaseModel):
    crop_name: str
    photo_urls: List[str]
    preliminary_notes: Optional[str] = None


@router.get("/health")
def agent_health():
    return {
        "agent": "Quality & Compliance Validation Agent (Component C)",
        "model": settings.default_model,
        "llm_provider": settings.llm_provider,
        "gemini_configured": bool(settings.gemini_api_key),
        "status": "ready"
    }


@router.post("/validate")
async def validate_listing(req: EvaluateListingRequest):
    """
    Execute end-to-end Quality & Compliance agent workflow (LangGraph + Gemini 3.8 Flash).
    Returns deterministic tool logs, failed checks, flags, grade confidence, and gate recommendation.
    """
    try:
        initial_state = {
            "listing_id": req.listing_id,
            "crop_name": req.crop_name,
            "quantity": req.quantity,
            "unit": req.unit,
            "claimed_grade": req.claimed_grade,
            "confirmed_grade": req.confirmed_grade,
            "inspector_notes": req.inspector_notes,
            "photo_urls": req.photo_urls,
            "min_price": req.min_price,
            "proposed_price": req.proposed_price,
            "result": None
        }

        final_state = await quality_graph.ainvoke(initial_state)
        result = final_state.get("result")
        if not result:
            raise HTTPException(status_code=500, detail="Quality graph failed to produce a result.")
        return result
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Validation failed: {str(e)}")
