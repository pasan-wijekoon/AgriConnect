import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../src/utils/marketApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../src/utils/marketApi')>()),
  api: { getQuickPriceEstimate: vi.fn().mockResolvedValue(null) },
}))

import { AddEditListingModal } from '../../src/components/AddEditListingModal'

const crops = [{ id: 'crop-1', name: 'Tomatoes', category: 'Vegetables' }]
const regions = [{ id: 'region-1', name: 'Nuwara Eliya' }]
const onSubmit = vi.fn()
const onClose = vi.fn()

beforeEach(() => {
  onSubmit.mockReset().mockResolvedValue(undefined)
  onClose.mockReset()
})

function renderModal() {
  const view = render(<AddEditListingModal isOpen onClose={onClose} onSubmit={onSubmit} crops={crops} regions={regions} />)
  const q = (selector: string) => view.container.querySelector(selector) as HTMLInputElement
  return {
    quantity: () => q('input[type="number"][min="0.01"]'),
    floorPrice: () => q('input[type="number"][min="1"]'),
    dates: () => Array.from(view.container.querySelectorAll('input[type="date"]')) as HTMLInputElement[],
    publish: () => screen.getByRole('button', { name: 'Publish Listing' }),
  }
}

async function setNumber(input: HTMLInputElement, value: string) {
  await userEvent.clear(input)
  await userEvent.type(input, value)
}

describe('Create-listing form (WEB-C-03, WEB-C-04)', () => {
  it('WEB-C-03 rejects a negative floor price (rangeUnderflow) and does not submit', async () => {
    const form = renderModal()

    await setNumber(form.floorPrice(), '-5')
    await userEvent.click(form.publish())

    expect(form.floorPrice().validity.rangeUnderflow).toBe(true)
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('WEB-C-03b floor price boundary: 0 is rejected, 1 is accepted', async () => {
    const form = renderModal()

    await setNumber(form.floorPrice(), '0.5')
    expect(form.floorPrice().validity.rangeUnderflow).toBe(true)
    await setNumber(form.floorPrice(), '1')
    expect(form.floorPrice().validity.valid).toBe(true)
  })

  it('WEB-C-03c quantity must be above zero (boundary: 0 rejected, 0.01 accepted)', async () => {
    const form = renderModal()

    await setNumber(form.quantity(), '0')
    expect(form.quantity().validity.rangeUnderflow).toBe(true)
    await setNumber(form.quantity(), '0.01')
    expect(form.quantity().validity.valid).toBe(true)
  })

  it('WEB-C-04 submits the entered values with an ISO pickup window', async () => {
    const form = renderModal()

    await setNumber(form.quantity(), '250')
    await setNumber(form.floorPrice(), '300')
    await userEvent.click(form.publish())

    expect(onSubmit).toHaveBeenCalledOnce()
    const payload = onSubmit.mock.calls[0][0]
    expect(payload).toMatchObject({ cropId: 'crop-1', regionId: 'region-1', quantity: 250, minPrice: 300, unit: 'kg' })
    expect(payload.pickupWindowStart).toMatch(/^\d{4}-\d{2}-\d{2}T/)
    expect(onClose).toHaveBeenCalled()
  })

  it('WEB-C-04b refuses to submit when the last photo is removed', async () => {
    const form = renderModal()

    await userEvent.click(screen.getByTitle('Remove photo'))
    await userEvent.click(form.publish())

    expect(await screen.findByText('Please provide at least one photo of the produce.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('WEB-C-04c refuses a pickup window that ends on or before it starts (boundary: equal dates)', async () => {
    const form = renderModal()
    const [start, end] = form.dates()

    await userEvent.clear(end)
    await userEvent.type(end, start.value)
    await userEvent.click(form.publish())

    expect(await screen.findByText('Pickup window end date must be after start date.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('WEB-C-04d shows the server message when saving fails (error state)', async () => {
    onSubmit.mockRejectedValue(new Error('API error 500: boom'))
    const form = renderModal()

    await userEvent.click(form.publish())

    expect(await screen.findByText('API error 500: boom')).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
  })
})
