import os

import pytest
from fastapi.testclient import TestClient

from src.app.main import app

VALID_PAYLOAD = {
    "order_id": "o1",
    "buyer_location": {"lat": 7.29, "lng": 80.63},
    "listing_id": "l1",
    "requested_quantity": 10,
    "candidate_centres": [
        {
            "centre_id": "c1",
            "name": "Kandy Centre",
            "lat": 7.30,
            "lng": 80.64,
            "capacity": 5,
            "current_confirmed_bookings": 1,
        }
    ],
}


@pytest.fixture(autouse=True)
def _no_internal_secret(monkeypatch):
    # Default: no INTERNAL_API_SECRET configured, endpoint stays open (dev mode).
    monkeypatch.delenv("INTERNAL_API_SECRET", raising=False)


@pytest.fixture()
def client():
    return TestClient(app)


def test_match_endpoint_returns_a_match(client):
    response = client.post("/agents/buyer-farmer-matching", json=VALID_PAYLOAD)

    assert response.status_code == 200
    body = response.json()
    assert body["matched_centre_id"] == "c1"
    assert body["order_id"] == "o1"


def test_match_endpoint_with_no_candidates_returns_no_match(client):
    payload = {**VALID_PAYLOAD, "candidate_centres": []}

    response = client.post("/agents/buyer-farmer-matching", json=payload)

    assert response.status_code == 200
    assert response.json()["matched_centre_id"] is None


def test_match_endpoint_rejects_invalid_quantity(client):
    payload = {**VALID_PAYLOAD, "requested_quantity": -5}

    response = client.post("/agents/buyer-farmer-matching", json=payload)

    assert response.status_code == 422


def test_match_endpoint_requires_correct_internal_api_key_when_configured(client, monkeypatch):
    monkeypatch.setenv("INTERNAL_API_SECRET", "the-real-secret")

    response = client.post("/agents/buyer-farmer-matching", json=VALID_PAYLOAD)

    assert response.status_code == 401


def test_match_endpoint_accepts_correct_internal_api_key(client, monkeypatch):
    monkeypatch.setenv("INTERNAL_API_SECRET", "the-real-secret")

    response = client.post(
        "/agents/buyer-farmer-matching",
        json=VALID_PAYLOAD,
        headers={"X-Internal-Api-Key": "the-real-secret"},
    )

    assert response.status_code == 200
