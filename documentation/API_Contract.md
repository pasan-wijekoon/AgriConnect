# API Contract — Component D

**Market Price Analytics & Reporting** · Owner: Student 4 · SRS: FR15–FR18

This document is a **two-way contract**:

- **Part 1** — what Component D will expose, so Members 1–3 can build against it before it exists.
- **Part 2** — what Component D needs from Components A, B, and C.

> **Status:** schema is implemented and migrated. Endpoints are **specified but not yet built**. Shapes here are the commitment — if one has to change, it gets announced to the team rather than changed silently.

---

## Conventions

| Aspect | Rule |
|---|---|
| Base path | `/api` |
| Auth | JWT bearer token, required on every route |
| Content type | `application/json` |
| Dates | `date` (ISO `YYYY-MM-DD`) for periods and ranges; `date-time` (ISO 8601 with offset) for timestamps |
| Money | `decimal`, 2 decimal places, LKR |
| IDs | `Guid` (UUID v4) |
| Errors | RFC 7807 `ProblemDetails` |
| Paging | `page` (1-based) and `size` (default 20, max 100) |

### Error shape

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Invalid date range",
  "status": 400,
  "detail": "'from' must be earlier than or equal to 'to'.",
  "instance": "/api/analytics/price-trends"
}
```

### Status codes

| Code | Meaning |
|---|---|
| 200 | Success |
| 202 | Accepted — report generation started |
| 400 | Validation failed (bad date range, page size over 100) |
| 401 | Missing or invalid token |
| 403 | Authenticated but wrong role |
| 404 | Resource not found |

---

# Part 1 — Endpoints Component D Exposes

## 1. `GET /api/analytics/price-trends`

Historical price trends per crop and region. **FR15** · Roles: `Officer`, `Administrator`

**Query parameters**

| Name | Type | Required | Notes |
|---|---|---|---|
| `cropId` | Guid | yes | |
| `regionId` | Guid | no | Omit for all regions |
| `from` | date | yes | |
| `to` | date | yes | Must be `>= from` |
| `bucket` | enum | no | `week` (default) or `month` |

**200 response**

```json
{
  "cropId": "3f2a...",
  "regionId": "8b1c...",
  "bucket": "week",
  "points": [
    {
      "period": "2026-09-07",
      "avgPrice": 182.50,
      "minPrice": 160.00,
      "maxPrice": 210.00,
      "sampleCount": 14
    }
  ]
}
```

> **Gaps are explicit.** A period with no listings is **omitted** from `points` — it is never returned as `avgPrice: 0`. A zero would render as a price crash on a chart. Consumers must handle missing periods rather than assuming a continuous series.

---

## 2. `GET /api/analytics/anomalies`

Listings priced significantly away from the AI-suggested fair range. **FR16** · Roles: `Officer`, `Administrator`

**Query parameters**

| Name | Type | Required | Notes |
|---|---|---|---|
| `status` | enum | no | `Open` (default), `Reviewed`, `Dismissed` |
| `cropId` | Guid | no | |
| `page` | int | no | 1-based, default 1 |
| `size` | int | no | Default 20, max 100 |

**200 response**

```json
{
  "items": [
    {
      "id": "c4d5...",
      "listingId": "9a8b...",
      "deviationPercent": 42.75,
      "flaggedAt": "2026-09-18T09:14:22+05:30",
      "status": "Open"
    }
  ],
  "page": 1,
  "size": 20,
  "total": 37
}
```

`deviationPercent` is **signed** — positive means priced above the AI suggestion, negative below.

---

## 3. `GET /api/analytics/anomalies/{listingId}/investigate`

Explains *why* a listing was flagged, cross-referencing price history, inspection records, and order activity. **FR16** · Roles: `Officer`, `Administrator`

This is the component's business-specific operation — a ranked interpretation, not a data dump.

**200 response**

```json
{
  "listingId": "9a8b...",
  "flag": {
    "deviationPercent": 42.75,
    "flaggedAt": "2026-09-18T09:14:22+05:30",
    "status": "Open"
  },
  "priceContext": {
    "regionalAvgPrice": 182.50,
    "listingPrice": 260.00,
    "percentileInRegion": 97
  },
  "inspectionContext": {
    "available": false,
    "reason": "Component C inspection data not yet integrated"
  },
  "orderContext": {
    "available": false,
    "reason": "Component B order data not yet integrated"
  },
  "likelyCauses": [
    {
      "cause": "PremiumQualityGrade",
      "confidence": "Medium",
      "explanation": "Listing claims Grade A while the regional average mixes all grades."
    }
  ]
}
```

> `inspectionContext` and `orderContext` return `available: false` with a stated `reason` until Components B and C land. The keys are present from day one so consumers do not need to change shape later.

---

## 4. `PATCH /api/analytics/anomalies/{id}`

Officer triage action. Roles: `Officer`, `Administrator`

**Request**

```json
{ "status": "Reviewed" }
```

Accepts `Reviewed` or `Dismissed`. Returns `200` with the updated flag, or `404` if the id is unknown.

---

## 5. `GET /api/analytics/shortages`

Recurring shortage and oversupply patterns. **FR17** · Roles: `Officer`, `Administrator`

**Query parameters:** `cropId`, `regionId`, `type` (`Shortage` | `Oversupply`), `severity` (`Low` | `Medium` | `High`) — all optional.

**200 response**

```json
{
  "items": [
    {
      "id": "7e6f...",
      "cropId": "3f2a...",
      "regionId": "8b1c...",
      "type": "Shortage",
      "severity": "High",
      "detectedAt": "2026-09-15T00:00:00+05:30",
      "notes": "Supply 58% below rolling 8-week baseline for 3 consecutive weeks."
    }
  ]
}
```

---

## 6. `POST /api/reports/export`

Generate a summary report. **FR18** · Roles: `Administrator` only

**Request**

```json
{
  "type": "PriceTrends",
  "dateRangeStart": "2026-07-01",
  "dateRangeEnd": "2026-09-30"
}
```

`type` accepts `Listings`, `Orders`, or `PriceTrends`.

**202 response**

```json
{
  "id": "b2c3...",
  "type": "PriceTrends",
  "generatedAt": "2026-09-20T11:02:44+05:30",
  "fileUrl": "/api/reports/b2c3.../download"
}
```

CSV is the initial format.

---

## 7. `GET /api/reports/{id}`

Retrieve a previously generated report. **FR18** · Roles: `Administrator` only

Returns the same shape as above, or `404` if unknown.

---

## 8. `POST /api/analytics/snapshots/refresh`

Manual rebuild of the materialized trend table. Roles: `Administrator` only

Idempotent — safe to re-run. Upserts against the unique `(CropId, RegionId, Period)` index rather than duplicating rows.

```json
{ "periodsProcessed": 84, "snapshotsUpserted": 84 }
```

---

# Part 2 — What Component D Needs From You

Analytics is read-heavy and depends on every other component. These are the fields required, and what happens until they arrive.

## From Component A — Produce Listings (Student 1)

Needed for price trends **and** anomaly detection — the two highest-priority features.

| Field | Type | Used for |
|---|---|---|
| `Listing.Id` | Guid | Anomaly flag reference |
| `Listing.CropId` | Guid | Trend + shortage grouping |
| `Listing.RegionId` | Guid | Trend + shortage grouping |
| `Listing.Price` | decimal | Trend aggregation, deviation calculation |
| `Listing.AiSuggestedPriceMin` | decimal | Deviation midpoint |
| `Listing.AiSuggestedPriceMax` | decimal | Deviation midpoint |
| `Listing.Status` | enum | Only `Published` listings are aggregated |
| `Listing.CreatedAt` | date-time | Period bucketing |

**Deviation formula:**

```
midpoint  = (AiSuggestedPriceMin + AiSuggestedPriceMax) / 2
deviation = (Price − midpoint) / midpoint × 100
```

If the suggested range is ever null, the listing is skipped, not flagged.

**Also needed:** the shared `Crop` and `Region` reference tables — `Id` plus a display `Name`, since every chart axis needs a label.

> **Until this lands:** Component D runs on its own seeded fixtures.

## From Component B — Orders & Logistics (Student 2)

| Field | Type | Used for |
|---|---|---|
| `Order.ListingId` | Guid | Order context in `/investigate` |
| `Order.Quantity` | decimal | Supply/demand baseline |
| `Order.Status` | enum | Excluding cancelled orders |
| `Order.CreatedAt` | date-time | Period bucketing |

**Also needed — the Logistics Scheduling Agent contract.** Student 4 owns this agent, but it operates on Component B's data and its output feeds `POST /api/orders/{id}/schedule`. Agree this early:

```json
// Agent input
{ "orderId": "...", "centreId": "...", "preferredWindow": { "start": "...", "end": "..." }, "existingBookings": [] }

// Agent output
{ "proposedSlotStart": "...", "proposedSlotEnd": "...", "conflictChecked": true }
```

> **Until this lands:** `BookingCalendarTool` and `CentreCapacityTool` return stubbed responses. The agent is demoable without Component B.

## From Component C — Quality Grading (Student 3)

| Field | Type | Used for |
|---|---|---|
| `Inspection.ListingId` | Guid | Inspection context in `/investigate` |
| `Inspection.ClaimedGrade` | enum | Explaining a price premium |
| `Inspection.InspectedGrade` | enum | Detecting grade mismatch as an anomaly cause |
| `Inspection.InspectedAt` | date-time | Ordering inspection history |

Grade mismatch is a strong candidate explanation for a price anomaly — a listing claiming Grade A but inspected as Grade C explains an inflated price better than market movement does.

> **Until this lands:** `/investigate` returns `inspectionContext.available: false`.

## From Shared Auth

| Claim | Used for |
|---|---|
| `sub` (user id) | `ReportExport.RequestedBy` |
| `role` | `Officer` / `Administrator` route authorization |

> **Until this lands:** development runs against a fake claims principal.

---

## Change Policy

If a shape here has to change, post it in the team chat before merging. Other members may already be coding against it — a silent change costs someone else an afternoon.

**Related:** [`AgriConnect_DFD.md`](AgriConnect_DFD.md) §4.4 · [`AgriConnect_SRS.md`](AgriConnect_SRS.md) FR15–FR18 · [`Member4_ComponentD_DevelopmentMap.md`](Member4_ComponentD_DevelopmentMap.md)
