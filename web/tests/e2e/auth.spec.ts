import { expect, test } from '@playwright/test'
import { ACCOUNTS, roleBadge, signIn } from './helpers'


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
    await expect(roleBadge(page)).toContainText('Farmer')
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
    await expect(roleBadge(page)).toContainText('Buyer')
  })
})

test.describe('Role isolation (XP-W-05)', () => {
  test('a buyer never sees officer tools or the back-office link', async ({ page }) => {
    await signIn(page, ACCOUNTS.buyer)
    await expect(roleBadge(page)).toContainText('Buyer')

    await expect(page.getByRole('link', { name: 'Inspection Queue' })).toHaveCount(0)
    await expect(page.getByRole('link', { name: 'Price Trends' })).toHaveCount(0)
  })

  test('an officer is offered the back office and can open it', async ({ page }) => {
    await signIn(page, ACCOUNTS.officer)

    await expect(page.getByRole('link', { name: 'Inspection Queue' })).toBeVisible()
    await page.getByRole('link', { name: 'Orders', exact: true }).click()

    await expect(page).toHaveURL(/\/orders$/)
  })
})
