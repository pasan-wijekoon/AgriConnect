from src.app.agents.buyer_farmer_matching_agent import BuyerFarmerMatchingAgent, CandidateCentre
from src.app.tools.distance_lookup_tool import DistanceLookupTool, DistanceResult


class StubDistanceTool(DistanceLookupTool):
    """Returns a caller-supplied distance for each destination, keyed by
    centre lat/lng, so matching-order tests are deterministic without
    depending on the real haversine formula."""

    def __init__(self, distances_by_dest: dict[tuple[float, float], float], degraded: bool = False):
        self._distances = distances_by_dest
        self._degraded = degraded

    def get_distance(self, origin_lat, origin_lng, dest_lat, dest_lng):
        return DistanceResult(distance_km=self._distances[(dest_lat, dest_lng)], eta_minutes=None, degraded=self._degraded)


def make_centre(centre_id, name="Centre", lat=0.0, lng=0.0, capacity=3, current_confirmed_bookings=0):
    return CandidateCentre(
        centre_id=centre_id, name=name, lat=lat, lng=lng,
        capacity=capacity, current_confirmed_bookings=current_confirmed_bookings,
    )


def test_match_with_no_candidates_returns_no_match():
    agent = BuyerFarmerMatchingAgent(distance_tool=StubDistanceTool({}))

    result = agent.match(order_id="o1", buyer_lat=0, buyer_lng=0, candidate_centres=[])

    assert result.matched_centre_id is None
    assert result.match_confidence == 0.0
    assert result.candidates_considered == 0


def test_match_picks_the_closest_centre_with_capacity():
    centres = [
        make_centre("far", lat=10, lng=10),
        make_centre("near", lat=1, lng=1),
    ]
    distances = {(10, 10): 40.0, (1, 1): 2.0}
    agent = BuyerFarmerMatchingAgent(distance_tool=StubDistanceTool(distances))

    result = agent.match(order_id="o1", buyer_lat=0, buyer_lng=0, candidate_centres=centres)

    assert result.matched_centre_id == "near"
    assert result.candidates_considered == 2


def test_match_skips_centres_at_full_capacity():
    centres = [
        make_centre("full", lat=1, lng=1, capacity=2, current_confirmed_bookings=2),
        make_centre("available", lat=5, lng=5, capacity=2, current_confirmed_bookings=1),
    ]
    distances = {(1, 1): 1.0, (5, 5): 5.0}
    agent = BuyerFarmerMatchingAgent(distance_tool=StubDistanceTool(distances))

    result = agent.match(order_id="o1", buyer_lat=0, buyer_lng=0, candidate_centres=centres)

    assert result.matched_centre_id == "available"


def test_match_when_all_centres_full_returns_no_match_with_explanatory_notes():
    centres = [make_centre("full", capacity=1, current_confirmed_bookings=1)]
    agent = BuyerFarmerMatchingAgent(distance_tool=StubDistanceTool({}))

    result = agent.match(order_id="o1", buyer_lat=0, buyer_lng=0, candidate_centres=centres)

    assert result.matched_centre_id is None
    assert result.match_confidence == 0.0
    assert "capacity" in result.notes.lower()


def test_match_confidence_is_within_bounds_and_decreases_with_distance():
    close_centre = make_centre("close", lat=1, lng=1)
    far_centre = make_centre("far", lat=2, lng=2)

    close_result = BuyerFarmerMatchingAgent(
        distance_tool=StubDistanceTool({(1, 1): 1.0})
    ).match(order_id="o1", buyer_lat=0, buyer_lng=0, candidate_centres=[close_centre])

    far_result = BuyerFarmerMatchingAgent(
        distance_tool=StubDistanceTool({(2, 2): 45.0})
    ).match(order_id="o2", buyer_lat=0, buyer_lng=0, candidate_centres=[far_centre])

    assert 0.0 <= far_result.match_confidence <= close_result.match_confidence <= 1.0


def test_match_propagates_degraded_flag_from_distance_tool():
    centre = make_centre("c1", lat=1, lng=1)
    agent = BuyerFarmerMatchingAgent(distance_tool=StubDistanceTool({(1, 1): 5.0}, degraded=True))

    result = agent.match(order_id="o1", buyer_lat=0, buyer_lng=0, candidate_centres=[centre])

    assert result.degraded is True
    assert "approximate" in result.notes.lower()
