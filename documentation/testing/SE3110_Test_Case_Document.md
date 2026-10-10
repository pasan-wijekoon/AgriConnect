# SE3110 — Test Case Document: AgriConnect


---

## Part A: How to Read This Document

- Every row below was executed or consciously left **Not Run** on **2026-10-07**. **Actual Result** and **Status** are filled from real tool output; the last column names the automated test or evidence file that proves it. Status is **Passed**, **Failed** or **Not Run** (with the reason in the Actual Result).
- Where the first run **Failed**, the Actual Result says so, links the defect in Part D, and the Status shows the result of the **retest** after the fix.
- Test IDs, steps and expected results are the plan's. Where the real system or the available tooling differs from the plan (for example xUnit with a real PostgreSQL database instead of Testcontainers, xUnit `WebApplicationFactory` instead of Newman, reduced k6 scale), the Actual Result states the difference. Nothing was marked Passed without a test that ran.
- A complete list of every automated test that ran (xUnit 378, pytest 89, Vitest 24, Playwright 12, flutter_test 20) is in [SE3110_Executed_Test_Results.md](SE3110_Executed_Test_Results.md), generated from the raw tool output.
- Components: **M1** Produce Listings & Price Discovery, **M2** Order & Collection-Centre Logistics, **M3** Quality Grading & Inspection, **M4** Market Price Analytics & Reporting.
- Case types: **N** normal, **I** invalid, **B** boundary/edge, **F** failure. Roles: Farmer, Buyer, Officer, Admin.
- Evidence paths are relative to the repository root; `run-2026-10-07/` means `testing-evidence/run-2026-10-07/`.

**Test environment:** Windows 11, .NET 10 Release build of the API on `localhost:5000`, PostgreSQL (local, dedicated database `agriconnect_perf`, migrated and seeded), React dev server on `:3000`, agentic-ai (mock LLM) on `:8000`, Chromium 1228 for browser tests. Performance and security tests were run only against this local database.

---

## Part B — Test Cases

### Area 1 — Backend Testing (xUnit, Newman)

#### 1A. Unit tests (xUnit) — Owner: [Name 1]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| BE-U-01 | M1 listing creation (N) | Service with mocked repository | Create listing: valid crop, qty 100 kg, price 150 | Listing saved with status Draft/Active per rules; ID returned | 201; listing saved as PendingApproval (it must pass the FR5 quality gate before it is published) | Passed | xUnit ListingsApiTests.Create_WithFutureWindow_ReturnsPendingApproval |
| BE-U-02 | M1 price validation (I) | Same | Create listing with price = -5 | Validation error; nothing saved | Not run at API/service level (negative price is rejected by the form: see WEB-C-03) | Not Run | No backend test; UI level WEB-C-03 |
| BE-U-03 | M1 price boundary (B) | Same | Price = 0, then price = 0.01 | 0 rejected; 0.01 accepted (or per SRS minimum) | Not run at API/service level (UI boundary 0 / 1 passed: WEB-C-03b) | Not Run | No backend test; UI level WEB-C-03b |
| BE-U-04 | M1 quantity boundary (B) | Same | Quantity = 0, then 1 | 0 rejected; 1 accepted | Order quantity <= 0 rejected with invalid-request; listing-quantity boundary only checked in UI (WEB-C-03c) | Passed | xUnit OrderServiceTests.PlaceOrderAsync_WithNonPositiveQuantity_ReturnsInvalidRequest, OrdersApiTests.Create_WithInvalidQuantity_Returns400 |
| BE-U-05 | M2 stock reservation (N) | Listing with stock 50 | Reserve 20 | Stock available becomes 30; reservation created | Reservation created, available stock reduced by the ordered quantity and restored on cancel | Passed | xUnit OrderServiceTests.PlaceOrderAsync_WithValidRequest_ReturnsPendingOrder, ListingAvailabilityApiTests.AvailableQuantity_DropsWhenAnOrderIsPlaced_AndIsRestoredOnCancel |
| BE-U-06 | M2 over-reservation (I) | Listing with stock 50 | Reserve 51 | Rejected with insufficient-stock error; stock unchanged | Over-reservation rejected with a conflict; stock unchanged | Passed | xUnit StockReservationServiceConcurrencyTests.TwoConcurrentReservations_WhereSumExceedsAvailable_ExactlyOneSucceeds, OrderServiceTests.PlaceOrderAsync_WhenReservationFails_ReturnsConflict |
| BE-U-07 | M2 exact reservation (B) | Listing with stock 50 | Reserve exactly 50 | Accepted; available stock = 0 | Exact-stock reservation accepted; the next one is refused | Passed | xUnit StockReservationServiceConcurrencyTests.TwoConcurrentReservations_WhereSumEqualsAvailable_BothSucceed_ThirdFails; k6 order race: stock 370 -> exactly 37 x 10 accepted |
| BE-U-08 | M3 grading rule (N) | Inspection service | Submit inspection scores for each grade band | Correct grade assigned for each band | Claimed vs inspected grade compared correctly; matching grade has no discrepancy, mismatch is flagged, invalid grade rejected | Passed | xUnit InspectionServiceTests (RecordInspectionAsync_*) |
| BE-U-09 | M3 grade boundary (B) | Same | Score exactly on each band threshold | Threshold value falls in the band defined by SRS | Not applicable as written: grades are officer-assigned (A/B/C), not numeric score bands; discrepancy rule covered by BE-U-08 | Not Run | n/a |
| BE-U-10 | M4 analytics aggregation (N) | Known price history set | Request average price for a crop and period | Average equals hand-calculated value | Aggregation of price history matches the expected values | Passed | xUnit AnalyticsServicesTests (AggregateAsync_*, BuildPoints_*) |
| BE-U-11 | M4 empty data (B) | No price history | Request analytics | Empty/zero result, no exception | No data: nothing written, no exception | Passed | xUnit AnalyticsServicesTests.AggregateAsync_WithNoData_WritesNothing |
| BE-U-12 | Order state transitions (I) | Order in Completed state | Attempt to cancel | Rejected; invalid transition error | Cancel of a Completed order refused (409); illegal status transition refused (400) | Passed | xUnit OrderServiceTests.CancelAsync_OfficerCancelsCompletedOrder_ReturnsConflict, OrdersApiTests.UpdateStatus_IllegalTransition_Returns400 |
| BE-U-13 | AI proposal approval rule (F) | AI price proposal in Pending | Attempt to apply without approval | Rejected; price unchanged | The AI schedule proposal is only Proposed; approving the order never confirms it, and the agent contracts have no commit field | Passed | xUnit OrderWorkflowApiTests.Approve_AutoProposesAScheduleAtTheRegionCentre_AndNeverConfirmsIt; pytest test_ai_safe_03, test_ai_safe_04 (schedule/match proposal variant; the price-proposal variant was not tested) |
| BE-U-14 | Controller authorization (I) | Controller with Buyer principal | Call Officer-only action | Forbid/403 result | Buyer calling Officer/Farmer-only actions gets 403 | Passed | xUnit OrdersApiTests.Create_AsFarmer_Returns403, UpdateStatus_AsBuyer_Returns403; AdminUsersApiTests |

#### 1B. API integration tests (Newman collection) — Owner: [Name 1]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| BE-A-01 | Login (N) | Seeded Farmer account | `POST /api/auth/login` valid credentials | 200; JWT token returned | 200 with a JWT; 130+ valid logins in the k6 login scenario all returned a token | Passed | k6 login_load check 'login 200' 100%; xUnit AdminUsersApiTests login |
| BE-A-02 | Login (I) | Same | Wrong password | 401; no token | 401 for a wrong password; no token | Passed | scripted SEC-06 / AuthThrottleApiTests (401 before throttle) |
| BE-A-03 | Registration validation (I) | None | Register with invalid email / missing fields | 400 with field-level errors | Not run | Not Run | No registration-validation test |
| BE-A-04 | Create listing as Farmer (N) | Farmer token | `POST /api/listings` valid body | 201; body matches input | 201 PendingApproval for a Farmer | Passed | xUnit ListingsApiTests.Create_WithFutureWindow_ReturnsPendingApproval |
| BE-A-05 | Create listing as Buyer (I) | Buyer token | Same request | 403 | 403 | Passed | xUnit ListingsApiTests.Create_AsBuyer_Returns403 |
| BE-A-06 | No token (I) | None | `GET` a protected endpoint without header | 401 | 401 | Passed | xUnit OrdersApiTests.Create_Unauthenticated_Returns401; security_checks SEC-A1 |
| BE-A-07 | Expired/invalid token (I) | Tampered token | Call protected endpoint | 401 | 401 for a JWT with a tampered signature | Passed | security_checks SEC-A2 |
| BE-A-08 | IDOR: other farmer's listing (I) | Two Farmers | Farmer B updates Farmer A's listing | 403/404; listing unchanged | Orders: another farmer/buyer gets 404/403 and no data (listing-edit IDOR itself not tested) | Passed | xUnit OrdersApiTests.GetById_DifferentBuyer_Returns404NotForbidden, Cancel_DifferentBuyer_Returns404; security_checks SEC-A5, SEC-A6 |
| BE-A-09 | Place order (N) | Buyer token, Active listing stock ≥ qty | `POST /api/orders` | 201; stock reduced by qty | 201 Pending with reservation; stock reduced | Passed | xUnit OrdersApiTests.Create_ValidRequest_Returns201WithReservation; Playwright XP-W-01/02 |
| BE-A-10 | Order over stock (I) | Same listing | Order qty > stock | 400/409; no order created | Refused (409) when stock is insufficient; no order created | Passed | k6 order race: 23 of 60 orders refused once stock ran out; OrderServiceTests |
| BE-A-11 | Order against unknown listing (I) | Buyer token | Non-existent listing ID | 404 | 404 | Passed | xUnit OrdersApiTests.Create_WithUnknownListing_Returns404 |
| BE-A-12 | Officer submits inspection (N) | Officer token, pending inspection | `POST` inspection result | 200/201; grade stored | Inspection recorded and grade stored (service level; the HTTP endpoint itself was not exercised) | Passed | xUnit InspectionServiceTests.RecordInspectionAsync_* |
| BE-A-13 | Admin-only analytics (I) | Farmer token | Request admin report | 403 | 403 for non-admin roles on admin/officer-only endpoints | Passed | security_checks SEC-A4; xUnit TodayPricesApiTests.Catalog_ManagementIsAdministratorOnly |
| BE-A-14 | Malformed JSON (I) | Any token | Send invalid JSON body | 400, no stack trace in response | 400 and no stack trace | Passed | security_checks SEC-05a |

---

### Area 2 — Database Testing (Testcontainers for .NET + EF Core) — Owner: [Name 1]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| DB-01 | Migrations apply (N) | Empty `postgres:18` container | Run all EF migrations | Completes without error; all tables exist | All EF migrations applied to a brand-new PostgreSQL database; app started and seeded | Passed | logs/backend-api-local.log ('migrations applied', seeded 15 centres, 96 snapshots) |
| DB-02 | UNIQUE constraint (I) | Migrated DB | Insert two users with the same email | Second insert fails with unique violation | Duplicate email refused (409) through the unique index | Passed | xUnit AdminUsersApiTests.CreateOfficer_WithAnEmailAlreadyInUse_Returns409_EvenInADifferentCase |
| DB-03 | CHECK price ≥ 0 (I) | Migrated DB | Insert listing with negative price directly via SQL/EF | DB rejects (check violation) | Not run (no direct-SQL constraint test) | Not Run | - |
| DB-04 | CHECK quantity ≥ 0 (I) | Migrated DB | Insert/update stock to negative | DB rejects | Not run directly; stock never went negative under load (see DB-09) | Not Run | - |
| DB-05 | NOT NULL (I) | Migrated DB | Insert listing without required column | DB rejects | Not run | Not Run | - |
| DB-06 | FK enforcement (I) | Migrated DB | Insert order referencing non-existent listing | FK violation | Not run directly (service returns 404 for an unknown listing: BE-A-11) | Not Run | - |
| DB-07 | Cascade/Restrict (N) | Listing with orders | Delete the listing | Behaviour matches schema rule (restricted, or cascaded as designed) | Not run | Not Run | - |
| DB-08 | Relationship integrity (N) | Seeded data | Load Listing → Orders → Inspection via EF includes | All related rows returned correctly | Order responses join listing, crop, region, buyer, farmer and centre correctly | Passed | xUnit OrdersApiTests.GetById_*; Playwright XP-W-01 |
| DB-09 | Concurrent stock reservation (B) | Listing stock = 10 | 20 parallel transactions reserve 1 each | Exactly 10 succeed; stock ends at 0, never negative | 20 parallel reservations for capacity 1: exactly 1 succeeded; over HTTP 60 orders vs stock 370: exactly 37 accepted, stock ended at 0, never negative | Passed | xUnit StockReservationServiceConcurrencyTests.TwentyConcurrentReservations_ForCapacityOfOne_ExactlyOneSucceeds; testing-evidence/run-2026-10-07/k6-run-C-after-fixes-output.txt (FR9 check) |
| DB-10 | Transaction rollback (F) | Migrated DB | Order creation fails midway (forced exception) | Entire transaction rolled back; stock and order unchanged | Not run (no forced-failure rollback test) | Not Run | - |

---

### Area 3 — Web Testing (Vitest, Playwright) — Owner: [Name 2]

#### 3A. Component tests (Vitest) 

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| WEB-C-01 | Login form validation (I) | Component rendered | Submit empty form | Required-field errors shown; no API call | Empty submit blocked, both fields marked missing, login() not called | Passed | Vitest LoginPage.test.tsx WEB-C-01 |
| WEB-C-02 | Login form email format (I) | Same | Enter `abc` as email | Invalid-email message shown | 'abc' rejected as an invalid email; API not called | Passed | Vitest LoginPage.test.tsx WEB-C-02 |
| WEB-C-03 | Listing form price (I) | Create-listing form | Enter negative price | Validation message; submit blocked | Negative floor price rejected, submit blocked | Passed | Vitest AddEditListingModal.test.tsx WEB-C-03, 03b, 03c |
| WEB-C-04 | Listing form valid (N) | Same | Enter valid values and submit | Submit handler called with correct payload | Valid values submitted with the correct payload | Passed | Vitest AddEditListingModal.test.tsx WEB-C-04..04d |
| WEB-C-05 | Protected route (I) | No auth state | Navigate to farmer dashboard | Redirected to login | Anonymous visitor sees the login form, no dashboard | Passed | Vitest Marketplace.test.tsx WEB-C-05 |
| WEB-C-06 | Role-based route (I) | Buyer logged in | Navigate to officer/admin page | Access denied or redirect | Buyer gets the buyer dashboard only; officer gets the back-office navigation; Administrator role mapped | Passed | Vitest Marketplace.test.tsx WEB-C-06, 06b, 06c |
| WEB-C-07 | Listing list loading state (N) | API mocked with delay | Render list | Loading indicator shown, then items | Loading message, then the items | Passed | Vitest Marketplace.test.tsx WEB-C-07 |
| WEB-C-08 | Empty state (B) | API mocked to return [] | Render list | Friendly empty-state message | Friendly empty state with a reset button | Passed | Vitest Marketplace.test.tsx WEB-C-08 |
| WEB-C-09 | API error state (F) | API mocked to return 500 | Render list | Error message and retry option; no crash | Error panel "Couldn't load produce" with Retry; no crash | Passed | Vitest Marketplace.test.tsx WEB-C-09 |
| WEB-C-10 | Order quantity boundary (B) | Order form, stock = 5 | Enter 0, 5, 6 | 0 and 6 rejected; 5 accepted | Stock 5: 0 and 6 rejected, 1 and 5 accepted | Passed | Vitest Marketplace.test.tsx WEB-C-10, WEB-C-11 |

#### 3B. Web end-to-end (Playwright, within web app)

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| WEB-E-01 | Farmer creates listing (N) | Running stack, Farmer account | Login → New Listing → submit | Listing appears in "My Listings" | Not run (no Playwright spec for listing creation) | Not Run | - |
| WEB-E-02 | Buyer browses and orders (N) | Active listing | Login as Buyer → open listing → place order | Order confirmation shown; appears in Order History | Buyer opened a listing in the browser and placed an order; confirmation shown and order present in the API | Passed | Playwright workflow.spec.ts XP-W-01 (buyer leg) |
| WEB-E-03 | Invalid login (I) | None | Enter wrong password | Error message; stays on login page | Error shown, stays on the login page, no session stored | Passed | Playwright auth.spec.ts WEB-E-03 |
| WEB-E-04 | Session expiry (F) | Logged in | Clear token, then act | Redirected to login | Removing the session sends the user back to the login page; session survives a reload while signed in | Passed | Playwright auth.spec.ts WEB-E-04, WEB-E-04b |

---

### Area 4 — Mobile Testing (flutter_test, integration_test) — Owner: [Name 3]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| MOB-W-01 | Login form validation (I) | Widget pumped | Submit empty fields | Validation errors shown | Validation errors shown for empty fields | Passed | flutter_test MOB-W-01 |
| MOB-W-02 | Listing form price (I) | Create-listing widget | Enter negative/non-numeric price | Error shown; submit disabled/blocked | -5 and 'abc' prices block submission | Passed | flutter_test MOB-W-02 (x2) |
| MOB-W-03 | Listing card rendering (N) | Mock listing model | Pump card widget | Crop name, price, grade, quantity displayed | Crop, price, grade and quantity displayed | Passed | flutter_test MOB-W-03 |
| MOB-W-04 | Navigation (N) | App widget tree | Tap listing → detail | Detail screen shown with correct listing | Tapping a listing opens its detail | Passed | flutter_test MOB-W-04 |
| MOB-W-05 | Loading/empty/error (F) | Mock repository (mocktail) | Return delayed, empty, error | Spinner, empty message, error with retry | Loading, empty and error-with-retry states all render | Passed | flutter_test MOB-W-05 (x3) |
| MOB-U-01 | API client parsing (N) | Mock HTTP client | Valid JSON response | Model parsed with correct fields | Listing fields parsed from valid JSON | Passed | flutter_test MOB-U-01 |
| MOB-U-02 | API client malformed data (F) | Mock HTTP client | Missing fields in JSON | Handled gracefully; no crash | Missing fields fall back to safe defaults, no crash | Passed | flutter_test MOB-U-02 |
| MOB-E-01 | Farmer login and create listing (N) | Emulator API 34, backend running | Login → create listing | Listing visible in list | Not run: no emulator available, and no integration_test suite exists in mobile/ | Not Run | - |
| MOB-E-02 | Buyer places order (N) | Same | Login as Buyer → order listing | Confirmation and order in history | Not run (see MOB-E-01) | Not Run | - |
| MOB-E-03 | Offline / server down (F) | Backend stopped | Open app and refresh list | User-friendly error; app remains usable | Not run (see MOB-E-01); the error-state widget is covered by MOB-W-05 | Not Run | - |

---

### Area 5 — Cross-Platform Integration Testing (Playwright, Appium) — Owners: [Name 2] (web, with Playwright) / [Name 3] (mobile, Appium)

> At least one of these must run on the real stack (UI → API → DB) and be demonstrated live in the viva.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| XP-W-01 | Full workflow, web (N) | Real API + DB; Farmer, Buyer, Officer accounts | Farmer lists produce → Officer inspects/grades → Buyer orders → Officer schedules pickup | Each step succeeds; order shows final status; DB rows (listing, inspection, order, reservation) are consistent | Buyer ordered in the UI -> officer approved -> logistics agent proposed a slot (POST /agents/logistics/schedule 200) -> officer confirmed -> Scheduled -> Completed; audit trail has all 5 events. Scope differs from the plan: the farmer-listing and inspection legs are covered by API/service tests, not driven through the UI | Passed | Playwright workflow.spec.ts; testing-evidence/run-2026-10-07/xp-w-01-evidence.txt |
| XP-W-02 | Stock consistency across UI and DB (N) | Listing stock = 10 | Buyer orders 4 on web | UI shows 6 remaining; DB stock = 6 | UI said '5 reserved'; API order Pending and listing availableQuantity dropped by exactly 5 | Passed | Playwright workflow.spec.ts (stock assertion) |
| XP-W-03 | Cross-component: M1 listing orderable via M2 (N) | New listing created via M1 | Order it via M2 | Order succeeds with correct price and grade | The order was placed against a seeded published listing created by the listings module and succeeded | Passed | Playwright workflow.spec.ts |
| XP-W-04 | Cross-component: M3 grade visible in M4 analytics (N) | Inspected listings exist | Open analytics report | Grade-based figures reflect inspections | Not run | Not Run | - |
| XP-W-05 | Role isolation (I) | Buyer logged in | Try to open Officer page by URL | Blocked; no data leaked | Buyer never sees officer navigation; opening /orders/{id} by URL shows 'Access denied' and no actions | Passed | Playwright auth.spec.ts XP-W-05; workflow.spec.ts XP-W-05b |
| XP-M-01 | Full workflow, mobile (N) | Emulator, real API + DB | Farmer lists → Buyer orders on mobile | Rows persisted; status correct on both clients | Not run: Appium/emulator not set up | Not Run | - |
| XP-M-02 | Cross-platform consistency (N) | Listing created on web | Open same listing on mobile via Appium | Same price, quantity, and grade displayed | Not run: Appium/emulator not set up | Not Run | - |
| XP-M-03 | AI proposal needs human approval (N) | AI price proposal pending (mock mode) | Open proposal → approve in UI | Price changes only after approval; audit entry created | Not run in the UI; backend rule covered by BE-U-13 | Not Run | - |
| XP-M-04 | AI proposal rejected (N) | Same | Reject proposal | Price unchanged; rejection recorded | Not run | Not Run | - |

---

### Area 6 — Performance / Load / Stress Testing (k6) — Owner: [Name 4]

Targets below are proposed; adjust to SRS §8 values if specified. Record machine specs for the runner and the system under test.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| PERF-01 | Baseline browse listings (N) | Seeded DB (≥ 1,000 listings) | 1 VU, 1 min, `GET /api/listings` | p95 < 500 ms; 0% errors | Not run as specified (1 VU, 1 min). Earlier single-endpoint smoke run (1 VU, 10 s) exists from 3 Oct | Not Run | testing-evidence/performance/k6-output.txt (older run) |
| PERF-02 | Load: normal (N) | Same | 50 VUs, 5 min mixed browse/login | p95 < 800 ms; error rate < 1% | Adapted: 20 VUs x 30 s mixed browse (listings, price trends, centres). p95 36 ms, 0% errors, 100% checks | Passed | testing-evidence/run-2026-10-07/k6-run-C-after-fixes-output.txt; threshold p(95)<500 ms met |
| PERF-03 | Stress: ramp (B) | Same | Ramp 50 → 300 VUs | Record breaking point; errors are controlled (4xx/5xx, no crash) | Adapted: ramp 0 -> 200 VUs in 30 s. No server errors; no breaking point reached on the local Release build | Passed | testing-evidence/run-2026-10-07/k6-run-C-after-fixes-output.txt (browse_stress) |
| PERF-04 | Concurrent stock reservation (B) | Listing stock = 100 | 200 VUs each reserve 1 simultaneously | Exactly 100 succeed; stock = 0; never negative; no duplicates | Adapted: 60 orders from 30 VUs against stock 370 -> exactly 37 accepted, 23 refused, stock 0, never negative, no 5xx | Passed | testing-evidence/run-2026-10-07/k6-run-C-after-fixes-output.txt (FR9 checks 4/4) |
| PERF-05 | Recovery (F) | After stress test | Drop to 10 VUs for 2 min | Response times return to baseline; API healthy | Not run as a separate recovery phase | Not Run | - |
| PERF-06 | Spike (B) | Seeded DB | Jump to 150 VUs for 30 s | System recovers; failed request rate recorded | Not run (the ramp is not a spike) | Not Run | - |

---

### Area 7 — Accessibility (Lighthouse) — Owner: [Name 2]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| A11Y-01 | Login page (N) | Web app running | Run Lighthouse accessibility audit | Score ≥ 90; list any failing audits | Lighthouse accessibility 100 | Passed | testing-evidence/web/lighthouse/A11Y-01-login.json |
| A11Y-02 | Listings page (N) | Logged in | Same | Score ≥ 90; images have alt text; form labels present | Run 1: 89 (select-name, color-contrast, heading-order, label-content-name-mismatch) -> DEF-W-03; after labelling the filters: 93 (farmer and buyer dashboards) | Passed | testing-evidence/web/lighthouse-before-fix/, testing-evidence/web/lighthouse/ |
| A11Y-03 | Order form (N) | Logged in as Buyer | Same | Score ≥ 90; sufficient colour contrast; focusable controls | Buyer dashboard 93; the order modal was not audited separately | Passed | testing-evidence/web/lighthouse/A11Y-03-buyer-dashboard.json |
| A11Y-04 | Keyboard navigation (N) | Same | Tab through login and order flow | All controls reachable and operable by keyboard | Not run | Not Run | - |

---

### Area 8 — Compatibility (Playwright) — Owner: [Name 2]

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| COMP-01 | Login + browse on Chromium (N) | Playwright projects configured | Run suite on Chromium | All pass | All 12 Playwright tests pass on Chromium | Passed | testing-evidence/run-2026-10-07/web-playwright.txt |
| COMP-02 | Login + browse on Firefox (N) | Same | Run suite on Firefox | All pass | Not run: Firefox could not be downloaded (system drive full) | Not Run | - |
| COMP-03 | Login + browse on WebKit (N) | Same | Run suite on WebKit | All pass | Not run: WebKit could not be downloaded (system drive full) | Not Run | - |
| COMP-04 | Login + browse on Edge channel (N) | Edge installed | Run suite with `channel: 'msedge'` | All pass | Not run: Edge channel not available | Not Run | - |
| COMP-05 | Order flow across all browsers (N) | Same | Run WEB-E-02 on all projects | Same result in all; record pass/fail matrix | Chromium only (passed); other browsers not run | Not Run | testing-evidence/run-2026-10-07/web-playwright.txt |

---

### Area 9 — Security (OWASP ZAP) — Owner: [Name 4]

Scan only a local/test deployment. Authorization checks BE-A-05, BE-A-06, BE-A-07, BE-A-08, BE-A-13, and XP-W-05 complement these scans.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| SEC-01 | Baseline passive scan (N) | API running on test host; OpenAPI spec | ZAP baseline scan | Report generated; 0 High-risk alerts | ZAP API scan (passive checks, safe mode) after the fixes: 0 FAIL, 2 WARN (Low), 116 PASS | Passed | testing-evidence/run-2026-10-07/zap-after-fix-output.txt, zap-after-fix/zap-report.html |
| SEC-02 | Active scan with auth (N) | Valid JWT configured in ZAP | ZAP full/active scan of API | 0 High; Medium alerts documented | ZAP active scan with an officer JWT, 84 URLs, 18,422 requests: 0 High, 0 Medium, 4 Low (nosniff header, CORP header, timestamp disclosure, content-type). nosniff fixed, 2 Low remain | Passed | testing-evidence/run-2026-10-07/zap/zap-report.html, zap-output.txt |
| SEC-03 | SQL injection (I) | Search/filter endpoints | Inject payloads in query parameters | No SQL errors or data leakage; 400/empty result | SQL injection payloads in search and sortBy: no 5xx, no SQL text, no extra rows | Passed | security_checks SEC-03, SEC-03b |
| SEC-04 | XSS in listing text (I) | Farmer account | Create listing with `<script>` in description; view in web app | Script not executed; output encoded | Not run dynamically. Static check: the web app has no dangerouslySetInnerHTML, so React escapes listing text | Not Run | grep web/src |
| SEC-05 | Security headers / CORS (N) | API running | Inspect scan results | Sensible CORS policy; no sensitive info in error responses | Malformed JSON and malformed id: no stack trace; CORS does not allow an arbitrary origin | Passed | security_checks SEC-05a/b/c |
| SEC-06 | Brute-force login (I) | API running | Repeated failed logins (script or ZAP fuzzer) | Account/IP throttled or locked, or finding recorded as defect | Run 1 FAILED: 40 wrong passwords in a row were all answered 401, never throttled (DEF-S-01). After the fix: locked out after 5 failures (429 + Retry-After). Retest passed | Passed | testing-evidence/run-2026-10-07/security-checks-BEFORE-fix-output.txt, security-checks-AFTER-fix-output.txt |
| SEC-07 | Price tampering (I) | Buyer token | Submit order with client-supplied lower price | Server ignores client price; order uses server-side price | Order with client-supplied price/status accepted as Pending; client values ignored | Passed | security_checks SEC-07 |

---

### Area 10: Agentic AI Testing and Evaluation (pytest, deterministic cases): Owner: [Name 1]

The assignment lists this as a testing area; the original plan did not. Cases use a mock LLM and fake models, so they are deterministic and need no API key.

| ID | Feature | Preconditions | Steps / Input | Expected Result | Actual Result | Status | Evidence / automated test |
|---|---|---|---|---|---|---|---|
| AI-01 | Task completion: golden cases 1-3 (N) | Logistics agent, mock tools (60-min slots, 8/day, daily lunch booking) | No conflicts / centre at capacity / partial conflicts | First free slot; next day when full; slot after the last booking | All three proposals correct | Passed | pytest test_golden_1, _2, _3 |
| AI-02 | Required input (I) | Same | Missing order id; end before start; unknown field | Validation error, no proposal | Rejected in all three | Passed | pytest test_golden_4, test_invalid_window_*, test_unknown_input_field_is_rejected |
| AI-03 | Structured-output validation (I) | Fake LLM replies | Prose, truncated JSON, moved slot, lunch-booking slot, conflictChecked=false, empty reasoning, extra key | Every malformed reply rejected | 7 of 7 rejected (ValidationError) | Passed | pytest test_malformed_llm_output_raises_validation_error |
| AI-04 | Tool use and failure of tools (F) | Real capacity/calendar tools with a faked HTTP layer | Malformed bookings; HTTP 500; valid capacity | Validated; errors wrapped as ToolError | As expected | Passed | pytest test_real_calendar_tool_*, test_real_capacity_tool_* |
| AI-05 | Business rules: matching (N/B) | Matching agent, stub distance tool | Closest centre; full centre skipped; all full; no candidates | Closest with capacity; no match with an explanation | As expected, confidence within 0..1 | Passed | pytest test_buyer_farmer_matching_agent.py (12 tests) |
| AI-06 | Prompt injection in a data field (I) | Order id / centre name containing "IGNORE ALL PREVIOUS INSTRUCTIONS..." | Run the agents | Output unchanged; instruction not obeyed | Same slot and same centre as without the injection | Passed | pytest test_ai_safe_01, test_ai_safe_05 |
| AI-07 | A model that obeys the injection (F) | Fake LLM that books 03:00 / names the far centre | Run the agents | Caught by validation or discarded | Slot rejected (ValidationError); misleading narration discarded | Passed | pytest test_ai_safe_02, test_ai_safe_05 |
| AI-08 | Approval enforcement: AI cannot commit (N) | Agent contracts; API workflow | Inspect output contracts; approve an order; officer confirms | No commit field; slot stays Proposed until a human confirms | Contracts have no status field; xUnit and Playwright show Proposed -> officer Approve -> Scheduled | Passed | pytest test_ai_safe_03, _04; xUnit OrderWorkflowApiTests; Playwright XP-W-01 |
| AI-09 | Failure recovery and safe failure (F) | LLM unavailable / quota error | Call the endpoints | 502 for the schedule agent; matching falls back to deterministic notes | 502 returned; matching answered with the same centre | Passed | pytest test_endpoint_returns_502_when_llm_unavailable, test_llm_failure_falls_back_to_the_notes, test_ai_safe_06 |
| AI-10 | No capacity anywhere (B) | All centres full | Request a slot | 409 / no match, never a made-up slot | 409 returned; "no match" with notes | Passed | pytest test_endpoint_returns_409_when_no_slot, test_match_when_all_centres_full_* |
| AI-11 | Explanation guardrails (I) | LLM narration | Names an unknown centre; omits the chosen one; blank; too long | Discarded and replaced by deterministic notes | All discarded | Passed | pytest test_matching_validation.py (13 tests) |
| AI-12 | Service authentication (I) | Internal API key configured | Call without / with the right key | 401 without, 200 with | As expected | Passed | pytest test_match_endpoint_requires_correct_internal_api_key_when_configured |

---

## Part C: Traceability and Coverage Summary

| Area | Test IDs | Components covered | FR refs (SRS section 7) |
|---|---|---|---|
| Backend | BE-U-01..14, BE-A-01..14 | M1-M4, Auth | FR1-FR3, FR5, FR8-FR11, FR12, FR14, FR15, FR19, FR20, FR22 |
| Database | DB-01..10 | M1, M2, M3 | FR9, FR20 |
| Web | WEB-C-01..10, WEB-E-01..04 | M1, M2 | FR2, FR3, FR6, FR8 |
| Mobile | MOB-W/U/E | M1, M2, M4 | FR2, FR6, FR15 |
| Cross-Platform | XP-W-01..05, XP-M-01..04 | M1-M4, AI approval flow | FR8-FR11, FR19, FR20, FR22 |
| Performance | PERF-01..06 | M1, M2, M4 | FR6, FR9, NFR Performance, Scalability |
| Accessibility | A11Y-01..04 | Web | NFR Usability |
| Compatibility | COMP-01..05 | Web | NFR Reliability |
| Security | SEC-01..07 | API | FR2, NFR Security, Data Protection |
| Agentic AI | AI-01..12 | Logistics and Matching agents | FR10, FR19, FR21, NFR Safety |

---

## Part D: Defect / Bug Report

The full report (steps to reproduce, evidence, retest) is [SE3110_Defect_Report.md](SE3110_Defect_Report.md). Summary:

| Defect ID | Test ID | Severity | Status | Retest result |
|---|---|---|---|---|
| DEF-D-01 | (manual) D-M-61 | High | Fixed | Passed (defect-D-01-after.trx) |
| DEF-D-02, DEF-D-03 | (manual) D-M-30, D-M-40 | Medium | Fixed | Retest evidence not found in the repository |
| DEF-D-04 | (manual) D-M-62 | High | Fixed | Passed (price-edit-after-fix.txt) |
| DEF-D-05 | (manual) D-M-60 | High | Fixed | Passed (defect-D-05-after.trx) |
| DEF-P-01 | PERF (cloud DB run) | High | Fixed | Passed: 702/702 requests 500 before, 750/750 503 + Retry-After after |
| DEF-P-02 | PERF (cloud DB run) | High | Open (configuration) | Not retested |
| DEF-S-01 | SEC-06 | High | Fixed | Passed (13/13 security checks) |
| DEF-S-02 | SEC-02 | Low | Fixed | Passed (ZAP alerts 4 -> 2) |
| DEF-S-03, DEF-S-04 | SEC-02 | Low | Accepted risk | n/a |
| DEF-W-01 | A11Y-02 | Medium | Fixed | Passed (Lighthouse 89 -> 93) |
| DEF-W-02 | A11Y-02 | Low | Open | n/a |
| DEF-T-01..04 | test suites | Medium | Fixed | Passed (89/89, 24/24, 12/12) |

---

## Part E: Test Execution Summary

Planned cases (Part B) after the final run on 2026-10-07:

| Area | Planned | Executed | Passed | Failed | Not Run | Defects found | Defects fixed |
|---|---:|---:|---:|---:|---:|---:|---:|
| Backend | 28 | 24 | 24 | 0 | 4 | 4 (DEF-D-01, 04, 05, P-01) | 4 |
| Database | 10 | 4 | 4 | 0 | 6 | 0 | 0 |
| Web | 14 | 13 | 13 | 0 | 1 | 2 (DEF-D-02, 03) | 2 |
| Mobile | 10 | 7 | 7 | 0 | 3 | 0 | 0 |
| Cross-Platform | 9 | 4 | 4 | 0 | 5 | 0 | 0 |
| Performance | 6 | 3 | 3 | 0 | 3 | 1 (DEF-P-02) | 0 |
| Accessibility | 4 | 3 | 3 | 0 | 1 | 2 (DEF-W-01, W-02) | 1 |
| Compatibility | 5 | 1 | 1 | 0 | 4 | 0 | 0 |
| Security | 7 | 6 | 6 | 0 | 1 | 4 (DEF-S-01..04) | 2 (2 accepted) |
| Agentic AI (added) | 12 | 12 | 12 | 0 | 0 | 0 | 0 |
| **Total** | **105** | **77** | **77** | **0** | **28** | **13** | **9** |

Failures seen on first runs (SEC-06, the DEF-P-01 behaviour, Lighthouse 89 on A11Y-02, 9 of 15 Vitest tests, 8 of 10 Playwright tests, 4 of 83 pytest tests) are recorded as defects; the table shows the result after retest. DEF-T-01..04 (test-suite maintenance) are not counted per area.

Automated suites (all tests, final run):

| Suite | Tool | Executed | Passed | Failed |
|---|---|---:|---:|---:|
| Backend unit/service/API/concurrency (local PostgreSQL) | xUnit | 378 | 378 | 0 |
| Agentic AI | pytest | 89 | 89 | 0 |
| Web components | Vitest | 24 | 24 | 0 |
| Web end-to-end and integrated workflow | Playwright (Chromium) | 12 | 12 | 0 |
| Mobile widgets and unit | flutter_test | 20 | 20 | 0 |
| Performance | k6 | 4 scenarios, 77,000+ checks | all thresholds met | 0 |
| Security | OWASP ZAP + scripted checks | 84 URLs scanned; 13 checks | 0 High/Medium; 13/13 | 0 |
| Accessibility | Lighthouse | 5 pages | 5/5 >= 90 | 0 |
| **Total tests (xUnit + pytest + Vitest + Playwright + flutter_test)** | | **523** | **523** | **0** |

**Conclusion.** Every executed case passes after fixes. 28 of 105 planned cases were not run: mobile integration and Appium cases (no emulator, no `integration_test` suite), Firefox/WebKit/Edge (browser downloads failed because the system drive was full), direct database-constraint tests, and some performance shapes (1-VU baseline, spike, recovery). The most important findings were a missing login throttle, the API answering 500 instead of 503 when the database is saturated, and a cloud-database connection limit (15 clients) that made the API unusably slow under 20 users. The first two are fixed and retested; the third needs a configuration change.

---

## Part F: Evidence Checklist

| Evidence | Owner | Saved path | Done |
|---|---|---|---|
| xUnit results (378) | [Name 1] | `run-2026-10-07/full-suite-after-fixes.trx`, `backend-dotnet-test-after-fixes.txt` | Yes |
| pytest results + coverage (89, 67%) | [Name 1] | `run-2026-10-07/agentic-ai-pytest.txt` | Yes |
| Newman report | [Name 1] | Not produced: API integration was done with xUnit `WebApplicationFactory` | No (replaced) |
| Testcontainers output | [Name 1] | Not produced: real local PostgreSQL used | No (replaced) |
| Vitest results + coverage | [Name 2] | `run-2026-10-07/web-vitest.txt`, `web-vitest-results.json` | Yes |
| Playwright report (Chromium) | [Name 2] | `testing-evidence/web/playwright-report/`, `run-2026-10-07/web-playwright.txt` | Yes (Chromium only) |
| Lighthouse reports | [Name 2] | `testing-evidence/web/lighthouse/`, `lighthouse-before-fix/` | Yes |
| flutter_test results + coverage | [Name 3] | `run-2026-10-07/mobile-flutter-results.jsonl`, `mobile-lcov.info` | Yes |
| integration_test / Appium logs | [Name 3] | not produced | No |
| k6 summary output | [Name 4] | `run-2026-10-07/k6-run-A-supabase-*`, `k6-run-B-local-*`, `k6-run-C-after-fixes-*`, `k6-dbdown-*` | Yes |
| OWASP ZAP report | [Name 4] | `run-2026-10-07/zap/zap-report.html`, `zap-after-fix/` | Yes |
| Scripted security checks | [Name 4] | `run-2026-10-07/security-checks-BEFORE-fix-*`, `-AFTER-fix-*` | Yes |
| Integrated workflow evidence | [Name 2] | `run-2026-10-07/xp-w-01-evidence.txt` | Yes |
| Git commit history per member | All | `git log --author` | To do |
