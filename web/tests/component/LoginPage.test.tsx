import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const { login, register } = vi.hoisted(() => ({ login: vi.fn(), register: vi.fn() }))
vi.mock('../../src/context/AuthContext', () => ({ useAuth: () => ({ login, register }) }))

import { LoginPage } from '../../src/pages/LoginPage'

beforeEach(() => {
  login.mockReset()
  register.mockReset()
})

const emailInput = () => screen.getByPlaceholderText('name@agriconnect.lk') as HTMLInputElement
const passwordInput = () => document.querySelector('input[type="password"]') as HTMLInputElement
// "Sign In" is both the tab and the submit button; the submit one is type=submit.
const submit = () =>
  screen.getAllByRole('button', { name: 'Sign In' }).find((b) => (b as HTMLButtonElement).type === 'submit')!

describe('Login form (WEB-C-01, WEB-C-02)', () => {
  it('WEB-C-01 blocks an empty submit and marks both fields as missing', async () => {
    render(<LoginPage />)

    await userEvent.click(submit())

    expect(login).not.toHaveBeenCalled()
    expect(emailInput().validity.valueMissing).toBe(true)
    expect(passwordInput().validity.valueMissing).toBe(true)
  })

  it('WEB-C-02 rejects an email without @ and does not call the API', async () => {
    render(<LoginPage />)

    await userEvent.type(emailInput(), 'abc')
    await userEvent.type(passwordInput(), 'password')
    await userEvent.click(submit())

    expect(emailInput().validity.typeMismatch).toBe(true)
    expect(login).not.toHaveBeenCalled()
  })

  it('WEB-C-02b submits valid credentials to login()', async () => {
    login.mockResolvedValue({})
    render(<LoginPage />)

    await userEvent.type(emailInput(), 'farmer@agriconnect.lk')
    await userEvent.type(passwordInput(), 'password')
    await userEvent.click(submit())

    expect(login).toHaveBeenCalledExactlyOnceWith('farmer@agriconnect.lk', 'password')
  })

  it('WEB-C-02c shows the server message when login fails (error state)', async () => {
    login.mockRejectedValue(new Error('Invalid email or password'))
    render(<LoginPage />)

    await userEvent.type(emailInput(), 'farmer@agriconnect.lk')
    await userEvent.type(passwordInput(), 'wrong-password')
    await userEvent.click(submit())

    expect(await screen.findByText('Invalid email or password')).toBeInTheDocument()
  })
})

describe('Register form validation (WEB-C-01b)', () => {
  async function fillRegister({ phone, password }: { phone: string; password: string }) {
    render(<LoginPage />)
    await userEvent.click(screen.getByRole('button', { name: 'Register' }))
    await userEvent.type(screen.getByPlaceholderText('Kamal Perera'), 'Test Farmer')
    await userEvent.type(screen.getByPlaceholderText('kamal@domain.com'), 'test@agriconnect.lk')
    await userEvent.type(screen.getByPlaceholderText('07X-XXX XXXX'), phone)
    await userEvent.type(screen.getByPlaceholderText('At least 6 characters'), password)
    await userEvent.click(screen.getByRole('button', { name: 'Register Account' }))
  }

  it('rejects a phone number that does not start with 07', async () => {
    await fillRegister({ phone: '0112345678', password: 'secret1' })

    expect(await screen.findByText(/Phone must be 10 digits starting with 07/)).toBeInTheDocument()
    expect(register).not.toHaveBeenCalled()
  })

  it('rejects a password shorter than 6 characters (boundary: 5 fails)', async () => {
    await fillRegister({ phone: '0771234567', password: '12345' })

    expect(await screen.findByText('Password must be at least 6 characters')).toBeInTheDocument()
    expect(register).not.toHaveBeenCalled()
  })

  it('accepts a 6-character password and a valid phone (boundary: 6 passes)', async () => {
    register.mockResolvedValue({})
    await fillRegister({ phone: '0771234567', password: '123456' })

    expect(register).toHaveBeenCalledOnce()
    expect(register.mock.calls[0][0]).toMatchObject({ role: 'Farmer', phone: '0771234567', password: '123456' })
  })
})

describe('Accessibility of the login form (A11Y-05)', () => {
  it('A11Y-05 every input has a programmatically associated label', () => {
    render(<LoginPage />)

    // getByLabelText only succeeds when <label for> / id (or nesting) links the label to the input.
    expect(screen.getByLabelText('Email Address')).toBeInTheDocument()
    expect(screen.getByLabelText('Password')).toBeInTheDocument()
  })
})
