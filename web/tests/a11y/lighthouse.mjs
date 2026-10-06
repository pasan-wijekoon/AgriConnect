// Lighthouse accessibility audit (A11Y-01..03) of the running web app.
// Uses Playwright's Chromium so signed-in pages can be audited: the session lives in
// localStorage, so we sign in once in a persistent profile and Lighthouse reuses it.
// Usage (inside the Playwright container, see tests/e2e/README.md):  node tests/a11y/lighthouse.mjs
import fs from 'node:fs'
import lighthouse from 'lighthouse'
import { chromium } from '@playwright/test'

const BASE = process.env.BASE_URL ?? 'http://localhost:3000'
const OUT = process.env.OUT_DIR ?? '../testing-evidence/web/lighthouse'
const PORT = 9333
const THRESHOLD = 90

const pages = [
  { id: 'A11Y-01-login', path: '/', as: null },
  { id: 'A11Y-02-farmer-dashboard', path: '/', as: 'farmer@agriconnect.lk' },
  { id: 'A11Y-03-buyer-dashboard', path: '/', as: 'buyer@agriconnect.lk' },
  { id: 'A11Y-04-price-trends', path: '/analytics/price-trends', as: null },
  { id: 'A11Y-05-ai-scheduling', path: '/analytics/ai-scheduling', as: null },
]

fs.mkdirSync(OUT, { recursive: true })
const summary = []

for (const target of pages) {
  const context = await chromium.launchPersistentContext(`/tmp/lh-${target.id}`, {
    args: [`--remote-debugging-port=${PORT}`, '--no-sandbox'],
  })
  const page = context.pages()[0] ?? (await context.newPage())

  if (target.as) {
    await page.goto(BASE)
    await page.getByPlaceholder('name@agriconnect.lk').fill(target.as)
    await page.locator('input[type="password"]').fill('password')
    await page.locator('form button[type="submit"]').click()
    await page.getByPlaceholder('name@agriconnect.lk').waitFor({ state: 'hidden' })
  }

  const result = await lighthouse(
    `${BASE}${target.path}`,
    { port: PORT, output: ['json', 'html'], onlyCategories: ['accessibility'], logLevel: 'error' },
  )
  const score = Math.round(result.lhr.categories.accessibility.score * 100)
  const failed = Object.values(result.lhr.audits)
    .filter((a) => a.score === 0 && a.scoreDisplayMode === 'binary')
    .map((a) => a.id)

  fs.writeFileSync(`${OUT}/${target.id}.json`, result.report[0])
  fs.writeFileSync(`${OUT}/${target.id}.html`, result.report[1])
  summary.push({ id: target.id, url: `${BASE}${target.path}`, score, pass: score >= THRESHOLD, failedAudits: failed })
  console.log(`${target.id}: ${score}  ${score >= THRESHOLD ? 'PASS' : 'FAIL'}  ${failed.join(', ')}`)

  await context.close()
}

fs.writeFileSync(`${OUT}/summary.json`, JSON.stringify(summary, null, 2))
