# SE3110 — Test Case Document: AgriConnect


---

## Part A — How to Use This Document

- **Actual Result** and **Status** are left as `TBD` / `Not Run`. Fill them in after running each test with the tool named for that area. Use **Passed** or **Failed** for executed tests, and **Not Run** or **Blocked** when execution did not occur.
- Endpoint paths, field names, and thresholds below are the intended design. **Confirm them against Swagger and the SRS before running**, and correct the Steps/Expected columns if the real system differs.
- Tests are grouped by the nine agreed testing areas. Components: **M1** Produce Listings & Price Discovery, **M2** Order & Collection-Centre Logistics, **M3** Quality Grading & Inspection, **M4** Market Price Analytics & Reporting.
- Case types: **N** normal, **I** invalid, **B** boundary/edge, **F** failure.
- Link every Failed test to a defect ID in Part D.
- Roles: Farmer, Buyer, Officer (Collection-Centre Officer), Admin.

**Test accounts needed:** one per role; listings in Draft, Active, Sold-out, and Rejected status; one inspection record per grade.

---

## Part B — Test Cases

### Area 1 — Backend Testing (xUnit, Newman)

#### 1A. Unit tests (xUnit) — Owner: [Name 1]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| BE-U-01 | M1 listing creation (N) | Service with mocked repository | Create listing: valid crop, qty 100 kg, price 150 | Listing saved with status Draft/Active per rules; ID returned | TBD | Not Run |
| BE-U-02 | M1 price validation (I) | Same | Create listing with price = -5 | Validation error; nothing saved | TBD | Not Run |
| BE-U-03 | M1 price boundary (B) | Same | Price = 0, then price = 0.01 | 0 rejected; 0.01 accepted (or per SRS minimum) | TBD | Not Run |
| BE-U-04 | M1 quantity boundary (B) | Same | Quantity = 0, then 1 | 0 rejected; 1 accepted | TBD | Not Run |
| BE-U-05 | M2 stock reservation (N) | Listing with stock 50 | Reserve 20 | Stock available becomes 30; reservation created | TBD | Not Run |
| BE-U-06 | M2 over-reservation (I) | Listing with stock 50 | Reserve 51 | Rejected with insufficient-stock error; stock unchanged | TBD | Not Run |
| BE-U-07 | M2 exact reservation (B) | Listing with stock 50 | Reserve exactly 50 | Accepted; available stock = 0 | TBD | Not Run |
| BE-U-08 | M3 grading rule (N) | Inspection service | Submit inspection scores for each grade band | Correct grade assigned for each band | TBD | Not Run |
| BE-U-09 | M3 grade boundary (B) | Same | Score exactly on each band threshold | Threshold value falls in the band defined by SRS | TBD | Not Run |
| BE-U-10 | M4 analytics aggregation (N) | Known price history set | Request average price for a crop and period | Average equals hand-calculated value | TBD | Not Run |
| BE-U-11 | M4 empty data (B) | No price history | Request analytics | Empty/zero result, no exception | TBD | Not Run |
| BE-U-12 | Order state transitions (I) | Order in Completed state | Attempt to cancel | Rejected; invalid transition error | TBD | Not Run |
| BE-U-13 | AI proposal approval rule (F) | AI price proposal in Pending | Attempt to apply without approval | Rejected; price unchanged | TBD | Not Run |
| BE-U-14 | Controller authorization (I) | Controller with Buyer principal | Call Officer-only action | Forbid/403 result | TBD | Not Run |

#### 1B. API integration tests (Newman collection) — Owner: [Name 1]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| BE-A-01 | Login (N) | Seeded Farmer account | `POST /api/auth/login` valid credentials | 200; JWT token returned | TBD | Not Run |
| BE-A-02 | Login (I) | Same | Wrong password | 401; no token | TBD | Not Run |
| BE-A-03 | Registration validation (I) | None | Register with invalid email / missing fields | 400 with field-level errors | TBD | Not Run |
| BE-A-04 | Create listing as Farmer (N) | Farmer token | `POST /api/listings` valid body | 201; body matches input | TBD | Not Run |
| BE-A-05 | Create listing as Buyer (I) | Buyer token | Same request | 403 | TBD | Not Run |
| BE-A-06 | No token (I) | None | `GET` a protected endpoint without header | 401 | TBD | Not Run |
| BE-A-07 | Expired/invalid token (I) | Tampered token | Call protected endpoint | 401 | TBD | Not Run |
| BE-A-08 | IDOR: other farmer's listing (I) | Two Farmers | Farmer B updates Farmer A's listing | 403/404; listing unchanged | TBD | Not Run |
| BE-A-09 | Place order (N) | Buyer token, Active listing stock ≥ qty | `POST /api/orders` | 201; stock reduced by qty | TBD | Not Run |
| BE-A-10 | Order over stock (I) | Same listing | Order qty > stock | 400/409; no order created | TBD | Not Run |
| BE-A-11 | Order against unknown listing (I) | Buyer token | Non-existent listing ID | 404 | TBD | Not Run |
| BE-A-12 | Officer submits inspection (N) | Officer token, pending inspection | `POST` inspection result | 200/201; grade stored | TBD | Not Run |
| BE-A-13 | Admin-only analytics (I) | Farmer token | Request admin report | 403 | TBD | Not Run |
| BE-A-14 | Malformed JSON (I) | Any token | Send invalid JSON body | 400, no stack trace in response | TBD | Not Run |

---

### Area 2 — Database Testing (Testcontainers for .NET + EF Core) — Owner: [Name 1]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| DB-01 | Migrations apply (N) | Empty `postgres:18` container | Run all EF migrations | Completes without error; all tables exist | TBD | Not Run |
| DB-02 | UNIQUE constraint (I) | Migrated DB | Insert two users with the same email | Second insert fails with unique violation | TBD | Not Run |
| DB-03 | CHECK price ≥ 0 (I) | Migrated DB | Insert listing with negative price directly via SQL/EF | DB rejects (check violation) | TBD | Not Run |
| DB-04 | CHECK quantity ≥ 0 (I) | Migrated DB | Insert/update stock to negative | DB rejects | TBD | Not Run |
| DB-05 | NOT NULL (I) | Migrated DB | Insert listing without required column | DB rejects | TBD | Not Run |
| DB-06 | FK enforcement (I) | Migrated DB | Insert order referencing non-existent listing | FK violation | TBD | Not Run |
| DB-07 | Cascade/Restrict (N) | Listing with orders | Delete the listing | Behaviour matches schema rule (restricted, or cascaded as designed) | TBD | Not Run |
| DB-08 | Relationship integrity (N) | Seeded data | Load Listing → Orders → Inspection via EF includes | All related rows returned correctly | TBD | Not Run |
| DB-09 | Concurrent stock reservation (B) | Listing stock = 10 | 20 parallel transactions reserve 1 each | Exactly 10 succeed; stock ends at 0, never negative | TBD | Not Run |
| DB-10 | Transaction rollback (F) | Migrated DB | Order creation fails midway (forced exception) | Entire transaction rolled back; stock and order unchanged | TBD | Not Run |

---

### Area 3 — Web Testing (Vitest, Playwright) — Owner: [Name 2]

#### 3A. Component tests (Vitest) 

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| WEB-C-01 | Login form validation (I) | Component rendered | Submit empty form | Required-field errors shown; no API call | TBD | Not Run |
| WEB-C-02 | Login form email format (I) | Same | Enter `abc` as email | Invalid-email message shown | TBD | Not Run |
| WEB-C-03 | Listing form price (I) | Create-listing form | Enter negative price | Validation message; submit blocked | TBD | Not Run |
| WEB-C-04 | Listing form valid (N) | Same | Enter valid values and submit | Submit handler called with correct payload | TBD | Not Run |
| WEB-C-05 | Protected route (I) | No auth state | Navigate to farmer dashboard | Redirected to login | TBD | Not Run |
| WEB-C-06 | Role-based route (I) | Buyer logged in | Navigate to officer/admin page | Access denied or redirect | TBD | Not Run |
| WEB-C-07 | Listing list loading state (N) | API mocked with delay | Render list | Loading indicator shown, then items | TBD | Not Run |
| WEB-C-08 | Empty state (B) | API mocked to return [] | Render list | Friendly empty-state message | TBD | Not Run |
| WEB-C-09 | API error state (F) | API mocked to return 500 | Render list | Error message and retry option; no crash | TBD | Not Run |
| WEB-C-10 | Order quantity boundary (B) | Order form, stock = 5 | Enter 0, 5, 6 | 0 and 6 rejected; 5 accepted | TBD | Not Run |

#### 3B. Web end-to-end (Playwright, within web app)

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| WEB-E-01 | Farmer creates listing (N) | Running stack, Farmer account | Login → New Listing → submit | Listing appears in "My Listings" | TBD | Not Run |
| WEB-E-02 | Buyer browses and orders (N) | Active listing | Login as Buyer → open listing → place order | Order confirmation shown; appears in Order History | TBD | Not Run |
| WEB-E-03 | Invalid login (I) | None | Enter wrong password | Error message; stays on login page | TBD | Not Run |
| WEB-E-04 | Session expiry (F) | Logged in | Clear token, then act | Redirected to login | TBD | Not Run |

---

### Area 4 — Mobile Testing (flutter_test, integration_test) — Owner: [Name 3]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| MOB-W-01 | Login form validation (I) | Widget pumped | Submit empty fields | Validation errors shown | Shows “Email is required” and “Password is required”; no login/loading state | Passed |
| MOB-W-02 | Listing form price (I) | Create-listing widget | Enter negative/non-numeric price | Error shown; submit disabled/blocked | Both invalid values show “Enter a valid positive price”; createListing is never called | Passed |
| MOB-W-03 | Listing card rendering (N) | Mock listing model | Pump card widget | Crop name, price, grade, quantity displayed | Mock card displays Carrot, LKR 240 / kg, Grade A, and 12 kg | Passed |
| MOB-W-04 | Navigation (N) | App widget tree | Tap listing → detail | Detail screen shown with correct listing | Tapping the mocked Carrot card opens its detail page with crop, category, region and Published status | Passed |
| MOB-W-05 | Loading/empty/error (F) | Mock repository (mocktail) | Return delayed, empty, error | Spinner, empty message, error with retry | Spinner and “No listings found” shown; API failure shows error snackbar and refresh; retry loads listing | Passed |
| MOB-U-01 | API client parsing (N) | Mock HTTP client | Valid JSON response | Model parsed with correct fields | Mock HTTP listing JSON parsed with id, crop, region, quantity, availability, grade, status, price, photo and pagination | Passed |
| MOB-U-02 | API client malformed data (F) | Mock HTTP client | Missing fields in JSON | Handled gracefully; no crash | Missing optional listing fields use actual Listing.fromJson defaults; parser completes without crash | Passed |
| MOB-E-01 | Farmer login and create listing (N) | Physical Android phone; .NET backend reachable via adb reverse | Login → create listing | Listing visible in list | Failed on the physical phone: opening the create-listing form triggers Flutter rendering assertion “BoxConstraints forces an infinite width” at `mobile/lib/screens/create_listing_screen.dart:737` (Add URL button); listing submission is not reached. Log: `mobile/evidence/integration_test_farmer_log.txt`. | Failed |
| MOB-E-02 | Buyer places order (N) | Physical Android phone; published listing and backend running | Login as Buyer → order listing | Confirmation and order in history | Failed on the physical phone: the expected “Order placed!” confirmation was absent. The test stopped at this assertion, so the order-history check and persistence were not verified. Log: `mobile/evidence/integration_test_buyer_log.txt`; `mobile/evidence/MOB-E-02_failure.png` was captured after the test and shows the launcher, not the in-app failure state. | Failed |
| MOB-E-03 | Offline / server down (F) | Physical Android phone; API_BASE_URL points to unreachable localhost port 5999 | Open app and refresh list | User-friendly error; app remains usable | Passed using an unreachable API base URL (backend was not stopped): marketplace remained open, showed “Error loading listings” and Refresh listings; no uncaught test exception. Log: `mobile/evidence/integration_test_log.txt`. | Passed |

---

### Area 5 — Cross-Platform Integration Testing (Playwright, Appium) — Owners: [Name 2] (web, with Playwright) / [Name 3] (mobile, Appium)

> At least one of these must run on the real stack (UI → API → DB) and be demonstrated live in the viva.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| XP-W-01 | Full workflow, web (N) | Real API + DB; Farmer, Buyer, Officer accounts | Farmer lists produce → Officer inspects/grades → Buyer orders → Officer schedules pickup | Each step succeeds; order shows final status; DB rows (listing, inspection, order, reservation) are consistent | TBD | Not Run |
| XP-W-02 | Stock consistency across UI and DB (N) | Listing stock = 10 | Buyer orders 4 on web | UI shows 6 remaining; DB stock = 6 | TBD | Not Run |
| XP-W-03 | Cross-component: M1 listing orderable via M2 (N) | New listing created via M1 | Order it via M2 | Order succeeds with correct price and grade | TBD | Not Run |
| XP-W-04 | Cross-component: M3 grade visible in M4 analytics (N) | Inspected listings exist | Open analytics report | Grade-based figures reflect inspections | TBD | Not Run |
| XP-W-05 | Role isolation (I) | Buyer logged in | Try to open Officer page by URL | Blocked; no data leaked | TBD | Not Run |
| XP-M-01 | Full workflow, mobile (N) | Physical phone, real API + DB; listing requires Officer inspection/publish before Buyer can order | Farmer lists → Officer publishes after inspection → Buyer orders on mobile | Rows persisted; status correct on both clients | Not Run: Flutter Integration Driver package is present locally and the Appium APK was built, but Appium CLI registration fails on Windows (see `mobile/evidence/appium_driver_install.log`); WDIO tests were not run. Device integration failures are recorded under MOB-E-01/02. New farmer listings require Officer inspection/publish before a Buyer can order. | Not Run |
| XP-M-02 | Cross-platform consistency (N) | Listing created on web | Open same listing on mobile via Appium | Same price, quantity, and grade displayed | Not Run: Appium CLI registration fails on Windows, so WDIO tests were not run; see `mobile/evidence/appium_driver_install.log`. | Not Run |
| XP-M-03 | AI proposal needs human approval (N) | AI price proposal pending (mock mode) | Open proposal → approve in UI | Price changes only after approval; audit entry created | Blocked: current mobile app can display/refresh AI price suggestions but has no proposal approve UI; backend decision endpoints are Officer/Admin-only. | Blocked |
| XP-M-04 | AI proposal rejected (N) | Same | Reject proposal | Price unchanged; rejection recorded | Blocked: current mobile app has no proposal rejection UI; do not invent app behavior. | Blocked |

---

### Area 6 — Performance / Load / Stress Testing (k6) — Owner: [Name 4]

Targets below are proposed; adjust to SRS §8 values if specified. Record machine specs for the runner and the system under test.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| PERF-01 | Baseline browse listings (N) | Seeded DB (≥ 1,000 listings) | 1 VU, 1 min, `GET /api/listings` | p95 < 500 ms; 0% errors | TBD | Not Run |
| PERF-02 | Load: normal (N) | Same | 50 VUs, 5 min mixed browse/login | p95 < 800 ms; error rate < 1% | TBD | Not Run |
| PERF-03 | Stress: ramp (B) | Same | Ramp 50 → 300 VUs | Record breaking point; errors are controlled (4xx/5xx, no crash) | TBD | Not Run |
| PERF-04 | Concurrent stock reservation (B) | Listing stock = 100 | 200 VUs each reserve 1 simultaneously | Exactly 100 succeed; stock = 0; never negative; no duplicates | TBD | Not Run |
| PERF-05 | Recovery (F) | After stress test | Drop to 10 VUs for 2 min | Response times return to baseline; API healthy | TBD | Not Run |
| PERF-06 | Spike (B) | Seeded DB | Jump to 150 VUs for 30 s | System recovers; failed request rate recorded | TBD | Not Run |

---

### Area 7 — Accessibility (Lighthouse) — Owner: [Name 2]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| A11Y-01 | Login page (N) | Web app running | Run Lighthouse accessibility audit | Score ≥ 90; list any failing audits | TBD | Not Run |
| A11Y-02 | Listings page (N) | Logged in | Same | Score ≥ 90; images have alt text; form labels present | TBD | Not Run |
| A11Y-03 | Order form (N) | Logged in as Buyer | Same | Score ≥ 90; sufficient colour contrast; focusable controls | TBD | Not Run |
| A11Y-04 | Keyboard navigation (N) | Same | Tab through login and order flow | All controls reachable and operable by keyboard | TBD | Not Run |

---

### Area 8 — Compatibility (Playwright) — Owner: [Name 2]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| COMP-01 | Login + browse on Chromium (N) | Playwright projects configured | Run suite on Chromium | All pass | TBD | Not Run |
| COMP-02 | Login + browse on Firefox (N) | Same | Run suite on Firefox | All pass | TBD | Not Run |
| COMP-03 | Login + browse on WebKit (N) | Same | Run suite on WebKit | All pass | TBD | Not Run |
| COMP-04 | Login + browse on Edge channel (N) | Edge installed | Run suite with `channel: 'msedge'` | All pass | TBD | Not Run |
| COMP-05 | Order flow across all browsers (N) | Same | Run WEB-E-02 on all projects | Same result in all; record pass/fail matrix | TBD | Not Run |

---

### Area 9 — Security (OWASP ZAP) — Owner: [Name 4]

Scan only a local/test deployment. Authorization checks BE-A-05, BE-A-06, BE-A-07, BE-A-08, BE-A-13, and XP-W-05 complement these scans.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|---|---|
| SEC-01 | Baseline passive scan (N) | API running on test host; OpenAPI spec | ZAP baseline scan | Report generated; 0 High-risk alerts | TBD | Not Run |
| SEC-02 | Active scan with auth (N) | Valid JWT configured in ZAP | ZAP full/active scan of API | 0 High; Medium alerts documented | TBD | Not Run |
| SEC-03 | SQL injection (I) | Search/filter endpoints | Inject payloads in query parameters | No SQL errors or data leakage; 400/empty result | TBD | Not Run |
| SEC-04 | XSS in listing text (I) | Farmer account | Create listing with `<script>` in description; view in web app | Script not executed; output encoded | TBD | Not Run |
| SEC-05 | Security headers / CORS (N) | API running | Inspect scan results | Sensible CORS policy; no sensitive info in error responses | TBD | Not Run |
| SEC-06 | Brute-force login (I) | API running | Repeated failed logins (script or ZAP fuzzer) | Account/IP throttled or locked, or finding recorded as defect | TBD | Not Run |
| SEC-07 | Price tampering (I) | Buyer token | Submit order with client-supplied lower price | Server ignores client price; order uses server-side price | TBD | Not Run |

---

## Part C — Traceability and Coverage Summary

Fill the FR column using SRS §7 (FR1–FR22) once confirmed.

| Area | Test IDs | Components covered | FR refs |
|---|---|---|---|
| Backend | BE-U-01..14, BE-A-01..14 | M1–M4, Auth | TBD |
| Database | DB-01..10 | M1, M2, M3 | TBD |
| Web | WEB-C-01..10, WEB-E-01..04 | M1, M2 | TBD |
| Mobile | MOB-W/U/E-01.. | M1, M2 | TBD |
| Cross-Platform | XP-W-01..05, XP-M-01..04 | M1–M4, AI approval flow | TBD |
| Performance | PERF-01..06 | M1, M2 | TBD |
| Accessibility | A11Y-01..04 | Web | TBD |
| Compatibility | COMP-01..05 | Web | TBD |
| Security | SEC-01..07 | API | TBD |

---

## Part D — Defect / Bug Report

| Defect ID | Linked test | Component / severity / priority | Description and reproduction | Expected vs actual | Evidence | Status / retest |
|---|---|---|---|---|---|---|
| DEF-001 | MOB-E-01 | Mobile create-listing form / High / P1 | On the physical Android phone, sign in as Farmer → Listings → New listing. The create-listing form throws a Flutter layout assertion at the Add URL button before the form can be submitted. | Expected: form renders and accepts a listing. Actual: `BoxConstraints forces an infinite width`; listing creation cannot proceed. | `mobile/evidence/integration_test_farmer_log.txt` | Open; not fixed or retested. |
| DEF-002 | MOB-E-02 | Mobile order flow / High / P1 | On the physical Android phone, sign in as Buyer, open a published listing, enter a valid quantity, and submit the order. | Expected: “Order placed!” confirmation followed by the order in history. Actual: confirmation is absent and the test stops before the history check; backend persistence is unverified. Numeric enum parsing in the API response is a possible contract discrepancy, not a confirmed root cause. | `mobile/evidence/integration_test_buyer_log.txt`; `mobile/evidence/MOB-E-02_failure.png` is post-test launcher evidence only. | Open; not fixed or retested. |

Defect log summary:

| Defect ID | Test ID | Severity | Status | Retest result |
|---|---|---|---|---|
| DEF-001 | MOB-E-01 | High | Open | Not retested |
| DEF-002 | MOB-E-02 | High | Open | Not retested |

---

## Part E — Test Execution Summary

| Area | Planned | Executed | Passed | Failed | Not Run | Defects found | Defects fixed |
|---|---|---|---|---|---|---|---|
| Backend | 28 | | | | | | |
| Database | 10 | | | | | | |
| Web | 14 | | | | | | |
| Mobile | 10 | 10 | 8 | 2 | 0 | 2 | 0 |
| Cross-Platform | 9 | 0 | 0 | 0 | 9 | 0 | 0 |
| Performance | 6 | | | | | | |
| Accessibility | 4 | | | | | | |
| Compatibility | 5 | | | | | | |
| Security | 7 | | | | | | |
| **Total** | **93** | | | | | | |

**Conclusion:** For IT24103113's mobile cases, 8 of 10 executed cases passed (MOB-U-01/02, MOB-W-01..05, and MOB-E-03); MOB-E-01/02 failed and are tracked as DEF-001/002. XP-M-01/02 were not run because Appium driver installation failed, and XP-M-03/04 remain unsupported by the current mobile approval/rejection UI. No production defects were changed or retested. Other test areas in this document remain unexecuted; this is not an overall system release assessment.

---

## Part F — Evidence Checklist

| Evidence | Owner | Saved path | Done |
|---|---|---|---|
| xUnit results + coverage report | [Name 1] | | ☐ |
| Newman run report (HTML/JSON) | [Name 1] | | ☐ |
| Testcontainers DB test output | [Name 1] | | ☐ |
| Vitest results + coverage | [Name 2] | | ☐ |
| Playwright HTML report (all browsers) | [Name 2] | | ☐ |
| Lighthouse reports | [Name 2] | | ☐ |
| flutter_test / integration_test logs | [Name 3] | | ☐ |
| Appium logs + screenshots | [Name 3] | | ☐ |
| k6 summary output | [Name 4] | | ☐ |
| OWASP ZAP report | [Name 4] | | ☐ |
| Git commit history per member | All | | ☐ |
