"""
Fair-Price Estimation Agent (Component A).
Cross-references recent market prices for the same crop/region, applies
grade, volume, and momentum adjustments, and proposes a grounded fair price range.
"""
from typing import Dict, Any, List, Optional
from datetime import datetime, timezone
from ..tools.market_price_tool import MarketPriceLookupTool
from ..tools.historical_trend_tool import HistoricalTrendTool
from .llm_reasoning import generate_reasoning


class FairPriceEstimationAgent:
    """
    Component A: Fair-Price Estimation Agent.
    Input contract: { cropId, regionId, quantity, claimedGrade, recentSaleData[] }
    Output contract: { suggestedPriceMin, suggestedPriceMax, confidence, reasoningSummary }
    Allow-listed tools: MarketPriceLookupTool, HistoricalTrendTool
    """

    def __init__(self):
        self.market_tool = MarketPriceLookupTool()
        self.trend_tool = HistoricalTrendTool()

    def estimate_price(
        self,
        crop_id: str,
        region_id: str,
        quantity: float,
        claimed_grade: str,
        recent_sale_data: Optional[List[Any]] = None,
        crop_name: Optional[str] = None,
        region_name: Optional[str] = None
    ) -> Dict[str, Any]:
        tool_call_log: List[Dict[str, Any]] = []

        eff_crop_name = crop_name or crop_id
        eff_region_name = region_name or region_id

        # 1. Execute Allow-listed Tool 1: MarketPriceLookupTool
        market_res = self.market_tool.run(
            crop_name=eff_crop_name,
            region_name=eff_region_name,
            recent_sales=recent_sale_data
        )
        tool_call_log.append({
            "tool": self.market_tool.name,
            "input": {"crop": eff_crop_name, "region": eff_region_name, "recent_sales_count": len(recent_sale_data or [])},
            "output": market_res,
            "timestamp": datetime.now(timezone.utc).isoformat()
        })

        wholesale_benchmark = market_res["wholesale_benchmark_lkr"]
        farmgate_baseline = market_res["farmgate_baseline_lkr"]

        # 2. Execute Allow-listed Tool 2: HistoricalTrendTool
        trend_res = self.trend_tool.run(
            crop_name=eff_crop_name,
            region_name=eff_region_name,
            current_benchmark_lkr=wholesale_benchmark
        )
        tool_call_log.append({
            "tool": self.trend_tool.name,
            "input": {"crop": eff_crop_name, "region": eff_region_name, "benchmark": wholesale_benchmark},
            "output": trend_res,
            "timestamp": datetime.now(timezone.utc).isoformat()
        })

        momentum_pct = trend_res["momentum_7d_percent"]
        volatility = trend_res["volatility"]

        # 3. Grade Multiplier
        grade_norm = claimed_grade.upper().strip()
        if grade_norm == "A":
            grade_mult = 1.15
            grade_desc = "Grade A quality premium (+15%)"
        elif grade_norm == "C":
            grade_mult = 0.85
            grade_desc = "Grade C standard discount (-15%)"
        else:
            grade_mult = 1.00
            grade_desc = "Grade B commercial baseline (standard)"

        # 4. Quantity Volume Scale Adjustment
        if quantity >= 1000:
            volume_adj = 0.96  # 4% bulk efficiency discount
            vol_desc = f"Bulk commercial lot ({quantity:,.0f} kg, 4% volume discount applied)"
        elif quantity >= 300:
            volume_adj = 0.98  # 2% volume efficiency
            vol_desc = f"Wholesale quantity ({quantity:,.0f} kg, 2% volume discount applied)"
        else:
            volume_adj = 1.00
            vol_desc = f"Standard harvest parcel ({quantity:,.0f} kg)"

        # 5. Momentum buffer
        momentum_adj = 1.0 + (momentum_pct / 100.0 * 0.4)

        # 6. Calculate Anchored Fair Price Range
        target_center = farmgate_baseline * grade_mult * volume_adj * momentum_adj
        
        # Range spread ±7% for low volatility, ±10% for medium, ±12% for high volatility
        spread_pct = 0.12 if "High" in volatility else (0.07 if "Low" in volatility else 0.09)
        suggested_min = round(target_center * (1.0 - spread_pct), 2)
        suggested_max = round(target_center * (1.0 + spread_pct), 2)

        # Confidence calculation based on market data depth, volatility, and
        # whether the estimate is grounded in live Dambulla DEC data or a
        # static offline heuristic (live data earns higher confidence).
        sample_count = market_res["sample_records_analyzed"]
        data_source = trend_res.get("data_source") or market_res.get("data_source", "category_heuristic")
        is_live = data_source == "dambulla_dec_live"

        base_conf = (0.75 if is_live else 0.55) + min(0.20, sample_count * 0.01)
        if volatility == "Low":
            confidence = round(min(0.97 if is_live else 0.85, base_conf + 0.06), 2)
        elif "High" in volatility:
            confidence = round(max(0.72 if is_live else 0.50, base_conf - 0.05), 2)
        else:
            confidence = round(min(0.93 if is_live else 0.75, base_conf), 2)

        # 7. Construct Reasoning Summary (LLM-phrased when configured, otherwise
        # a deterministic template — see llm_reasoning.py). The numbers above are
        # always computed deterministically regardless of which path writes them up.
        reasoning = generate_reasoning({
            "crop": eff_crop_name,
            "region": eff_region_name,
            "market": market_res["primary_wholesale_market"],
            "wholesale": wholesale_benchmark,
            "farmgate": farmgate_baseline,
            "grade": grade_norm,
            "grade_desc": grade_desc,
            "vol_desc": vol_desc,
            "momentum_pct": momentum_pct,
            "trend_direction": trend_res["trend_direction"],
            "data_source": data_source,
            "suggested_min": suggested_min,
            "suggested_max": suggested_max,
            "confidence_pct": int(confidence * 100),
        })

        return {
            "suggestedPriceMin": suggested_min,
            "suggestedPriceMax": suggested_max,
            "confidence": confidence,
            "reasoningSummary": reasoning,
            # Supporting metadata for auditing and coordinator verification
            "benchmarkWholesale": wholesale_benchmark,
            "farmgateBaseline": farmgate_baseline,
            "historicalMa7d": trend_res["moving_average_7d_lkr"],
            "dataSource": data_source,
            "momentumPercent": momentum_pct,
            "trendDirection": trend_res["trend_direction"],
            "toolCallLog": tool_call_log
        }
