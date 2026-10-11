# SE3110 — Software Testing and Quality Evaluation
# Test Plan: AgriConnect

---

## 1. Introduction

### 1.1 Purpose

This document defines the test strategy, scope, tools, environment, and schedule for verifying the AgriConnect platform prior to release. AgriConnect is an integrated system consisting of an ASP.NET Core Web API backend, a PostgreSQL database, a React web application, a Flutter mobile application, and a Python-based Agentic AI subsystem. This plan covers functional and non-functional testing across every layer of that stack, plus the cross-platform workflows that tie them together.

### 1.2 Project Background

AgriConnect connects Farmers, Buyers, Collection-Centre Officers, and Administrators in a single marketplace covering produce listings, AI-assisted fair pricing, quality inspection, order placement and stock reservation, pickup/delivery scheduling, market analytics, and notifications. The system was built as four independently-developed components (Produce Listings & Price Discovery; Order & Collection-Centre Logistics; Quality Grading & Inspection; Market Price Analytics & Reporting) and merged onto a shared `integration` branch, making cross-component integration testing a first-class concern for this plan.

---

## 2. Scope

### 2.1 In Scope

Testing covers every category and tool listed in the Finalized Tools Table (§5), specifically:

- **Backend (API) testing** — unit-level correctness of business logic (xUnit) and integration-level correctness of exposed HTTP endpoints (Newman/Postman collections) for the ASP.NET Core Web API.
- **Database testing** — schema constraints, foreign-key enforcement, and cross-entity data relationships in PostgreSQL, exercised against a real, disposable database instance (Testcontainers for .NET) rather than an in-memory substitute, since several core guarantees (e.g. concurrency-safe stock reservation, CHECK constraints) do not hold under an in-memory provider.
- **Web application testing** — component-level correctness (Vitest) and full browser-driven end-to-end flows within the web app (Playwright).
- **Mobile application testing** — widget-level correctness (`flutter_test`) and on-device/emulator integration and end-to-end flows (`integration_test`).
- **Cross-platform integration testing** — complete workflows that cross the UI → API → database boundary, on both the web (Playwright) and mobile (Appium) clients.
- **Agentic AI testing and evaluation** — deterministic pytest cases for task completion, structured-output validation, business rules, prompt injection, approval enforcement and failure recovery of the Logistics Scheduling and Buyer-Farmer Matching agents (added to match the assignment's testing areas).
- **Non-functional testing** — performance/load/stress (k6), accessibility (Lighthouse), cross-browser compatibility (Playwright), and security vulnerability scanning (OWASP ZAP) plus scripted security checks.

### 2.2 Out of Scope

- Real, production-scale payment processing (explicitly out of scope for the product itself per the SRS — nothing to test).
- Fully autonomous AI decision-making — since the product itself never allows this, there is no "AI commits a transaction unattended" path to test; AI-proposal review/approval flows are in scope, but testing is limited to confirming a human approval step always exists and is enforced.
- Manual/exploratory testing procedures, penetration testing beyond OWASP ZAP's automated scan scope, and load testing of any external third-party service (Maps/Distance provider, LLM provider) — those are stubbed/mocked or exercised via their documented fallback paths instead.
- Localisation/multi-language testing (English-only at this stage, per SRS §6).
- Native iOS-specific and native Android-specific platform code paths beyond what `integration_test`/Appium can exercise without physical devices, if such devices are unavailable to the team.

### 2.3 Non-Functional Testing Selection and Justification

Performance and security testing are mandatory for this assessment. The additional types below were selected because they are relevant to AgriConnect's users and risks.

| Type | Status | Justification |
|---|---|---|
| Performance / Load / Stress (k6) | Required | Marketplace ordering peaks at harvest/market times, and stock reservation must stay correct under concurrent requests (SRS §8). |
| Security (OWASP ZAP) | Required | The API handles role-based data, prices, and orders; broken access control and injection are high-impact risks. |
| Accessibility (Lighthouse) | Selected | Farmers and buyers vary in literacy, device quality, and assistive-technology use. |
| Compatibility (Playwright) | Selected | Buyers and officers use different browsers; avoids implicitly testing on one engine only. |

---

## 3. Objectives

1. Verify that every Functional Requirement (SRS §7, FR1–FR22) behaves as specified, at the unit, integration, and end-to-end level appropriate to where the logic lives.
2. Verify that the four independently-developed components genuinely interoperate once merged — not just that each compiles and passes its own tests in isolation — with particular attention to data actually flowing correctly across component boundaries (e.g. a listing created via the Produce Listings module must be orderable via the Order module).
3. Verify database-level data integrity: constraints, foreign keys, and cascade/restrict behaviour hold under both normal and adversarial (concurrent, malformed, boundary) conditions.
4. Verify the system's non-functional qualities named in the SRS (§8): acceptable response time under load, high availability under peak conditions, safety of AI-driven changes (nothing commits without human approval), protection of financial/price data from invalid values, role-based access control, auditability, usability, and accessibility.
5. Identify and document defects with enough detail (steps, expected vs. actual result, environment) that they are reproducible and actionable by the development team, fix important defects, and retest them.
6. Produce a Test Case Document (companion to this plan) that gives full traceability from each Functional Requirement to the concrete test case(s) that verify it.

---

## 4. Testing Areas

Each row of the Finalized Tools Table (§5) is treated as one testing area with its own objective, mapped below.

| # | Testing Area | Objective |
|---|---|---|
| 1 | Backend Testing | Confirm business logic inside services/controllers is correct in isolation (xUnit), and that the API's actual HTTP contract (status codes, payload shapes, auth/role enforcement) is correct end-to-end without a UI in front of it (Newman). |
| 2 | Database Testing | Confirm the schema itself — not just the application code sitting on top of it — enforces the constraints the application depends on (uniqueness, CHECK constraints, FK integrity, cascade/restrict rules), using a real disposable PostgreSQL instance so results reflect production behaviour, not an approximation. |
| 3 | Web Testing | Confirm individual React components render and behave correctly in isolation (Vitest), and that a real browser session can complete a task end-to-end within the web app alone (Playwright). |
| 4 | Mobile Testing | Confirm individual Flutter widgets behave correctly in isolation (`flutter_test`), and that a real (or emulated) device session can complete a task end-to-end within the mobile app alone (`integration_test`). |
| 5 | Cross-Platform Integration Testing | Confirm that a single real-world workflow — initiated from a UI, hitting the real API, persisting to the real database, and reflected back in the UI — works correctly end-to-end, on both web (Playwright) and mobile (Appium). This is the level at which cross-component integration defects (e.g. a component's UI calling an endpoint another component silently changed) are caught. |
| 6 | Performance/Load/Stress Testing | Confirm the API meets acceptable response-time and throughput targets under expected normal load, and characterise its behaviour (degradation, error rate, recovery) under heavy concurrent load — particularly for the stock-reservation endpoint, which has an explicit correctness guarantee under concurrency. |
| 7 | Accessibility | Confirm the React web application meets a baseline level of WCAG compliance so it is usable by farmers and buyers using assistive technology. |
| 8 | Compatibility | Confirm the web application behaves consistently across the major browser engines (Chromium, Firefox, WebKit/Edge) rather than being implicitly tested on only one. |
| 9 | Security | Confirm the API's exposed endpoints are free of common vulnerability classes (OWASP Top 10) via automated scanning, complementing the manual authorization/IDOR checks already covered under Backend and Cross-Platform testing. |
| 10 | Agentic AI | Confirm the AI agents only propose, validate every model output, resist prompt injection and fail safely (pytest, deterministic cases with a mock LLM). |

---

## 5. Tools and Frameworks

The following tools were planned for this project. The deviations that were actually made are listed in section 5.1.

| Category | Type | Tools | Scope |
|---|---|---|---|
| Functional Testing | Backend Testing | xUnit (unit testing), Newman (API integration testing) | Unit testing and API integration testing |
| Functional Testing | Database Testing | Testcontainers for .NET (real PostgreSQL instance), EF Core | DB integration, constraint/FK enforcement, and data-relationship testing |
| Functional Testing | Web Testing | Vitest (unit/component testing), Playwright (E2E) | Component testing and within-platform end-to-end testing on the web app |
| Functional Testing | Mobile Testing | flutter_test (widget testing), integration_test (integration + E2E testing) | Widget testing, integration testing, and within-platform end-to-end testing on the mobile app |
| Functional Testing | Cross-Platform Integration Testing | Playwright (React UI → API → DB), Appium (Mobile UI → API → DB) | Complete workflows across web, mobile, API, DB |
| Non-Functional Testing | Performance/Load/Stress Testing | k6 | Response time, throughput, behavior under normal and heavy concurrent load |
| Non-Functional Testing | Accessibility | Lighthouse | WCAG compliance on React web app |
| Non-Functional Testing | Compatibility | Playwright | Run same test across Chromium, Firefox, Edge |
| Non-Functional Testing | Security | OWASP ZAP, scripted checks (Python + httpx) | Vulnerability scanning on API endpoints; injection, IDOR, brute-force, error leakage, CORS, price tampering |
| Functional Testing | Agentic AI Testing | pytest (+ pytest-cov), fake chat models | Task completion, schema validation, business rules, prompt injection, approval enforcement, failure recovery |

### 5.1 Tools actually used, and deviations from the plan

| Planned | Actually used | Reason |
|---|---|---|
| Newman (API integration) | xUnit with `WebApplicationFactory` (real routing, auth, validation, PostgreSQL) | Already in the repository; no extra Postman collection to maintain |
| Testcontainers for .NET | A real PostgreSQL instance (local, dedicated `agriconnect_perf` database) | Docker/Testcontainers not needed; the concurrency and constraint behaviour is still verified on real PostgreSQL |
| Appium (mobile cross-platform) | Not run | No emulator or device available in the test window |
| Mobile `integration_test` | Not run | No `integration_test` suite exists and no emulator was available |
| Playwright on Chromium, Firefox, WebKit, Edge | Chromium only | Browser downloads failed because the system drive was full |
| k6 at 50 VUs for 5 min and 50 to 300 VUs | 20 VUs for 30 s, ramp to 200 VUs, login at 10 VUs, order race at 30 VUs | Time and a single developer machine; thresholds kept (p95 < 500 ms, error rate < 1%) |
| OWASP ZAP full scan | ZAP API scan from the OpenAPI spec (active, authenticated as an officer) plus a passive re-scan after fixes | Officer role only; Administrator-only endpoints return 403 and were not scanned with admin rights |
| (not planned) Agentic AI area | pytest, 89 tests (including 6 safety cases) | Listed in the assignment |

Performance and security tests were run only against a local, disposable database. A first k6 run against the configured cloud database showed a connection-limit problem (DEF-P-02) and must not be repeated there.

---

## 6. Test Environment

### 6.1 Operating Systems

| Environment | OS |
|---|---|
| Local development / manual testing | Windows 11 , build 26200 or later |
| CI pipeline  | Ubuntu 22.04 LTS (GitHub Actions `ubuntu-latest` runner) |
| Mobile build host | Windows 11 (Android toolchain)  |

### 6.2 Backend(with APIs)

| Component | Version |
|---|---|
| .NET SDK / runtime | .NET 10 (net10.0) |
| ASP.NET Core | 10.x |
| Entity Framework Core | 10.0.12 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| xUnit | latest stable compatible with .NET 10 SDK |
| Newman | latest npm release (`npm install -g newman`) |
| Testcontainers for .NET | latest stable, `postgres:18` image pinned to match production PostgreSQL version |

### 6.3 Database

| Component | Version |
|---|---|
| PostgreSQL | 18 |
| Connection | Local instance for development; ephemeral Testcontainers-managed instance for automated DB tests (never the shared dev database) |

### 6.4 Web Application

| Component | Version |
|---|---|
| Node.js | 22.6 or later (required for native `node --test`/Vitest tooling used by this project; older Node versions are known to fail running the project's own test scripts) |
| React | 19.x |
| Vite | 8.x |
| Vitest | latest stable compatible with the above |
| Playwright | latest stable, with browser binaries installed for: |
| — Chromium | bundled Playwright Chromium build |
| — Firefox | bundled Playwright Firefox build |
| — WebKit (Edge/Safari proxy) | bundled Playwright WebKit build, plus a native Microsoft Edge (Chromium-based) channel install for a true Edge compatibility pass |
| Lighthouse | latest stable, run via `lighthouse-ci` or the Chrome DevTools/CLI integration |

### 6.5 Mobile Application

| Component | Version |
|---|---|
| Flutter SDK | channel stable, Dart SDK ^3.12.2 (per `mobile/pubspec.yaml`) |
| Android emulator | Android 14 (API level 34), Pixel 6 profile, x86_64 image |
| iOS simulator (if a macOS host is available) | iOS 17, iPhone 15 profile |
| Appium | 2.x, with the `uiautomator2` driver (Android) and `xcuitest` driver (iOS, if applicable) |

### 6.6 Agentic AI Subsystem

| Component | Version |
|---|---|
| Python | 3.12+ |
| Package manager | `uv` |
| LLM provider mode | `mock` for all automated test runs (deterministic, no external API key/network dependency); `gemini` mode exercised only in a separate, manual smoke-test pass |

### 6.7 Non-Functional Tooling

| Tool | Notes |
|---|---|
| k6 | Run from a dedicated load-generation machine/CI runner, never the developer workstation running the system under test, to avoid resource contention skewing results. If this is not possible, machine specifications are recorded and the limitation is noted with the results. |
| OWASP ZAP | Run in automated (baseline/full scan) mode against a dedicated test deployment of the API — never against a production instance |

---

## 7. Team Members and Assigned Testing Areas



| Name | Student ID | Assigned Testing Area(s) | Assigned Tools |
|---|---|---|---|
| Adithya M A D K | IT24102690 | Backend Testing, Database Testing | xUnit, Newman, Testcontainers for .NET, EF Core |
| Marasinghe M A Y D | IT24102714 | Web Testing, Accessibility, Compatibility | Vitest, Playwright, Lighthouse |
| Wijekoon W H M P V P | IT24103113 | Mobile Testing, Cross-Platform Integration Testing | flutter_test, integration_test, Appium |
| Subasingha S A P R  | IT24103048 | Performance/Load/Stress, Security | k6, OWASP ZAP |

Each member is responsible for the test code, execution, results, and defects of their own area, and must be able to run, explain, and modify those tests in the individual viva. Each member's test code must be traceable through their own Git commits.

---

## 8. Entry and Exit Criteria

### 8.1 Entry Criteria (before testing for a given area begins)

- The relevant feature(s) are merged onto the branch under test and the application builds without errors.
- Test data / fixtures required for the area are available (seeded reference data, at least one account per role, at least one listing in each relevant status).
- The tools listed in §5 for that area are installed and configured in the environment described in §6.

### 8.2 Exit Criteria (before testing for a given area is considered complete)

- All planned test cases for the area (see the companion Test Case Document) have been executed at least once.
- No open **Critical** or **High** severity defect remains unresolved for that area, unless explicitly deferred and accepted by the team/instructor.
- Test results (Actual Result, Pass/Fail) are recorded in the Test Case Document for every executed case.
- Important defects that were fixed have been retested, with the retest result recorded in the Defect / Bug Report.
- For non-functional areas specifically: the measured metric (response time, WCAG score, browser pass/fail matrix, vulnerability count by severity) is recorded against the target defined in that area's test cases.

---

## 9. Test Schedule (indicative)


| Phase | Date | Activity | Areas Covered |
|---|---|---|---|
| 1 | 3 Oct | Plan and test cases drafted; first k6 smoke run; earlier Component D defects fixed | Planning, Performance |
| 2 | 3-6 Oct | Backend, web and mobile test code written by the team; backend report of 6 Oct (364 tests) | Backend, Web, Mobile |
| 3 | 7 Oct | Full re-run of every suite; fixes to test infrastructure; k6 (3 runs), ZAP active and passive scans, scripted security checks, Lighthouse, integrated workflow, Agentic AI safety cases | All |
| 4 | 7 Oct | Defects fixed and retested (DEF-P-01, DEF-S-01, DEF-S-02, DEF-W-01, DEF-T-01..04) | Backend, Security, Web |
| 5 | 7-8 Oct | Test Case Document, Defect Report, Software Testing Report (PDF), AI-use declaration, final commit, CourseWeb submission (deadline 8 Oct) | All |


---

## 10. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Automated DB tests run against the shared development database instead of an isolated one, corrupting dev data or producing flaky results from leftover state | Medium | High | Use Testcontainers exclusively for automated DB tests; never point them at the shared dev connection string. |
| External dependencies (LLM provider, Maps/Distance provider) are unavailable or rate-limited during a test run | Medium | Medium | Run the Agentic AI subsystem in `mock` provider mode for all automated runs; only exercise real-provider mode in a separate, explicitly-scheduled manual smoke test. |
| Cross-component integration defects (a component's UI silently drifting from another component's API contract after independent development) go undetected until late | High | High | Prioritise Cross-Platform Integration Testing early rather than leaving it to the end; treat it as equally mandatory as unit testing, not a "nice to have" final pass. |
| Load testing against a resource-constrained developer machine produces misleading performance numbers | Medium | Medium | Run k6 load tests from a separate CI runner/dedicated machine, sized comparably to the intended deployment target; otherwise record machine specs with the results. |
| Security scanning (OWASP ZAP) is accidentally pointed at a production or shared environment | Low | Critical | Restrict ZAP scans to a dedicated, isolated test deployment; never scan production. |
| Very short remaining schedule before the 8 Oct deadline | High | High | Members work in parallel on their own areas; prioritise one complete E2E workflow, k6, and ZAP first. |
| Realised: the system drive filled up (browser and Docker image downloads failed) and the configured database was a shared cloud database | High | High | Used a local disposable PostgreSQL for load and security tests; Chromium only; recorded as limitations |
| A member cannot explain or reproduce AI-assisted tests in the viva | Medium | High | Every member verifies, runs, and understands their own tests; AI usage is declared per the module requirements and CLEAR framework. |

---

## 11. Deliverables

1. This Test Plan (`SE3110_Test_Plan.md`).
2. The companion Test Case Document (`SE3110_Test_Case_Document.md`), including Actual Result and Pass/Fail for every case after execution, plus the Defect / Bug Report and Test Execution Summary.
3. Raw tool output/reports retained as evidence: xUnit/Newman run logs, Testcontainers test logs, Vitest/Playwright reports (including the Playwright HTML report for the cross-browser compatibility matrix), Flutter/Appium test logs, k6 summary output, Lighthouse reports, and the OWASP ZAP scan report.
4. A defect log summarising any Critical/High severity issues found, their status, and resolution, with retest evidence.
5. Software Testing Report (PDF) containing the test plan, scope, execution summary, defect summary, and conclusion.
6. Automated test source code/scripts, the GitHub repository link with per-member commit evidence, and any configuration needed to rerun the tests.
7. AI-usage declaration per the module requirements and the CLEAR framework.
