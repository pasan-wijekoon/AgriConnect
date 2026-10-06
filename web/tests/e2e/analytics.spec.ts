import { expect, test } from '@playwright/test'

// Component D back-office pages, run on every browser project (COMP-01..05).
test.describe('Market price analytics (back office)', () => {
  test('COMP-02 Price Trends opens on a crop that has prices and draws the chart', async ({ page }) => {
    await page.goto('/analytics/price-trends')

    const crop = page.locator('select').first()
    await expect(crop).toHaveValue(/.+/)
    await expect(crop.locator('option:checked')).not.toContainText('no data yet')
    await expect(page.getByText('Current average')).toBeVisible()
    await expect(page.locator('svg path').first()).toBeVisible()
  })

  test('COMP-03 crops without price history are labelled in the filter', async ({ page }) => {
    await page.goto('/analytics/price-trends')
    await expect(page.getByText('Current average')).toBeVisible()

    const labels = await page.locator('select').first().locator('option').allTextContents()

    expect(labels.some((l) => l.includes('(no data yet)'))).toBe(true)
    expect(labels.some((l) => !l.includes('(no data yet)'))).toBe(true)
  })

  test('COMP-04 switching to a crop with no data shows an empty state, not a broken chart', async ({ page }) => {
    await page.goto('/analytics/price-trends')
    await expect(page.getByText('Current average')).toBeVisible()
    const crop = page.locator('select').first()
    const noData = crop.locator('option', { hasText: '(no data yet)' }).first()

    await crop.selectOption({ label: (await noData.textContent())!.trim() })

    await expect(page.getByText('Current average')).toBeHidden()
    await expect(page.locator('main')).toContainText(/no .*(prices|data)/i)
  })

  test('COMP-05 the back-office sidebar reaches the other analytics pages', async ({ page }) => {
    await page.goto('/analytics/price-trends')

    await page.getByRole('link', { name: 'AI Scheduling' }).click()
    await expect(page).toHaveURL(/\/analytics\/ai-scheduling$/)

    await page.getByRole('link', { name: 'Marketplace' }).click()
    await expect(page).toHaveURL(/\/$/)
  })
})
