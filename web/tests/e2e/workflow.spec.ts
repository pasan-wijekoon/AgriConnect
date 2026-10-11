import { expect, test, type APIRequestContext } from '@playwright/test'
import { ACCOUNTS, signedIn } from './helpers'

// XP-W-01, XP-W-02, XP-W-05b: one complete business workflow across every layer: React UI -> ASP.NET Core API ->
// PostgreSQL -> Agentic AI (the logistics agent proposes the pickup slot) -> back to the UI.
// Needs the full stack: web on :3000, API on :5000 (seeded), agentic-ai on :8000.
const API = process.env.API_URL ?? 'http://localhost:5000'
const QUANTITY = 5

async function apiLogin(request: APIRequestContext, email: string) {
  const res = await request.post(`${API}/api/auth/login`, { data: { email, password: 'password' } })
  expect(res.status()).toBe(200)
  return (await res.json()).token as string
}

const bearer = (token: string) => ({ Authorization: `Bearer ${token}` })

test('XP-W-01 buyer orders, officer approves and confirms the AI-proposed slot, order completes', async ({ browser, request }) => {
  test.setTimeout(150_000)
  const buyerToken = await apiLogin(request, ACCOUNTS.buyer)
  const officerToken = await apiLogin(request, ACCOUNTS.officer)

  // A published listing with enough stock.
  const listings = await (await request.get(`${API}/api/listings?pageSize=50`, { headers: bearer(buyerToken) })).json()
  const listing = listings.items.find((l: { availableQuantity: number }) => l.availableQuantity >= 50)
  expect(listing, 'a published listing with stock').toBeTruthy()
  const stockBefore: number = listing.availableQuantity

  // --- Buyer, in the browser: open the listing and place the order -------------------------
  const buyerPage = await (await browser.newContext()).newPage()
  await signedIn(buyerPage, ACCOUNTS.buyer)
  await buyerPage.getByRole('heading', { name: listing.cropName }).first().click()
  await buyerPage.getByRole('button', { name: /Request Wholesale Order/ }).click()
  await buyerPage.locator('input[type="number"]').fill(String(QUANTITY))
  await buyerPage.getByRole('button', { name: /Confirm & Submit Order/ }).click()
  await expect(buyerPage.getByText(new RegExp(`Order placed .* ${QUANTITY} .* reserved`))).toBeVisible()

  // XP-W-02: the UI said "reserved"; the API/DB must agree.
  const orders = await (await request.get(`${API}/api/orders?size=5`, { headers: bearer(buyerToken) })).json()
  const order = orders.items[0]
  expect(order.listingId).toBe(listing.id)
  expect(order.status).toBe('Pending')
  const after = await (await request.get(`${API}/api/listings/${listing.id}`, { headers: bearer(buyerToken) })).json()
  expect(after.availableQuantity).toBe(stockBefore - QUANTITY)

  // --- Officer, in the browser: approve -> the logistics agent proposes a slot -> confirm --------
  const officerPage = await (await browser.newContext()).newPage()
  await signedIn(officerPage, ACCOUNTS.officer)
  await officerPage.goto(`/orders/${order.id}`)
  await officerPage.getByRole('button', { name: 'Approve Order' }).click()
  await expect(officerPage.getByText(/pickup slot was proposed automatically/)).toBeVisible()

  // The slot is only a proposal until a human confirms it (FR10 / AI approval rule).
  const proposed = await (await request.get(`${API}/api/orders/${order.id}/schedule`, { headers: bearer(officerToken) })).json()
  expect(proposed.status).toBe('Proposed')
  await officerPage.getByRole('button', { name: 'Approve', exact: true }).click()
  await expect(officerPage.getByRole('button', { name: 'Mark Completed' })).toBeVisible()

  const scheduled = await (await request.get(`${API}/api/orders/${order.id}`, { headers: bearer(officerToken) })).json()
  expect(scheduled.status).toBe('Scheduled')

  await officerPage.getByRole('button', { name: 'Mark Completed' }).click()
  await expect(officerPage.getByRole('button', { name: 'Mark Completed' })).toBeHidden()

  // --- Back to the buyer: the same order is Completed and the buyer was notified ---------------
  const final = await (await request.get(`${API}/api/orders/${order.id}`, { headers: bearer(buyerToken) })).json()
  expect(final.status).toBe('Completed')
  const notes = await (await request.get(`${API}/api/notifications`, { headers: bearer(buyerToken) })).json()
  const items = Array.isArray(notes) ? notes : notes.items
  expect(items.length).toBeGreaterThan(0)

  await buyerPage.goto('/my-orders')
  await expect(buyerPage.getByText('Completed').first()).toBeVisible()
})

test('XP-W-05b a buyer cannot open the officer order page by URL', async ({ browser, request }) => {
  const buyerToken = await apiLogin(request, ACCOUNTS.buyer)
  const orders = await (await request.get(`${API}/api/orders?size=1`, { headers: bearer(buyerToken) })).json()
  const page = await (await browser.newContext()).newPage()
  await signedIn(page, ACCOUNTS.buyer)

  await page.goto(`/orders/${orders.items[0]?.id ?? '00000000-0000-0000-0000-000000000000'}`)

  await expect(page.getByText('Access denied')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Approve Order' })).toHaveCount(0)
})
