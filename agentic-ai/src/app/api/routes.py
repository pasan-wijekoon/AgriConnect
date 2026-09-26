"""
FastAPI Routes for Agentic AI Service.
Exposes Fair-Price Estimation Agent, LangGraph Orchestration Coordinator,
and Today Market Prices Discovery feed.
"""
import os
from typing import List, Optional, Any, Dict
from fastapi import APIRouter, Depends, HTTPException, Header, Query
from pydantic import BaseModel, Field

from ..agents.fair_price_agent import FairPriceEstimationAgent
from ..orchestration.coordinator import LangGraphCoordinator
from ..tools.market_price_tool import BENCHMARK_PRICES


def verify_internal_secret(x_internal_api_secret: Optional[str] = Header(default=None)):
    """
    This service is internal-only (README: "reachable only by the API"), but a
    private Render instance is still reachable by anyone who has/guesses its URL.
    Require a shared secret header, set via INTERNAL_API_SECRET, from the .NET
    backend on every call. If INTERNAL_API_SECRET isn't configured (e.g. a bare
    local dev run), the check is skipped so `uv run uvicorn ...` still works
    out of the box — set it in any shared/deployed environment.
    """
    expected = os.getenv("INTERNAL_API_SECRET")
    if not expected:
        return
    if x_internal_api_secret != expected:
        raise HTTPException(status_code=401, detail="Missing or invalid internal API secret.")


router = APIRouter(dependencies=[Depends(verify_internal_secret)])

# Singletons for agent and coordinator
fair_price_agent = FairPriceEstimationAgent()
coordinator = LangGraphCoordinator()


# --- Pydantic Request / Response Models ---
class FairPriceRequest(BaseModel):
    cropId: str = Field(..., description="Unique ID or name of the crop")
    regionId: str = Field(..., description="Unique ID or name of the region")
    quantity: float = Field(..., gt=0, description="Quantity in designated unit")
    claimedGrade: str = Field(default="A", description="Claimed quality grade (A, B, C)")
    recentSaleData: Optional[List[Any]] = Field(default=[], description="Recent transaction prices or sale records")
    cropName: Optional[str] = None
    regionName: Optional[str] = None


class OrchestrationRunRequest(BaseModel):
    objectiveText: str = Field(..., description="Triggering domain objective")
    triggerType: str = Field(default="NewListingSubmitted", description="Domain trigger event")
    triggerEntityId: Optional[str] = None
    listingContext: Dict[str, Any] = Field(..., description="Listing details and parameters")


# Curated produce catalog with photos for Today's Market Prices Discovery
# 20 of Sri Lanka's most popular wholesale produce items
TODAY_PRODUCE_CATALOG = [
    {
        "cropId": "a1000000-0000-0000-0000-000000000004",
        "name": "Tomatoes",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Dambulla",
        "imageUrl": "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=500&auto=format&fit=crop",
        "change24h": 4.2,
        "trend": "rising"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000005",
        "name": "Carrots",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Nuwara Eliya",
        "imageUrl": "https://images.unsplash.com/photo-1598170845058-32b9d6a5c317?w=500&auto=format&fit=crop",
        "change24h": -1.8,
        "trend": "falling"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000008",
        "name": "Potatoes",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Nuwara Eliya",
        "imageUrl": "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=500&auto=format&fit=crop",
        "change24h": 0.8,
        "trend": "stable"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000007",
        "name": "Onions",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Dambulla",
        "imageUrl": "https://images.unsplash.com/photo-1618512496248-a07fe83aa8cb?w=500&auto=format&fit=crop",
        "change24h": 2.5,
        "trend": "rising"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000006",
        "name": "Chili",
        "category": "Spices",
        "unit": "kg",
        "defaultRegion": "Jaffna",
        "imageUrl": "https://images.unsplash.com/photo-1588252303782-cb80119abd6d?w=500&auto=format&fit=crop",
        "change24h": 5.4,
        "trend": "rising"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000001",
        "name": "Rice (Keeri Samba)",
        "category": "Grains",
        "unit": "kg",
        "defaultRegion": "Anuradhapura",
        "imageUrl": "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=500&auto=format&fit=crop",
        "change24h": 0.2,
        "trend": "stable"
    },
    {
        "cropId": "a1000000-0000-0000-0000-00000000000a",
        "name": "Banana (Ambul)",
        "category": "Fruits",
        "unit": "kg",
        "defaultRegion": "Kurunegala",
        "imageUrl": "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=500&auto=format&fit=crop",
        "change24h": -0.9,
        "trend": "falling"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000003",
        "name": "Coconut",
        "category": "Fruits",
        "unit": "nut",
        "defaultRegion": "Kurunegala",
        "imageUrl": "https://images.unsplash.com/photo-1544376798-89aa6b82c6cd?w=500&auto=format&fit=crop",
        "change24h": 1.1,
        "trend": "stable"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000002",
        "name": "Tea (BOP)",
        "category": "Beverages",
        "unit": "kg",
        "defaultRegion": "Nuwara Eliya",
        "imageUrl": "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=500&auto=format&fit=crop",
        "change24h": 0.4,
        "trend": "stable"
    },
    {
        "cropId": "a1000000-0000-0000-0000-000000000009",
        "name": "Cinnamon (Alba)",
        "category": "Spices",
        "unit": "kg",
        "defaultRegion": "Matara",
        "imageUrl": "https://images.unsplash.com/photo-1509358271058-acd22cc93898?w=500&auto=format&fit=crop",
        "change24h": 1.7,
        "trend": "rising"
    },
    {
        "cropId": "c0000001-0000-0000-0000-00000000000b",
        "name": "Leeks",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Nuwara Eliya",
        "imageUrl": "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=500&auto=format&fit=crop",
        "change24h": -2.1,
        "trend": "falling"
    },
    {
        "cropId": "c0000001-0000-0000-0000-00000000000c",
        "name": "Cabbage",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Nuwara Eliya",
        "imageUrl": "https://images.unsplash.com/photo-1594282486552-05b4d80fbb9f?w=500&auto=format&fit=crop",
        "change24h": 3.1,
        "trend": "rising"
    },
    {
        "cropId": "c0000001-0000-0000-0000-00000000000d",
        "name": "Pumpkin",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Kurunegala",
        "imageUrl": "https://images.unsplash.com/photo-1570586437263-ab629fccc818?w=500&auto=format&fit=crop",
        "change24h": -0.5,
        "trend": "stable"
    },
    {
        "cropId": "c0000001-0000-0000-0000-00000000000e",
        "name": "Beans",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Badulla",
        "imageUrl": "https://images.unsplash.com/photo-1567375698348-5d9d5ae10c3a?w=500&auto=format&fit=crop",
        "change24h": 1.9,
        "trend": "rising"
    },
    {
        "cropId": "c0000001-0000-0000-0000-00000000000f",
        "name": "Papaya",
        "category": "Fruits",
        "unit": "kg",
        "defaultRegion": "Gampaha",
        "imageUrl": "https://images.unsplash.com/photo-1517282009859-f000ec3b26fe?w=500&auto=format&fit=crop",
        "change24h": -1.2,
        "trend": "falling"
    },
    {
        "cropId": "c0000001-0000-0000-0000-000000000010",
        "name": "Mango",
        "category": "Fruits",
        "unit": "kg",
        "defaultRegion": "Jaffna",
        "imageUrl": "https://images.unsplash.com/photo-1553279768-865429fa0078?w=500&auto=format&fit=crop",
        "change24h": 6.8,
        "trend": "rising"
    },
    {
        "cropId": "c0000001-0000-0000-0000-000000000011",
        "name": "Pepper (Black)",
        "category": "Spices",
        "unit": "kg",
        "defaultRegion": "Matale",
        "imageUrl": "https://images.unsplash.com/photo-1599909533601-aa1e5c0fb0a4?w=500&auto=format&fit=crop",
        "change24h": 0.9,
        "trend": "stable"
    },
    {
        "cropId": "c0000001-0000-0000-0000-000000000012",
        "name": "Drumstick (Murunga)",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Jaffna",
        "imageUrl": "https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=500&auto=format&fit=crop",
        "change24h": 2.3,
        "trend": "rising"
    },
    {
        "cropId": "c0000001-0000-0000-0000-000000000013",
        "name": "Brinjal (Eggplant)",
        "category": "Vegetables",
        "unit": "kg",
        "defaultRegion": "Dambulla",
        "imageUrl": "https://images.unsplash.com/photo-1613881553903-4bedfcea4dd1?w=500&auto=format&fit=crop",
        "change24h": -0.7,
        "trend": "falling"
    },
    {
        "cropId": "c0000001-0000-0000-0000-000000000014",
        "name": "Lime",
        "category": "Fruits",
        "unit": "kg",
        "defaultRegion": "Colombo",
        "imageUrl": "https://images.unsplash.com/photo-1590502593747-42a996133562?w=500&auto=format&fit=crop",
        "change24h": 3.5,
        "trend": "rising"
    }
]


@router.post("/agents/fair-price")
def get_fair_price_estimation(request: FairPriceRequest):
    """
    Component A: Fair-Price Estimation Agent endpoint.
    Contract:
      Input: { cropId, regionId, quantity, claimedGrade, recentSaleData[] }
      Output: { suggestedPriceMin, suggestedPriceMax, confidence, reasoningSummary }
    """
    try:
        result = fair_price_agent.estimate_price(
            crop_id=request.cropId,
            region_id=request.regionId,
            quantity=request.quantity,
            claimed_grade=request.claimedGrade,
            recent_sale_data=request.recentSaleData,
            crop_name=request.cropName,
            region_name=request.regionName
        )
        return {
            "cropId": request.cropId,
            "regionId": request.regionId,
            "claimedGrade": request.claimedGrade,
            "suggestedPriceMin": result["suggestedPriceMin"],
            "suggestedPriceMax": result["suggestedPriceMax"],
            "confidence": result["confidence"],
            "reasoningSummary": result["reasoningSummary"],
            "benchmarkWholesale": result["benchmarkWholesale"],
            "farmgateBaseline": result["farmgateBaseline"],
            "momentumPercent": result["momentumPercent"],
            "trendDirection": result["trendDirection"],
            "dataSource": result["dataSource"],
            "toolCallLog": result["toolCallLog"]
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Fair-Price Agent error: {str(e)}")


@router.post("/orchestration/run")
def run_orchestration(request: OrchestrationRunRequest):
    """
    LangGraph StateGraph Coordinator execution endpoint.
    Builds structured multi-step plan, routes to agents, validates bounds,
    and returns proposal paused at human-approval checkpoint.
    """
    try:
        final_state = coordinator.run_workflow(
            trigger_type=request.triggerType,
            objective_text=request.objectiveText,
            listing_context=request.listingContext,
            trigger_entity_id=request.triggerEntityId
        )
        return final_state
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Orchestration error: {str(e)}")


@router.get("/market/today-prices")
def get_today_market_prices(
    region: Optional[str] = Query(None, description="Optional region filter"),
    grade: Optional[str] = Query("A", description="Quality grade (A, B, C)")
):
    """
    Returns AI-estimated Today Prices for all main produce types.
    Powers the 'Today Prices' marketplace discovery page.
    """
    items = []
    grade_val = grade.upper() if grade else "A"

    for produce in TODAY_PRODUCE_CATALOG:
        target_region = region if (region and region != "All") else produce["defaultRegion"]
        
        # Estimate fair price
        est = fair_price_agent.estimate_price(
            crop_id=produce["cropId"],
            region_id=target_region,
            quantity=100.0,
            claimed_grade=grade_val,
            crop_name=produce["name"],
            region_name=target_region
        )

        avg_price = round((est["suggestedPriceMin"] + est["suggestedPriceMax"]) / 2, 2)

        items.append({
            "cropId": produce["cropId"],
            "name": produce["name"],
            "category": produce["category"],
            "unit": produce["unit"],
            "region": target_region,
            "grade": grade_val,
            "suggestedPriceMin": est["suggestedPriceMin"],
            "suggestedPriceMax": est["suggestedPriceMax"],
            "averagePrice": avg_price,
            "confidence": est["confidence"],
            "change24h": produce["change24h"],
            "trend": produce["trend"],
            "imageUrl": produce["imageUrl"],
            "reasoning": est["reasoningSummary"],
            "benchmarkWholesale": est["benchmarkWholesale"]
        })

    return {
        "date": "Today",
        "totalCrops": len(items),
        "selectedGrade": grade_val,
        "selectedRegion": region or "All Regions",
        "marketStatus": "Active Trading",
        "items": items
    }


@router.get("/market/estimate")
def get_quick_price_estimate(
    crop: str = Query(..., description="Crop name or ID"),
    region: str = Query(..., description="Region name or ID"),
    grade: str = Query("A", description="Claimed grade"),
    quantity: float = Query(100.0, description="Quantity")
):
    """
    Quick price estimate for live display when farmer is adding a crop.
    """
    result = fair_price_agent.estimate_price(
        crop_id=crop,
        region_id=region,
        quantity=quantity,
        claimed_grade=grade,
        crop_name=crop,
        region_name=region
    )
    return {
        "crop": crop,
        "region": region,
        "grade": grade,
        "suggestedPriceMin": result["suggestedPriceMin"],
        "suggestedPriceMax": result["suggestedPriceMax"],
        "averagePrice": round((result["suggestedPriceMin"] + result["suggestedPriceMax"]) / 2, 2),
        "confidence": result["confidence"],
        "reasoningSummary": result["reasoningSummary"],
        "benchmarkWholesale": result["benchmarkWholesale"]
    }
