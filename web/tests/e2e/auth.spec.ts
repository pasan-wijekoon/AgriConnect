import { expect, test, type Page } from '@playwright/test'

// Seeded demo accounts (password "password"); see AgriConnectDbContext.
const ACCOUNTS = {
  farmer: 'farmer@agriconnect.lk',
  buyer: 'buyer@agriconnect.lk',
  officer: 'officer@agriconnect.lk',
}

async function signIn(page: Page, email: string, password = 'password') {
  await page.goto('/')
  await page.getByPlaceholder('name@agriconnect.lk').fill(email)
  await page.locator('input[type="password"]').fill(password)
  await page.locator('form button[type="submit"]').click()
}

test.describe('Sign in and sessions', () => {
  test('WEB-E-03 a wrong password shows an error and stays on the login page', async ({ page }) => {
    await signIn(page, ACCOUNTS.farmer, 'definitely-wrong')

    await expect(page.getByText(/API error 401|Invalid|incorrect|failed/i).first()).toBeVisible()
    await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeVisible()
    expect(await page.evaluate(() => localStorage.getItem('agriconnect_user'))).toBeNull()
  })

  test('COMP-01 a farmer can sign in and sees the farmer workspace', async ({ page }) => {
    await signIn(page, ACCOUNTS.farmer)

    await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeHidden()
    await expect(page.locator('header').getByText('Farmer', { exact: true })).toBeVisible()
    expect(await page.evaluate(() => JSON.parse(localStorage.getItem('agriconnect_user')!).role)).toBe('Farmer')
  })

  test('WEB-E-04 removing the session sends the user back to the login page', async ({ page }) => {
    await signIn(page, ACCOUNTS.buyer)
    await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeHidden()

    await page.evaluate(() => localStorage.removeItem('agriconnect_user'))
    await page.reload()

    await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeVisible()
  })

  test('WEB-E-04b the session survives a reload while signed in', async ({ page }) => {
    await signIn(page, ACCOUNTS.buyer)
    await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeHidden()

    await page.reload()

    await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeHidden()
    await expect(page.locator('header').getByText('Buyer', { exact: true })).toBeVisible()
  })
})

test.describe('Role isolation (XP-W-05)', () => {
  test('a buyer never sees officer tools or the back-office link', async ({ page }) => {
    await signIn(page, ACCOUNTS.buyer)
    await expect(page.locator('header').getByText('Buyer', { exact: true })).toBeVisible()

    await expect(page.getByText('Officer tools')).toHaveCount(0)
    await expect(page.getByRole('link', { name: /Back office/ })).toHaveCount(0)
  })

  test('an officer is offered the back office and can open it', async ({ page }) => {
    await signIn(page, ACCOUNTS.officer)

    await expect(page.getByText('Officer tools')).toBeVisible()
    await page.getByRole('link', { name: 'Open the back office' }).click()

    await expect(page).toHaveURL(/\/orders$/)
  })
})
