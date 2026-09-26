# AgriConnect — Agentic AI Service

Internal FastAPI service that runs AgriConnect's LangGraph agents. Only the ASP.NET Core API
calls it; it never writes to the database, and every proposal it returns waits for an
officer's approval.

## Setup

```bash
uv sync --extra dev
```

```bash
cp .env.example .env
```

`LLM_PROVIDER=mock` (the default) needs no API key and makes no LLM calls — use it for
development, tests and demos.

## Run

```bash
uv run uvicorn src.app.main:app --reload --port 8000
```

API docs: http://localhost:8000/docs

## Test

```bash
uv run pytest
```

## Agents

### Logistics Scheduling Agent — `POST /agents/logistics/schedule`

Owner: Student 4 (Component D). Finds a conflict-free pickup/delivery slot at a collection
centre; its output feeds Component B's `/api/orders/{id}/schedule`.

```json
{
  "orderId": "ORD-42",
  "centreId": "CC-DAMBULLA",
  "preferredWindow": { "start": "2026-10-01T08:00:00+05:30", "end": "2026-10-01T17:00:00+05:30" },
  "existingBookings": [{ "slotStart": "2026-10-01T08:00:00+05:30", "slotEnd": "2026-10-01T09:00:00+05:30" }]
}
```

Returns `{ proposedSlotStart, proposedSlotEnd, conflictChecked, reasoning }`.

| Status | Meaning |
|---|---|
| 200 | Slot proposed |
| 409 | No free slot in the window on the preferred day or the next 3 days |
| 422 | Invalid request (e.g. missing `orderId`, timestamp without a timezone offset) |
| 502 | A tool's data source failed, or the LLM's reply failed validation |

**How it decides.** A deterministic LangGraph scheduler picks the slot: it reads the centre's
capacity, merges the caller's bookings with the booking calendar, skips days that are at
capacity, and takes the earliest gap that fits. The LLM, when enabled, only writes the
explanation — its reply must be valid JSON that repeats the chosen slot exactly, or it is
rejected. Raw LLM text is never returned.

**Tools** (`src/app/tools/`): `CentreCapacityTool` and `BookingCalendarTool`. In mock mode they
return fixed data (8 × 60-minute slots per day, lunch booked 12:00–13:00); otherwise they call
the ASP.NET Core API at `BACKEND_API_BASE_URL`. Those endpoints belong to Component B and do
not exist yet.
