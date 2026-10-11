# Screenshots of tool evidence (SE3110, run of 2026-10-07)

Generated from the saved tool output in `testing-evidence/run-2026-10-07/` and `testing-evidence/web/` by headless Chromium.
The ZAP and Lighthouse images are screenshots of the tools' own HTML reports. The k6, security-check and xUnit images are the saved terminal output rendered in a terminal-style page (they are not live terminal captures).

| Image | What it shows | Used for |
|---|---|---|
| `k6-run-C-after-fixes-summary.png` | k6 on local PostgreSQL: all thresholds met, 74,789 checks, FR9 stock arithmetic (370 kg -> 37 orders) | PERF-02, PERF-03, PERF-04 |
| `k6-run-A-cloud-db-summary.png` | k6 against the cloud database: thresholds crossed (p95 4.8 s, 28% failed) | DEF-P-02 |
| `k6-dbdown-BEFORE-fix.png` / `k6-dbdown-AFTER-fix.png` | Database unreachable: 702 x HTTP 500 before; 750 x 503 + Retry-After after | DEF-P-01 |
| `security-checks-BEFORE-fix.png` / `security-checks-AFTER-fix.png` | Scripted security checks: 12/13 (SEC-06 failed) then 13/13 | DEF-S-01, SEC-03..07 |
| `zap-active-scan-report.png`, `zap-active-scan-summary.png` | OWASP ZAP active scan, officer JWT: 0 High, 0 Medium, 4 Low | SEC-02 |
| `zap-after-fix-report.png`, `zap-passive-rescan-after-fix-summary.png` | ZAP re-scan after fixes: 2 Low left | SEC-01, DEF-S-02 |
| `lighthouse-buyer-BEFORE-fix.png` / `lighthouse-buyer-AFTER-fix.png` | Lighthouse accessibility 89 then 93 | A11Y-02, DEF-W-01 |
| `xunit-378-pass.png` | `dotnet test`: 378 passed | Backend suite |
