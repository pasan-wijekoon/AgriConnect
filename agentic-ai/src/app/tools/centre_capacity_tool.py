from src.app.config import Settings
from src.app.tools._http import get_json
from src.app.validation.logistics_schemas import CentreCapacity


class CentreCapacityTool:
    """How many pickup/delivery slots a collection centre can take per day, and how long each is."""

    name = "centre_capacity"
    description = "Input: centreId. Output: { maxDailySlots, slotDurationMinutes }."

    MOCK_CAPACITY = CentreCapacity(maxDailySlots=8, slotDurationMinutes=60)

    def __init__(self, settings: Settings) -> None:
        self._settings = settings

    def run(self, centre_id: str) -> CentreCapacity:
        if self._settings.llm_provider == "mock":
            return self.MOCK_CAPACITY

        # Owned by Component B; path to be confirmed with Student 2.
        data = get_json(self._settings, f"/api/centres/{centre_id}/capacity")
        return CentreCapacity.model_validate(data)
