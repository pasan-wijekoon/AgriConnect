"""Distance estimation for the Buyer-Farmer Matching Agent (plan §8.1).

The agentic AI service never calls back into the ASP.NET Core API (the
architecture diagram, plan §2, only has an API -> Agent arrow), so this is an
independent Python implementation of the same idea as the backend's
DistanceService (backend/src/services/DistanceService.cs): try the Maps API
first, fall back to a local haversine estimate with `degraded=True` on any
failure, and never accept anything but raw coordinates as input (DFD §8
data-minimisation — no buyer/farmer identity or listing data is sent to the
third party).
"""

from __future__ import annotations

import math
import os
from dataclasses import dataclass

import httpx

EARTH_RADIUS_KM = 6371.0


@dataclass(frozen=True)
class DistanceResult:
    distance_km: float
    eta_minutes: float | None
    degraded: bool


def haversine_distance_km(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """Straight-line great-circle distance. Pure function, no I/O."""
    d_lat = math.radians(lat2 - lat1)
    d_lon = math.radians(lon2 - lon1)
    a = (
        math.sin(d_lat / 2) ** 2
        + math.cos(math.radians(lat1)) * math.cos(math.radians(lat2)) * math.sin(d_lon / 2) ** 2
    )
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
    return EARTH_RADIUS_KM * c


class DistanceLookupTool:
    """Wraps a Maps/Distance lookup with the same retry-then-degrade behaviour
    as the backend's DistanceService, so both sides of the system fail the same
    way when the Maps API is unavailable."""

    def __init__(
        self,
        client: httpx.Client | None = None,
        base_url: str | None = None,
        api_key: str | None = None,
        timeout_seconds: float = 5.0,
        max_retries: int = 2,
    ) -> None:
        self._base_url = (base_url or os.getenv("MAPS_API_BASE_URL", "https://api.openrouteservice.org")).rstrip("/")
        self._api_key = api_key if api_key is not None else os.getenv("MAPS_API_KEY", "")
        self._timeout_seconds = timeout_seconds
        self._max_retries = max_retries
        self._client = client

    def get_distance(self, origin_lat: float, origin_lng: float, dest_lat: float, dest_lng: float) -> DistanceResult:
        if self._api_key:
            for attempt in range(self._max_retries + 1):
                try:
                    return self._call_matrix_api(origin_lat, origin_lng, dest_lat, dest_lng)
                except (httpx.HTTPError, ValueError, KeyError, IndexError):
                    if attempt == self._max_retries:
                        break

        distance_km = haversine_distance_km(origin_lat, origin_lng, dest_lat, dest_lng)
        return DistanceResult(distance_km=distance_km, eta_minutes=None, degraded=True)

    def _call_matrix_api(
        self, origin_lat: float, origin_lng: float, dest_lat: float, dest_lng: float
    ) -> DistanceResult:
        payload = {
            "locations": [[origin_lng, origin_lat], [dest_lng, dest_lat]],
            "sources": [0],
            "destinations": [1],
            "metrics": ["distance", "duration"],
            "units": "km",
        }
        headers = {"Authorization": self._api_key}

        client = self._client or httpx.Client(timeout=self._timeout_seconds)
        should_close = self._client is None
        try:
            response = client.post(f"{self._base_url}/v2/matrix/driving-car", json=payload, headers=headers)
            response.raise_for_status()
            body = response.json()
            distance_km = body["distances"][0][0]
            duration_seconds = body.get("durations", [[None]])[0][0]
            eta_minutes = duration_seconds / 60 if duration_seconds is not None else None
            return DistanceResult(distance_km=distance_km, eta_minutes=eta_minutes, degraded=False)
        finally:
            if should_close:
                client.close()
