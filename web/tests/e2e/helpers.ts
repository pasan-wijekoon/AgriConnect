import { expect, type Page } from '@playwright/test'

// Seeded demo accounts (password "password"); see AgriConnectDbContext.
export const ACCOUNTS = {
  farmer: 'farmer@agriconnect.lk',
  buyer: 'buyer@agriconnect.lk',
  officer: 'officer@agriconnect.lk',
}

export async function signIn(page: Page, email: string, password = 'password') {
  await page.goto('/')
  await page.getByPlaceholder('name@agriconnect.lk').fill(email)
  await page.locator('input[type="password"]').fill(password)
  await page.locator('form button[type="submit"]').click()
}

// The header badge reads "<Role>" or "<Role> · <Region>", so match on containment.
export const roleBadge = (page: Page) => page.locator('.app-header-user-role')

// signIn plus waiting until the login form is gone, so a following page.goto() cannot abort the login.
export async function signedIn(page: Page, email: string) {
  await signIn(page, email)
  await expect(page.getByPlaceholder('name@agriconnect.lk')).toBeHidden()
}
