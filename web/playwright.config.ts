import { defineConfig, devices } from '@playwright/test'

// Runs the same specs on Chromium, Firefox and WebKit (COMP-01..05). Set EDGE=1 on a
// machine with Microsoft Edge installed to add the real Edge channel (COMP-04).
// The stack must be up first: web on :3000 and the API on :5000 (see tests/e2e/README.md).
// Set CHROMIUM_PATH to reuse an already-downloaded Chromium (e.g. when `playwright install`
// cannot download a matching build); unset, Playwright uses its own managed browser.
const chromiumPath = process.env.CHROMIUM_PATH
const projects = [
  {
    name: 'chromium',
    use: { ...devices['Desktop Chrome'], ...(chromiumPath ? { launchOptions: { executablePath: chromiumPath } } : {}) },
  },
  { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
  { name: 'webkit', use: { ...devices['Desktop Safari'] } },
]
if (process.env.EDGE) {
  projects.push({ name: 'edge', use: { ...devices['Desktop Edge'], channel: 'msedge' } })
}

export default defineConfig({
  testDir: 'tests/e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 45_000,
  expect: { timeout: 10_000 },
  reporter: [
    ['list'],
    ['html', { outputFolder: '../testing-evidence/web/playwright-report', open: 'never' }],
    ['junit', { outputFile: '../testing-evidence/web/playwright-junit.xml' }],
  ],
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:3000',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects,
})
