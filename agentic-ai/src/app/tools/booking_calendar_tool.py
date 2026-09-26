from datetime import date, datetime, time, timedelta, timezone

from pydantic import TypeAdapter

from src.app.config import Settings
from src.app.tools._http import get_json
from src.app.validation.logistics_schemas import Booking

# Collection centres are all in Sri Lanka, which has no daylight saving.
CENTRE_TZ = timezone(timedelta(hours=5, minutes=30), "Asia/Colombo")

_bookings = TypeAdapter(list[Booking])


class BookingCalendarTool:
    """Existing pickup/delivery bookings at a collection centre on one day."""

    name = "booking_calendar"
    description = "Input: centreId, date (YYYY-MM-DD). Output: [{ slotStart, slotEnd }]."

    # Mock mode: every centre is closed for lunch, 12:00-13:00 centre time.
    MOCK_DAILY_BOOKINGS = [(time(12, 0), time(13, 0))]

    def __init__(self, settings: Settings) -> None:
        self._settings = settings

    def run(self, centre_id: str, day: date) -> list[Booking]:
        if self._settings.llm_provider == "mock":
            return [
                Booking(
                    slotStart=datetime.combine(day, start, CENTRE_TZ),
                    slotEnd=datetime.combine(day, end, CENTRE_TZ),
                )
                for start, end in self.MOCK_DAILY_BOOKINGS
            ]

        # Owned by Component B (Student 2); not implemented in the API yet.
        data = get_json(
            self._settings,
            "/api/orders/bookings",
            params={"centreId": centre_id, "date": day.isoformat()},
        )
        return _bookings.validate_python(data)
