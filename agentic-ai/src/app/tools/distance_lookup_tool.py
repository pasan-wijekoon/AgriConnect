"""Distance estimation for the Buyer-Farmer Matching Agent (plan §8.1).

The agentic AI service never calls back into the ASP.NET Core API (the
architecture diagram, plan §2, only has an API -> Agent arrow), so this is an
independent Python implementation of the same idea as the backend's
DistanceService (backend/src/services/DistanceService.cs): try the Maps API
first, fall back to a local haversine estimate with `degraded=True` on any
failure, and never accept anything but raw coordinates as input (DFD §8
data-minimisation — no buyer/farmer identity or listing data is sent to the
third party).

Uses OSRM's public Table API (router.project-osrm.org) rather than
OpenRouteService — free, unlimited, and needs no API key at all. `api_key`/
`MAPS_API_KEY` stay supported (sent as an Authorization header when present)
for a self-hosted OSRM instance or a different provider, but the public demo
server needs none — unlike the previous provider, the live call is always
attempted regardless of whether a key is configured.
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
        self._base_url = (base_url or os.getenv("MAPS_API_BASE_URL", "https://router.project-osrm.org")).rstrip("/")
        self._api_key = api_key if api_key is not None else os.getenv("MAPS_API_KEY", "")
        self._timeout_seconds = timeout_seconds
        self._max_retries = max_retries
        self._client = client

    def get_distance(self, origin_lat: float, origin_lng: float, dest_lat: float, dest_lng: float) -> DistanceResult:
        for attempt in range(self._max_retries + 1):
            try:
                return self._call_table_api(origin_lat, origin_lng, dest_lat, dest_lng)
            except (httpx.HTTPError, ValueError, KeyError, IndexError):
                if attempt == self._max_retries:
                    break

        distance_km = haversine_distance_km(origin_lat, origin_lng, dest_lat, dest_lng)
        return DistanceResult(distance_km=distance_km, eta_minutes=None, degraded=True)

    def _call_table_api(
        self, origin_lat: float, origin_lng: float, dest_lat: float, dest_lng: float
    ) -> DistanceResult:
        # OSRM's Table service: GET /table/v1/{profile}/{lng,lat;lng,lat;...}
        # — coordinates in the URL path (lng,lat order), sources/destinations
        # as 0-based indexes into that coordinate list.
        coords = f"{origin_lng},{origin_lat};{dest_lng},{dest_lat}"
        url = f"{self._base_url}/table/v1/driving/{coords}"
        params = {"sources": "0", "destinations": "1", "annotations": "distance,duration"}
        headers = {"Authorization": self._api_key} if self._api_key else {}

        client = self._client or httpx.Client(timeout=self._timeout_seconds)
        should_close = self._client is None
        try:
            response = client.get(url, params=params, headers=headers)
            response.raise_for_status()
            body = response.json()
            if body.get("code") != "Ok":
                raise ValueError(f"OSRM table request did not succeed (code: {body.get('code')}).")

            distance_meters = body["distances"][0][0]
            duration_seconds = body.get("durations", [[None]])[0][0]
            eta_minutes = duration_seconds / 60 if duration_seconds is not None else None
            return DistanceResult(distance_km=distance_meters / 1000, eta_minutes=eta_minutes, degraded=False)
        finally:
            if should_close:
                client.close()
