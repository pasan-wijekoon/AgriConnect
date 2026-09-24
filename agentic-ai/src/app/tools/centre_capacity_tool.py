"""Centre-capacity evaluation, shared between the Buyer-Farmer Matching Agent
(mine) and the Logistics Scheduling Agent (Student 4's) — plan §8.5 flags this
file as shared, coordinate before changing its shape.

Kept intentionally generic: given a centre's total capacity and how many
Confirmed bookings currently overlap the window being considered, report
whether there's room for one more. This mirrors the same "max concurrent
Confirmed bookings whose windows overlap" definition of capacity used in the
backend's SchedulingService (backend/src/services/SchedulingService.cs), so
both sides of the system agree on what "capacity" means (plan §8.3, the
documented default for an otherwise-unspecified DFD column).
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class CapacityCheckResult:
    has_capacity: bool
    remaining_slots: int


class CentreCapacityTool:
    @staticmethod
    def check(capacity: int, current_confirmed_bookings: int) -> CapacityCheckResult:
        remaining = capacity - current_confirmed_bookings
        return CapacityCheckResult(has_capacity=remaining > 0, remaining_slots=max(remaining, 0))
