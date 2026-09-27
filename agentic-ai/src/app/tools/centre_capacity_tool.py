"""Centre-capacity evaluation, shared between the Buyer-Farmer Matching Agent
(Component B's) and the Logistics Scheduling Agent (Student 4's) — plan §8.5
flags this file as shared, coordinate before changing its shape.

Both agents independently added a `CentreCapacityTool` here and, on merging,
turned out to need genuinely different call shapes:
  - Buyer-Farmer Matching Agent: a pure static check, given a centre's total
    capacity and how many Confirmed bookings currently overlap the window
    being considered — mirrors the "max concurrent Confirmed bookings whose
    windows overlap" definition of capacity used in the backend's
    SchedulingService (backend/src/services/SchedulingService.cs), so both
    sides of the system agree on what "capacity" means (plan §8.3).
  - Logistics Scheduling Agent: an instance tool (settings-configured, mockable)
    that looks up a centre's daily slot capacity from the backend by centre id.
Rather than pick one and break the other agent's already-tested code, this
file keeps both APIs on the one shared class.
"""

from __future__ import annotations

from dataclasses import dataclass

from src.app.config import Settings
from src.app.tools._http import get_json
from src.app.validation.logistics_schemas import CentreCapacity


@dataclass(frozen=True)
class CapacityCheckResult:
    has_capacity: bool
    remaining_slots: int


class CentreCapacityTool:
    """How many pickup/delivery slots a collection centre can take per day, and how long each is."""

    name = "centre_capacity"
    description = "Input: centreId. Output: { maxDailySlots, slotDurationMinutes }."

    MOCK_CAPACITY = CentreCapacity(maxDailySlots=8, slotDurationMinutes=60)

    def __init__(self, settings: Settings | None = None) -> None:
        self._settings = settings

    def run(self, centre_id: str) -> CentreCapacity:
        if self._settings is None or self._settings.use_mock_tools:
            return self.MOCK_CAPACITY

        # Owned by Component B; path to be confirmed with Student 2.
        data = get_json(self._settings, f"/api/centres/{centre_id}/capacity")
        return CentreCapacity.model_validate(data)

    @staticmethod
    def check(capacity: int, current_confirmed_bookings: int) -> CapacityCheckResult:
        remaining = capacity - current_confirmed_bookings
        return CapacityCheckResult(has_capacity=remaining > 0, remaining_slots=max(remaining, 0))
