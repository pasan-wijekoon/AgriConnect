import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const { getRegions, getListings, getMe } = vi.hoisted(() => ({ getRegions: vi.fn(), getListings: vi.fn(), getMe: vi.fn() }))
vi.mock('../../src/utils/marketApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../src/utils/marketApi')>()),
  api: { getRegions, getListings, getMe },
}))

import App from '../../src/App'
import { BuyerDashboard } from '../../src/pages/BuyerDashboard'
import { ProductDetailModal } from '../../src/components/ProductDetailModal'
import { AuthProvider } from '../../src/context/AuthContext'

const listing = {
  id: 'l1', farmerId: 'f1', cropName: 'Tomatoes', cropCategory: 'Vegetables', regionName: 'Nuwara Eliya',
  quantity: 5, unit: 'kg', claimedGrade: 'A', pickupWindowStart: '2026-10-05T00:00:00Z',
  pickupWindowEnd: '2026-10-09T00:00:00Z', status: 'Published' as const, minPrice: 200, description: 'Fresh',
  createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', photos: [],
}

beforeEach(() => {
  // AuthContext re-validates a saved session through api.getMe(); echo the saved user back.
  getMe.mockReset().mockImplementation(async () => JSON.parse(localStorage.getItem('agriconnect_user') ?? '{}'))
  getRegions.mockReset().mockResolvedValue([])
  getListings.mockReset().mockResolvedValue({ items: [listing], totalCount: 1, page: 1, pageSize: 50 })
})

function signedInAs(role: string) {
  localStorage.setItem('agriconnect_user', JSON.stringify({ id: 'u1', fullName: 'Test User', email: 't@agriconnect.lk', role, token: 'x' }))
}

function renderApp(path = '/') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('Protected routes (WEB-C-05, WEB-C-06)', () => {
  it('WEB-C-05 an anonymous visitor to / sees the login form, not a dashboard', async () => {
    renderApp('/')

    expect(await screen.findByPlaceholderText('name@agriconnect.lk')).toBeInTheDocument()
    expect(screen.queryByText(/Showing/)).not.toBeInTheDocument()
    expect(getListings).not.toHaveBeenCalled()
  })

  it('WEB-C-06 a buyer gets the buyer dashboard only', async () => {
    signedInAs('Buyer')
    renderApp('/')

    expect(await screen.findByText(/available wholesale batches/)).toBeInTheDocument()
    expect(screen.queryByText('Inspection Queue')).not.toBeInTheDocument() // back-office nav is officer/admin only
  })

  it('WEB-C-06b an officer gets the back-office navigation, not a marketplace dashboard', async () => {
    signedInAs('Officer')
    renderApp('/')

    expect(await screen.findByText('Inspection Queue')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Schedule/ })).toHaveAttribute('href', '/orders/schedule')
    expect(getListings).not.toHaveBeenCalled()
  })

  it('WEB-C-06c the API role "Administrator" is mapped so an admin does not land on an empty page', async () => {
    signedInAs('Administrator')
    renderApp('/')

    await waitFor(() => expect(screen.queryByText('Loading AgriConnect...')).not.toBeInTheDocument())
    expect(screen.queryByPlaceholderText('name@agriconnect.lk')).not.toBeInTheDocument()
    expect(document.body.textContent).not.toBe('')
  })
})

describe('Buyer listing states (WEB-C-07, WEB-C-08, WEB-C-09)', () => {
  it('WEB-C-07 shows a loading message, then the listings', async () => {
    let release!: (v: unknown) => void
    getListings.mockReturnValue(new Promise((resolve) => { release = resolve }))
    render(<AuthProvider><BuyerDashboard /></AuthProvider>)

    expect(screen.getByText(/Loading published produce/)).toBeInTheDocument()
    release({ items: [listing], totalCount: 1, page: 1, pageSize: 50 })

    expect(await screen.findByText('Tomatoes')).toBeInTheDocument()
    expect(screen.queryByText(/Loading published produce/)).not.toBeInTheDocument()
  })

  it('WEB-C-08 shows a friendly empty state when nothing is published', async () => {
    getListings.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 })
    render(<AuthProvider><BuyerDashboard /></AuthProvider>)

    expect(await screen.findByText('No produce matches your filters')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Reset All Filters' })).toBeInTheDocument()
  })

  it('WEB-C-09 tells the buyer when the API fails, instead of pretending nothing matched', async () => {
    getListings.mockRejectedValue(new Error('API error 500'))
    vi.spyOn(console, 'error').mockImplementation(() => {})
    render(<AuthProvider><BuyerDashboard /></AuthProvider>)

    expect(await screen.findByRole('alert')).toHaveTextContent(/couldn.t load produce/i)
    expect(screen.queryByText('No produce matches your filters')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /try again|retry/i })).toBeInTheDocument()
  })
})

describe('Order form (WEB-C-10, WEB-C-11)', () => {
  async function openOrderForm() {
    signedInAs('Buyer')
    render(
      <AuthProvider>
        <ProductDetailModal listing={listing} onClose={() => {}} />
      </AuthProvider>,
    )
    await userEvent.click(await screen.findByRole('button', { name: /Request Wholesale Order/ }))
    // The form pre-fills the quantity with the available stock (capped at 100): 5 for this listing.
    return screen.getByDisplayValue('5') as HTMLInputElement
  }

  it('WEB-C-10 quantity boundaries against stock of 5: 0 and 6 rejected, 1 and 5 accepted', async () => {
    const quantity = await openOrderForm()

    for (const [value, valid] of [['0', false], ['1', true], ['5', true], ['6', false]] as const) {
      await userEvent.clear(quantity)
      await userEvent.type(quantity, value)
      expect(quantity.validity.valid, `quantity ${value}`).toBe(valid)
    }
  })

  it('WEB-C-11 confirming an order actually sends it to the orders API', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('{}', { status: 201 }))
    const quantity = await openOrderForm()

    await userEvent.clear(quantity)
    await userEvent.type(quantity, '3')
    await userEvent.click(screen.getByRole('button', { name: /Confirm & Submit Order/ }))

    await waitFor(() => {
      const call = fetchSpy.mock.calls.find(([url]) => String(url).includes('/orders'))
      expect(call, 'no request to /orders was made').toBeDefined()
    })
  })
})
