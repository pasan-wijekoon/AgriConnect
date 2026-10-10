# SE3110: Defect / Bug Report: AgriConnect

Evidence paths are relative to the repository root. `run-2026-10-07/` means `testing-evidence/run-2026-10-07/`.
Severity: Critical, High, Medium, Low. Priority: P1 fix before release, P2 fix soon, P3 when convenient.

## Summary

| ID | Title | Severity / Priority | Found by | Status | Retest |
|---|---|---|---|---|---|
| DEF-D-01 | Overpriced listing never reached the officer anomaly queue | High / P1 | Manual + xUnit | Fixed | Passed |
| DEF-D-02 | Shortages heatmap labels overlap | Medium / P2 | Manual | Fixed | Not evidenced |
| DEF-D-03 | Analytics tables and numbers near-invisible (contrast) | Medium / P2 | Manual | Fixed | Not evidenced |
| DEF-D-04 | Edited price never re-checked for anomalies | High / P1 | Manual script | Fixed | Passed |
| DEF-D-05 | Unapproved listing price inflated the AI fair price | High / P1 | xUnit | Fixed | Passed |
| DEF-P-01 | API answers 500 when the database is saturated or unreachable | High / P1 | k6 | Fixed | Passed |
| DEF-P-02 | API unusably slow against the cloud database (connection limit) | High / P1 | k6 | **Open** (configuration) | Not retested |
| DEF-S-01 | No throttling of repeated failed logins | High / P1 | Scripted security check | Fixed | Passed |
| DEF-S-02 | Responses lack `X-Content-Type-Options: nosniff` | Low / P3 | OWASP ZAP | Fixed | Passed |
| DEF-S-03 | `Cross-Origin-Resource-Policy` header missing | Low / P3 | OWASP ZAP | Accepted risk | n/a |
| DEF-S-04 | "Timestamp disclosure" alert | Low / P3 | OWASP ZAP | Accepted (false positive) | n/a |
| DEF-W-01 | Dashboard filter dropdowns have no accessible name | Medium / P2 | Lighthouse | Fixed | Passed |
| DEF-W-02 | Colour contrast, heading order, notification button name | Low / P3 | Lighthouse | **Open** | n/a |
| DEF-T-01 | AI tests read the developer's `.env` (real LLM calls, 4 failures, 6 min) | Medium / P2 | pytest | Fixed | Passed |
| DEF-T-02 | Web test tools not declared in `web/package.json` | Medium / P2 | Vitest | Fixed | Passed |
| DEF-T-03 | Vitest component tests out of date with the UI (9 of 15 failing, one suite not loading) | Medium / P2 | Vitest | Fixed | Passed |
| DEF-T-04 | Playwright specs out of date with the UI (8 of 10 failing) | Medium / P2 | Playwright | Fixed | Passed |

---

## DEF-P-01: API answers 500 when the database is saturated or unreachable

- **Severity / priority:** High / P1.
- **Description:** In the first k6 run (against the cloud database) the database refused connections and the API returned HTTP 500 with no retry hint; 1,057 unhandled exceptions were logged (`EMAXCONNSESSION`, `Failed to connect`, `connection pool has been exhausted`). A busy database is a temporary condition and should be a 503 with `Retry-After`, so clients and load balancers can back off.
- **Steps to reproduce:** Start the API with `ConnectionStrings__Default` pointing to a port nothing listens on and `ASPNETCORE_ENVIRONMENT=Production`. Request `GET /api/analytics/filters` with a valid officer JWT. Run `testing-evidence/performance/pool-exhaustion.js` (k6, 50 VUs, 15 s).
- **Expected:** 503 Service Unavailable, `Retry-After`, no internal details. **Actual (before):** 500.
- **Evidence (before):** `run-2026-10-07/k6-dbdown-BEFORE-fix-output.txt` (702 requests, `status_500` 702, `status_503` 0); `run-2026-10-07/k6-run-A-supabase-output.txt`; `run-2026-10-07/defect-P-01-before.trx` (3 of 6 new unit tests fail on the old handler).
- **Fix:** `backend/src/config/ApiExceptionHandler.cs` maps database-unreachable, pool-timeout and "max clients reached" errors to 503 with `Retry-After: 5` and a fixed message (no host names). Serialization failures (40001) are deliberately left to the stock-reservation retry loop. Tests: `backend.Tests/services/ApiExceptionHandlerTests.cs`.
- **Retest:** `run-2026-10-07/k6-dbdown-AFTER-fix-output.txt`: 750 requests, `status_503` 750, `responses_503_with_retry_after` 750, `status_500` 0. `run-2026-10-07/defect-P-01-after.trx`: 6 of 6 pass. Full suite afterwards: 378 of 378 pass. **Status: Fixed, retest passed.**

## DEF-P-02: API unusably slow against the cloud database (connection limit)

- **Severity / priority:** High / P1.
- **Description:** Run A of the k6 test used the configured cloud database (Supabase session pooler). The pooler allows 15 clients (`EMAXCONNSESSION ... pool_size: 15`) while Npgsql's default is 100 connections per API instance, so with only 20 concurrent users 28% of browse requests failed, browse p95 was 4.84 s (target 500 ms) and login p95 was 15.04 s. The same API and test on a local PostgreSQL (run B/C) gave browse p95 36-39 ms, login p95 166-203 ms and 0% errors, so the application code is not the cause. The 378-test backend suite also takes 4 min 32 s on the cloud database against 31 s locally.
- **Steps to reproduce:** `k6 run testing-evidence/performance/agriconnect-performance.js` against an API configured with the cloud connection string.
- **Evidence:** `run-2026-10-07/k6-run-A-supabase-output.txt` (cloud) versus `k6-run-B-local-output.txt`, `k6-run-C-after-fixes-output.txt` (local).
- **Recommended fix (not applied, it is deployment configuration):** add `Maximum Pool Size=10` (below the pooler limit) to the connection string, or use the transaction pooler (port 6543), and size the Render instance count accordingly. **Status: Open.** Not retested because load tests must not be repeated against the shared database.
- **Note:** run A saturated the shared database's connection pool for about 3 minutes; teammates or the deployed site may have seen errors during that time.

## DEF-S-01: No throttling of repeated failed logins

- **Severity / priority:** High / P1 (OWASP A07, brute-forceable passwords).
- **Description:** 40 wrong passwords in a row for one account were all answered 401; the account was never slowed or locked.
- **Steps to reproduce:** `uv run --project agentic-ai python testing-evidence/security/security_checks.py` (check SEC-06), or POST `/api/auth/login` 40 times with a wrong password.
- **Expected:** throttled (429/423) after a few failures. **Actual (before):** `[401]` only.
- **Evidence (before):** `run-2026-10-07/security-checks-BEFORE-fix-output.txt` (12 passed, 1 failed).
- **Fix:** `backend/src/services/LoginAttemptTracker.cs` (5 failures per account and client address within 15 minutes, then 429 with `Retry-After`; a successful login clears the count) wired into `AuthController.Login`; singleton registered in `Program.cs`. Key is account plus client address so an attacker cannot lock a real user out from elsewhere. State is in memory (per instance). Tests: `LoginAttemptTrackerTests` (5), `AuthThrottleApiTests` (3).
- **Retest:** `run-2026-10-07/security-checks-AFTER-fix-output.txt`: 13 of 13 pass. The throttle was also seen working by accident: the Lighthouse run locked the buyer account on `127.0.0.1` after the 40 failed attempts, until the API was restarted. **Status: Fixed, retest passed.**

## DEF-S-02 / DEF-S-03 / DEF-S-04: ZAP low-risk alerts

- **Evidence:** `run-2026-10-07/zap/zap-report.html` (active scan, authenticated as an officer, 84 URLs, 18,422 requests): 0 High, 0 Medium, 4 Low.
- **DEF-S-02** `X-Content-Type-Options` missing: fixed with a small middleware in `Program.cs`. Retest `run-2026-10-07/zap-after-fix-output.txt`: that alert and "Unexpected Content-Type" no longer appear (WARN 4 to 2); `AuthThrottleApiTests.EveryResponse_CarriesNosniff` passes.
- **DEF-S-03** `Cross-Origin-Resource-Policy` missing: **accepted**. Photos are loaded by the web app from another origin, and `same-origin` would break them. Revisit when the front-end and API are on the same site.
- **DEF-S-04** "Timestamp disclosure - Unix" (2 hits): **accepted**, the values are ordinary business timestamps in JSON.

## DEF-W-01 / DEF-W-02: Accessibility (Lighthouse)

- **DEF-W-01 (Medium):** the farmer and buyer dashboards scored 89, below the 90 target. Six filter `<select>` elements had no label (`select-name`). **Fix:** `aria-label` on the six selects in `BuyerDashboard.tsx` and `FarmerDashboard.tsx`. **Evidence:** `testing-evidence/web/lighthouse-before-fix/` (89) and `testing-evidence/web/lighthouse/` (93); `run-2026-10-07/web-lighthouse-BEFORE-fix-output.txt`, `...-AFTER-fix-output.txt`. Vitest 24 of 24 and Playwright 12 of 12 still pass. **Status: Fixed, retest passed.**
- **DEF-W-02 (Low, open):** remaining audits on those pages: colour contrast of small grey and green text (ratio 3.0 to 4.3, needs 4.5), a heading skipping a level, and the notification button's accessible name not containing its visible text. Not fixed (design-token change in another member's pages).

## DEF-D-01 .. DEF-D-05: Component D defects (found and fixed before this run)

Recorded in `documentation/Manual_Test_Log_ComponentD.md`; fixes in commits `079d5f2`, `042af83`, `ca00cbe`.

| ID | Description | Fix commit | Evidence |
|---|---|---|---|
| DEF-D-01 (High) | An overpriced listing never reached the Anomaly Queue (FR16) because nothing called the anomaly check | `079d5f2` | `testing-evidence/backend/defect-D-01-before.trx` (1 of 3 fail), `defect-D-01-after.trx` (75 of 75 pass) |
| DEF-D-02 (Medium) | Shortages heatmap: labels overlap | `042af83` | Retest screenshots are referenced in the log but are not in the repository |
| DEF-D-03 (Medium) | Analytics tables and tile numbers near-invisible (light text on light cards) | `042af83` | As above |
| DEF-D-04 (High) | A raised price after creation never reached the Anomaly Queue | `ca00cbe` | `testing-evidence/flow/price-edit-before-fix.txt`, `price-edit-after-fix.txt`, `defect-D-04-after.trx` (94 of 94) |
| DEF-D-05 (High) | An unapproved, absurdly priced listing inflated the AI fair price of the next one | `ca00cbe` | `testing-evidence/backend/defect-D-05-before.trx` (fail), `defect-D-05-after.trx` (95 of 95) |

## DEF-T-01 .. DEF-T-04: Test-suite defects

| ID | Description | Cause | Fix | Retest |
|---|---|---|---|---|
| DEF-T-01 | 4 of 83 pytest tests failed and the run took 6 minutes | `main.py` calls `load_dotenv()`, so the developer's local `.env` (real Gemini key, `TOOLS_MODE=mock`) leaked into the tests; one test made real LLM calls | `agentic-ai/tests/conftest.py` pins `LLM_PROVIDER=mock` and disables `load_dotenv` | 83/83 in 20 s, later 89/89 |
| DEF-T-02 | `npx vitest` could not run on a clean checkout | vitest, jsdom, Testing Library, jest-dom and `@playwright/test` were not in `web/package.json` | Added as devDependencies (also `lighthouse`) | Vitest and Playwright run |
| DEF-T-03 | 9 of 15 Vitest tests failed, one suite did not load | Tests older than the UI: `LoginPage` now needs a Router, the saved session is revalidated through `api.getMe`, `DevIdentityContext` was removed, the order form pre-fills the available stock, the officer lands on the back-office shell | Tests updated (no app code changed) | 24/24 |
| DEF-T-04 | 8 of 10 Playwright tests failed | Back office now requires an officer login, header badge text includes the region, "Marketplace" link renamed "Dashboard", `svg path` matched sidebar icons, login not awaited before `goto` | Specs updated, `tests/e2e/helpers.ts` added | 10/10, then 20/20 with `--repeat-each=2`, final 12/12 |

---

## Open questions and limitations

- Firefox, WebKit and Edge runs, mobile `integration_test`/Appium runs and direct database-constraint tests were not done (see Part E of the Test Case Document).
- All backend tests of this report's final run, k6 and ZAP used a local PostgreSQL. The earlier backend run (364 tests) and k6 run A used the configured cloud database.
