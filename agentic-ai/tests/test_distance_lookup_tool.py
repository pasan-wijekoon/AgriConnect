import httpx
import pytest

from src.app.tools.distance_lookup_tool import DistanceLookupTool, haversine_distance_km


def test_haversine_distance_km_same_point_is_zero():
    assert haversine_distance_km(7.29, 80.63, 7.29, 80.63) == pytest.approx(0.0, abs=1e-6)


def test_haversine_distance_km_kandy_to_colombo_is_roughly_correct():
    distance = haversine_distance_km(7.2906, 80.6337, 6.9271, 79.8612)
    assert 85 <= distance <= 105


def test_get_distance_without_api_key_falls_back_to_haversine_directly():
    tool = DistanceLookupTool(api_key="")

    result = tool.get_distance(7.2906, 80.6337, 6.9271, 79.8612)

    assert result.degraded is True
    assert result.eta_minutes is None
    assert 85 <= result.distance_km <= 105


def test_get_distance_with_successful_api_response_is_not_degraded():
    def handler(request: httpx.Request) -> httpx.Response:
        return httpx.Response(200, json={"distances": [[42.5]], "durations": [[1800.0]]})

    client = httpx.Client(transport=httpx.MockTransport(handler))
    tool = DistanceLookupTool(client=client, api_key="test-key")

    result = tool.get_distance(7.29, 80.63, 6.93, 79.86)

    assert result.degraded is False
    assert result.distance_km == 42.5
    assert result.eta_minutes == 30.0


def test_get_distance_when_api_always_fails_retries_then_falls_back():
    call_count = 0

    def handler(request: httpx.Request) -> httpx.Response:
        nonlocal call_count
        call_count += 1
        return httpx.Response(503)

    client = httpx.Client(transport=httpx.MockTransport(handler))
    tool = DistanceLookupTool(client=client, api_key="test-key", max_retries=2)

    result = tool.get_distance(7.29, 80.63, 6.93, 79.86)

    assert result.degraded is True
    assert call_count == 3  # initial attempt + 2 retries


def test_get_distance_with_malformed_response_falls_back():
    def handler(request: httpx.Request) -> httpx.Response:
        return httpx.Response(200, json={"distances": []})

    client = httpx.Client(transport=httpx.MockTransport(handler))
    tool = DistanceLookupTool(client=client, api_key="test-key", max_retries=0)

    result = tool.get_distance(7.29, 80.63, 6.93, 79.86)

    assert result.degraded is True
