#!/usr/bin/env bash
# Runs the Playwright specs and the Lighthouse audit inside the official Playwright image.
# Use this when npm/browsers cannot be installed on the host. Requires the web app on
# :3000 and the API on :5000 (host), and Docker Desktop running.
#
#   bash web/tests/e2e/run-in-docker.sh            # Playwright, all browsers
#   bash web/tests/e2e/run-in-docker.sh lighthouse # accessibility audit
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd -W 2>/dev/null || pwd)"
PW_VERSION="$(node -p "require('./web/package.json').devDependencies['@playwright/test'].replace(/[^0-9.]/g,'')")"
MODE="${1:-playwright}"

# The browser inside the container must see the app at localhost:3000 and the API at
# localhost:5000 (the API only allows that origin for CORS), so forward both to the host.
FORWARD='for p in 3000 5000; do node -e "const n=require(\"net\");n.createServer(c=>{const s=n.connect($p,\"host.docker.internal\");c.pipe(s);s.pipe(c);c.on(\"error\",()=>s.destroy());s.on(\"error\",()=>c.destroy())}).listen($p)" & done; sleep 1'
case "$MODE" in
  lighthouse) CMD='npm install --no-save lighthouse --no-audit --no-fund >/dev/null && node tests/a11y/lighthouse.mjs' ;;
  *)          CMD='npx playwright test' ;;
esac

MSYS_NO_PATHCONV=1 docker run --rm --add-host=host.docker.internal:host-gateway \
  -v "$ROOT:/repo" -v agri_web_nm:/repo/web/node_modules -w /repo/web \
  "mcr.microsoft.com/playwright:v${PW_VERSION}-jammy" \
  bash -c "$FORWARD; $CMD"
