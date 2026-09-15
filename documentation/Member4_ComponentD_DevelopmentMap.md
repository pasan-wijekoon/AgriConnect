# Development Map — Member 4
## Component D: Market Price Analytics & Reporting

**Owner:** Student 4 · **Branch:** `feature/component/Market_Price_Analytics_and_Reporting`
**SRS coverage:** FR15, FR16, FR17, FR18 · **Agent owned:** Logistics Scheduling Agent

---

## 0. Scope Contract (what "done" means)

| Deliverable | Requirement | Source |
|---|---|---|
| ≥4 meaningful REST endpoints | 6 planned (see §2) | DFD §4 |
| ≥1 business operation beyond CRUD | `GET /api/analytics/anomalies/{listingId}/investigate` | DFD §4.4 |
| 4 DB entities | PriceTrendSnapshot, ShortageOversupplyEvent, PriceAnomalyFlag, ReportExport | DFD §6.3 |
| React surface | Trend charts, shortage heatmap, anomaly queue, export UI | DFD §4.4 |
| Flutter surface | Read-only price-trend view for farmers (intentionally minimal) | DFD §4.4 |
| Agentic AI | Logistics Scheduling Agent + golden-case evals | DFD §4.4, §9 |
| Tests | Trend aggregation, anomaly detection, report export integration, chart tests | DFD §9 |

**Hard architectural rules (do not violate):**
- Flutter and React talk **only** to the ASP.NET Core API — never to PostgreSQL or the AI service.
- The AI service **never writes to the database**; it returns a proposal, the API persists it.
- Every AI proposal waits for an Officer's Approve / Reject / Request Revision before taking effect.

---

## 1. Dependency Map — what you need from other members

| You need | From | Your unblock strategy until it lands |
|---|---|---|
| `Crop`, `Region` reference tables | Shared / Student 1 | Seed them yourself in a migration; coordinate to avoid duplicate migrations |
| `Listing` (Price, AiSuggestedPriceMin/Max, CropId, RegionId, CreatedAt) | Component A / Student 1 | Define a read-only EF entity + seed fixture data |
| `Order`, `CollectionCentre`, booking calendar | Component B / Student 2 | Stub tool responses in the agent; swap to real query later |
| `Inspection` history (for `/investigate`) | Component C / Student 3 | Return a documented empty block until available |
| `User` + JWT roles (Officer, Administrator) | Shared auth | Develop against a dev-mode fake claims principal |

> **Action in week 1:** agree with the team on who owns the shared `AgriConnectDbContext` and the `Crop` / `Region` / `User` migrations. Analytics is read-heavy and depends on everyone — a schema conflict here costs you the most.

---

## 2. API Surface (build in this order)

| # | Method | Route | FR | Roles | Priority |
|---|---|---|---|---|---|
| 1 | GET | `/api/analytics/price-trends?cropId=&regionId=&from=&to=&bucket=week` | FR15 | Officer, Admin | P0 |
| 2 | GET | `/api/analytics/anomalies?status=&cropId=&page=&size=` | FR16 | Officer, Admin | P0 |
| 3 | GET | `/api/analytics/shortages?cropId=&regionId=&type=&severity=` | FR17 | Officer, Admin | P1 |
| 4 | POST | `/api/reports/export` | FR18 | Admin | P1 |
| 5 | GET | `/api/reports/{id}` | FR18 | Admin | P1 |
| 6 | **GET** | **`/api/analytics/anomalies/{listingId}/investigate`** | FR16 | Officer, Admin | **P0 — your graded non-CRUD operation** |

Secondary (supporting, not counted toward the minimum):
- `PATCH /api/analytics/anomalies/{id}` — set Status to `Reviewed` / `Dismissed`
- `POST /api/analytics/snapshots/refresh` — Admin-only manual rebuild of the materialized trend table

---

## 3. Phased Build Plan

### Phase 0 — Foundation (day 1–2)
- [ ] Confirm `backend/src/` layering: `controllers/`, `services/`, `models/`, `dtos/`, `config/`
- [ ] Add EF Core + Npgsql; create or extend `AgriConnectDbContext`
- [ ] Read-only entity mappings for `Crop`, `Region`, `Listing`, `User`
- [ ] Run `docker compose up` locally and confirm Postgres on `localhost:5432`

### Phase 1 — Data layer (day 3–4)
- [ ] Entities: `PriceTrendSnapshot`, `ShortageOversupplyEvent`, `PriceAnomalyFlag`, `ReportExport`
- [ ] Constraints exactly per DFD §6.3:
  - `PriceTrendSnapshot` — **UNIQUE index on (CropId, RegionId, Period)**; `SampleCount CHECK >= 0`
  - `ShortageOversupplyEvent.Type` CHECK IN ('Shortage','Oversupply'); `Severity` CHECK IN ('Low','Medium','High')
  - `PriceAnomalyFlag.Status` CHECK IN ('Open','Reviewed','Dismissed'); `DeviationPercent numeric(5,2)`
  - `ReportExport.FileUrl varchar(500)` NOT NULL
- [ ] Indexes on `PriceAnomalyFlag.ListingId` and `PriceTrendSnapshot.(CropId, RegionId)`
- [ ] Migration + seed data: ≥12 weeks of synthetic price history across ≥3 crops × ≥2 regions — you cannot demo a trend chart without it

### Phase 2 — Services / business logic (day 5–8) ← **the graded core**
- [ ] `TrendAggregationService`
  - Buckets published listings by crop + region + period (week-start `date`)
  - Computes Avg / Min / Max / SampleCount; **upserts** into the snapshot table (idempotent, safe to re-run)
  - Handles empty buckets explicitly — return a gap, do not fabricate zeros
- [ ] `AnomalyDetectionService`
  - `DeviationPercent = (ListingPrice − AiSuggestedMidpoint) / AiSuggestedMidpoint × 100`
  - Flags when `|deviation|` exceeds a **configurable** threshold (default 25%) — put it in `appsettings.json`, not a magic number
  - Idempotent: one Open flag per listing, no duplicates on re-run
- [ ] `ShortageDetectionService`
  - "Recurring" = N consecutive periods where supply sits below or above a rolling baseline
  - Severity Low/Medium/High derived from distance from that baseline
- [ ] `AnomalyInvestigationService` ← **your non-CRUD operation**
  - Composes: the flag + price-trend context for that crop/region + inspection history (Component C) + order history (Component B)
  - Returns a structured verdict — a ranked likely-cause explanation, not a raw data dump
- [ ] `ReportExportService`
  - Generates CSV (start here) for listings / orders / price-trends over a date range
  - Persists a `ReportExport` row with `FileUrl`, `GeneratedAt`, `RequestedBy`

### Phase 3 — Controllers + DTOs (day 9–10)
- [ ] `AnalyticsController`, `ReportsController`
- [ ] `[Authorize(Roles = "Officer,Administrator")]` on analytics; `Administrator` only on reports
- [ ] Request DTOs with validation: date range required and `from <= to`, page size capped (e.g. ≤100)
- [ ] Response DTOs — never return EF entities directly
- [ ] Consistent error shape (ProblemDetails): 400 on bad range, 404 on unknown report id
- [ ] Swagger annotations; add the requests to `backend/backend.http`

### Phase 4 — React dashboard (day 11–14)
`web/src/pages/analytics/` + `web/src/components/charts/`
- [ ] `PriceTrendsPage` — line chart, crop/region/period filters, empty + loading + error states
- [ ] `ShortagesPage` — crop × region heatmap or severity-grouped table
- [ ] `AnomalyQueuePage` — paginated review queue, row → investigate drawer, Reviewed/Dismissed actions
- [ ] `ReportsPage` — date range + type picker, generate, download, list of past exports
- [ ] Typed API client in `web/src/utils/`; reuse the shared auth context in `web/src/context/`

### Phase 5 — Flutter read-only view (day 15–16)
`mobile/lib/screens/price_trends/`
- [ ] One screen: crop picker → line chart + "current average" figure
- [ ] `PriceTrendService` in `mobile/lib/services/`, model in `mobile/lib/models/`, provider in `mobile/lib/providers/`
- [ ] Keep it deliberately minimal — React is the primary analytics surface

### Phase 6 — Logistics Scheduling Agent (day 17–20)
`agentic-ai/src/app/agents/logistics_scheduling_agent.py`
- [ ] Input contract: `{ orderId, centreId, preferredWindow, existingBookings[] }`
- [ ] Output contract: `{ proposedSlotStart, proposedSlotEnd, conflictChecked: bool }`
- [ ] Allow-listed tools only: `CentreCapacityTool`, `BookingCalendarTool` (in `tools/`)
- [ ] Schema validation in `validation/` — reject malformed LLM output, never persist raw text
- [ ] Register in the LangGraph coordinator in `orchestration/`; log plan + tool calls to `AgentWorkflow`
- [ ] Output feeds Component B's `/api/orders/{id}/schedule` — **agree that contract with Student 2 early**
- [ ] Must work with `LLM_PROVIDER=mock` so the demo never depends on a live API key

> **Viva prep:** you must be able to explain and modify this agent's use of Component B's scheduling model (DFD §4.4 note, ADR decision 6).

### Phase 7 — Testing (run continuously, harden day 21–23)

| Layer | What to test |
|---|---|
| Backend unit | Trend bucketing incl. week boundaries; anomaly threshold at exactly 25%; empty and one-sample buckets; severity classification |
| Backend integration | Report export end-to-end; role rejection (Officer on an Admin-only route → 403); invalid date range → 400 |
| React | Chart renders with data; empty state; anomaly queue pagination; investigate drawer |
| Flutter | Read-only trend view widget test |
| Agent | Golden cases: valid slot found, no slot available, conflicting bookings, malformed LLM output rejected |

Aim for meaningful branch coverage on the three detection services — that is where the marks are.

### Phase 8 — Docs, CI, demo (day 24–25)
- [ ] Populate `.github/workflows/` backend + frontend jobs (currently scaffolded and empty)
- [ ] Add your component's section to the docs; confirm the DFD traceability matrix still matches what you built
- [ ] Record the end-to-end demo path: Flutter trend view → API → Postgres → agent → React approval

---

## 4. Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| Component A's `Listing` schema lands late or differs | Blocks aggregation + anomaly detection | Code against an interface; seed your own fixtures now |
| No realistic price history | Charts look broken at demo | Write the seeder in Phase 1, not at the end |
| Migration conflicts with teammates | Painful merges | Agree migration ownership and naming in week 1 |
| Agent depends on Component B bookings | Agent can't be demoed | Ship with a stubbed `BookingCalendarTool`, swap later |
| Report file storage undefined | `FileUrl` has nothing to point at | Start with an API-served CSV download; object storage only if time allows |

---

## 5. Commit Slices (one PR per slice keeps review sane)

1. `feat(analytics): add Component D entities and migration`
2. `feat(analytics): seed price history fixtures`
3. `feat(analytics): trend aggregation service + tests`
4. `feat(analytics): anomaly detection service + tests`
5. `feat(analytics): shortage/oversupply detection + tests`
6. `feat(analytics): price-trends and anomalies endpoints`
7. `feat(analytics): anomaly investigation endpoint`
8. `feat(reports): report export service and endpoints`
9. `feat(web): analytics dashboard pages`
10. `feat(mobile): read-only price trend screen`
11. `feat(ai): logistics scheduling agent + golden cases`
12. `chore(ci): backend and frontend workflows`
