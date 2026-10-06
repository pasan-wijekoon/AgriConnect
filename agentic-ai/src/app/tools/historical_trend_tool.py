"""
Historical Trend Tool (Read-only, scoped to Crop & Region).
Primary source: real historical daily price points from Dambulla DEC
(see dambulla_source.py), from which genuine 7/14/30-day moving averages and
momentum are computed. Falls back to a category-based heuristic only when no
live product/history exists for the crop (e.g. rice, tea, cinnamon — staples
not traded at Dambulla DEC — or the live API is unreachable).
"""
from typing import Dict, Any, List, Optional
from datetime import datetime, timezone

from . import dambulla_source


def _moving_average(points: List[dambulla_source.HistoryPoint], window_days: int, latest_date: str) -> Optional[float]:
    cutoff = _shift_iso_date(latest_date, -window_days)
    window = [p for p in points if cutoff <= p.date <= latest_date]
    if not window:
        return None
    avg = sum((p.min_price + p.max_price) / 2 for p in window) / len(window)
    return round(avg, 2)


def _shift_iso_date(iso_date: str, days: int) -> str:
    from datetime import date, timedelta
    y, m, d = (int(x) for x in iso_date.split("-"))
    return (date(y, m, d) + timedelta(days=days)).isoformat()


def _heuristic_trend(crop_name: str, current_benchmark_lkr: float) -> Dict[str, Any]:
    """Category-based fallback used only when no real history is available."""
    crop_lower = crop_name.lower().strip()

    if any(k in crop_lower for k in ["tomato", "chili", "carrot", "leek", "cabbage"]):
        momentum_pct = 3.8
        volatility = "Medium-High"
        ma_7d = round(current_benchmark_lkr * 0.97, 2)
        ma_14d = round(current_benchmark_lkr * 0.94, 2)
        ma_30d = round(current_benchmark_lkr * 0.91, 2)
        seasonal_factor = 1.05
    elif any(k in crop_lower for k in ["rice", "tea", "coconut"]):
        momentum_pct = 0.5
        volatility = "Low"
        ma_7d = round(current_benchmark_lkr * 0.99, 2)
        ma_14d = round(current_benchmark_lkr * 0.995, 2)
        ma_30d = round(current_benchmark_lkr * 0.98, 2)
        seasonal_factor = 1.01
    else:
        momentum_pct = 1.8
        volatility = "Medium"
        ma_7d = round(current_benchmark_lkr * 0.98, 2)
        ma_14d = round(current_benchmark_lkr * 0.96, 2)
        ma_30d = round(current_benchmark_lkr * 0.94, 2)
        seasonal_factor = 1.02

    return {
        "moving_average_7d_lkr": ma_7d,
        "moving_average_14d_lkr": ma_14d,
        "moving_average_30d_lkr": ma_30d,
        "momentum_7d_percent": momentum_pct,
        "volatility": volatility,
        "seasonal_factor": seasonal_factor,
        "data_source": "category_heuristic",
    }


class HistoricalTrendTool:
    """Allow-listed read-only tool to look up historical trends and momentum."""

    name = "HistoricalTrendTool"
    description = "Analyzes 7-day, 14-day, and 30-day historical moving averages, seasonal variance, and price momentum for a given crop and region."

    @staticmethod
    def run(
        crop_name: str,
        region_name: str,
        current_benchmark_lkr: float
    ) -> Dict[str, Any]:
        """
        Calculates moving averages and volatility for fair price anchoring, from
        real Dambulla DEC history when available, otherwise a labeled heuristic.
        """
        live_entry = dambulla_source.find_product(crop_name)
        history = dambulla_source.get_price_history(live_entry.product_id, days=30) if live_entry else None

        if live_entry is not None and history:
            latest_date = history[-1].date
            latest_mid = (history[-1].min_price + history[-1].max_price) / 2

            ma_7d = _moving_average(history, 7, latest_date) or current_benchmark_lkr
            ma_14d = _moving_average(history, 14, latest_date) or ma_7d
            ma_30d = _moving_average(history, 30, latest_date) or ma_14d

            momentum_pct = round(((latest_mid - ma_7d) / ma_7d) * 100, 2) if ma_7d else 0.0

            # Volatility from the coefficient of variation of the last 14 days of midpoints
            recent_mids = [
                (p.min_price + p.max_price) / 2 for p in history
                if p.date >= _shift_iso_date(latest_date, -14)
            ]
            if len(recent_mids) >= 3:
                mean_mid = sum(recent_mids) / len(recent_mids)
                variance = sum((m - mean_mid) ** 2 for m in recent_mids) / len(recent_mids)
                cv = (variance ** 0.5) / mean_mid if mean_mid else 0.0
                volatility = "High" if cv > 0.12 else ("Medium-High" if cv > 0.07 else ("Medium" if cv > 0.03 else "Low"))
            else:
                volatility = "Medium"

            trend_direction = "Rising" if momentum_pct > 1.5 else ("Falling" if momentum_pct < -1.5 else "Stable")

            return {
                "crop": crop_name,
                "region": region_name,
                "moving_average_7d_lkr": ma_7d,
                "moving_average_14d_lkr": ma_14d,
                "moving_average_30d_lkr": ma_30d,
                "momentum_7d_percent": momentum_pct,
                "trend_direction": trend_direction,
                "volatility": volatility,
                "seasonal_factor": 1.0,
                "data_source": "dambulla_dec_live",
                "data_points_used": len(history),
                "as_of_date": latest_date,
                "analysis_timestamp": datetime.now(timezone.utc).isoformat()
            }

        # No live history for this crop (e.g. rice/tea/cinnamon) or API unreachable
        fallback = _heuristic_trend(crop_name, current_benchmark_lkr)
        trend_direction = "Rising" if fallback["momentum_7d_percent"] > 1.5 else (
            "Falling" if fallback["momentum_7d_percent"] < -1.5 else "Stable"
        )
        return {
            "crop": crop_name,
            "region": region_name,
            **fallback,
            "trend_direction": trend_direction,
            "analysis_timestamp": datetime.now(timezone.utc).isoformat()
        }
