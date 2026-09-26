"""
Live market data source: Dambulla Dedicated Economic Centre (DEC) public price API.

Dambulla DEC publishes real daily wholesale min/max prices for ~90 vegetable
and fruit lines at https://dambulladec.com/home-dailyprice. Its backend exposes
two public (unauthenticated) endpoints that this module wraps:

  GET /api/prices/by-date/{YYYY-MM-DD}      -> today's/a day's price snapshot
  GET /api/prices/product/{id}/chart        -> full historical time series for one product

Both are best-effort: network failures, rate limits, or an unpublished day
must never crash a price estimate. Every public function here returns None
on failure so callers can fall back to the static heuristic benchmarks.
"""
from __future__ import annotations

import time
from dataclasses import dataclass
from datetime import date, timedelta
from typing import Dict, List, Optional

import httpx

BASE_URL = "https://api.dambulladec.com/api"
REQUEST_TIMEOUT_SECONDS = 4.0
SNAPSHOT_LOOKBACK_DAYS = 10  # how far back to search for the last published market day
SNAPSHOT_CACHE_TTL_SECONDS = 6 * 60 * 60  # 6 hours
HISTORY_CACHE_TTL_SECONDS = 6 * 60 * 60

_HEADERS = {"User-Agent": "AgriConnect-AgenticAI/1.0 (+internal market data client)"}


@dataclass
class SnapshotEntry:
    product_id: int
    name: str
    product_type: Optional[str]
    min_price: float
    max_price: float
    as_of_date: str


@dataclass
class HistoryPoint:
    date: str
    min_price: float
    max_price: float


# ── Tiny in-process TTL caches (no external cache infra needed for this scale) ──
_snapshot_cache: Dict[str, object] = {"expires_at": 0.0, "entries": []}
_history_cache: Dict[int, Dict[str, object]] = {}


def _normalize(text: Optional[str]) -> str:
    if not text:
        return ""
    return text.lower().strip().replace("-", " ").replace("_", " ")


def _fetch_by_date(day: date) -> Optional[List[dict]]:
    try:
        with httpx.Client(timeout=REQUEST_TIMEOUT_SECONDS, headers=_HEADERS) as client:
            resp = client.get(f"{BASE_URL}/prices/by-date/{day.isoformat()}", params={"limit": 200})
            if resp.status_code != 200:
                return None
            data = resp.json().get("data", [])
            return data if isinstance(data, list) else None
    except (httpx.HTTPError, ValueError):
        return None


def get_latest_snapshot() -> List[SnapshotEntry]:
    """
    Returns the most recently published day's price list across all Dambulla DEC
    products, walking backward up to SNAPSHOT_LOOKBACK_DAYS if today isn't published
    yet (the market publishes prices with a delay and skips non-trading days).
    Cached in-process for SNAPSHOT_CACHE_TTL_SECONDS. Returns [] if the live source
    is unreachable or has no recent data — callers must treat that as "no live data".
    """
    now = time.time()
    if now < _snapshot_cache["expires_at"]:
        return _snapshot_cache["entries"]  # type: ignore[return-value]

    entries: List[SnapshotEntry] = []
    today = date.today()
    for offset in range(SNAPSHOT_LOOKBACK_DAYS):
        day = today - timedelta(days=offset)
        records = _fetch_by_date(day)
        if not records:
            continue
        for rec in records:
            product = rec.get("product") or {}
            pid = product.get("id")
            min_p = rec.get("min_price")
            max_p = rec.get("max_price")
            if pid is None or min_p is None or max_p is None:
                continue
            entries.append(SnapshotEntry(
                product_id=pid,
                name=(product.get("name") or "").strip(),
                product_type=product.get("type"),
                min_price=float(min_p),
                max_price=float(max_p),
                as_of_date=rec.get("date") or day.isoformat(),
            ))
        if entries:
            break  # found the most recent published market day

    _snapshot_cache["entries"] = entries
    _snapshot_cache["expires_at"] = now + SNAPSHOT_CACHE_TTL_SECONDS
    return entries


def _singularize(word: str) -> str:
    """Cheap English de-pluralization — good enough for produce names
    ("Carrots" -> "Carrot", "Tomatoes" -> "Tomatoe"[sic, still matches "tomato"
    via substring below], "Potatoes" -> "Potatoe")."""
    if len(word) > 3 and word.endswith("es"):
        return word[:-2]
    if len(word) > 3 and word.endswith("s") and not word.endswith("ss"):
        return word[:-1]
    return word


def _tokens(text: str) -> List[str]:
    return [t for t in text.replace("-", " ").split(" ") if t]


def find_product(crop_name: str) -> Optional[SnapshotEntry]:
    """
    Fuzzy-matches a crop name against the latest live snapshot's product names.
    Dambulla DEC's product names are inconsistent about singular/plural and
    include region qualifiers ("Nuwaraeliya Carrot", "Big Onion Lanka"), so this
    compares individual (de-pluralized) tokens rather than the whole string.
    """
    snapshot = get_latest_snapshot()
    if not snapshot:
        return None

    c_key = _normalize(crop_name)
    if not c_key:
        return None

    if c_key == "":
        return None

    # 1. Exact-ish whole-string match first (cheap win for e.g. "Tomato" == "Tomato").
    for entry in snapshot:
        e_key = _normalize(entry.name)
        if e_key and (e_key == c_key or e_key in c_key or c_key in e_key):
            return entry

    # 2. Token-level match, ignoring plurals: any singularized word in the crop
    # name matching any singularized word in the product name (e.g. "carrots"
    # -> "carrot" matches "Nuwaraeliya Carrot" -> "carrot").
    c_tokens = {_singularize(t) for t in _tokens(c_key)}
    best: Optional[SnapshotEntry] = None
    for entry in snapshot:
        e_key = _normalize(entry.name)
        if not e_key:
            continue
        e_tokens = {_singularize(t) for t in _tokens(e_key)}
        if c_tokens & e_tokens:
            best = entry
            break

    return best


def get_price_history(product_id: int, days: int = 30) -> Optional[List[HistoryPoint]]:
    """
    Fetches the full price history for a Dambulla DEC product and returns the
    most recent `days` calendar days' worth of points, sorted oldest-to-newest.
    Cached per product for HISTORY_CACHE_TTL_SECONDS. Returns None on network
    failure (distinct from "[]", which means the product genuinely has no history).
    """
    now = time.time()
    cached = _history_cache.get(product_id)
    if cached and now < cached["expires_at"]:  # type: ignore[index]
        return cached["points"]  # type: ignore[index]

    try:
        with httpx.Client(timeout=REQUEST_TIMEOUT_SECONDS, headers=_HEADERS) as client:
            resp = client.get(f"{BASE_URL}/prices/product/{product_id}/chart")
            if resp.status_code != 200:
                return None
            raw = resp.json()
            if not isinstance(raw, list):
                return None
    except (httpx.HTTPError, ValueError):
        return None

    points: List[HistoryPoint] = []
    for rec in raw:
        d = rec.get("date")
        min_p = rec.get("min_price")
        max_p = rec.get("max_price")
        if d is None or min_p is None or max_p is None:
            continue
        points.append(HistoryPoint(date=d, min_price=float(min_p), max_price=float(max_p)))

    points.sort(key=lambda p: p.date)

    cutoff = (date.today() - timedelta(days=days)).isoformat()
    recent = [p for p in points if p.date >= cutoff]

    _history_cache[product_id] = {"points": recent, "expires_at": now + HISTORY_CACHE_TTL_SECONDS}
    return recent
