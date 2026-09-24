import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'

/**
 * Client-side counterpart to the backend's dev-auth seam
 * (backend/src/config/DevAuthenticationHandler.cs, plan §6). No shared
 * User/Auth/JWT implementation exists anywhere in the repo yet, so every
 * request carries X-Dev-Role/X-Dev-UserId headers built from whatever is
 * picked here. Swapping to real auth later only means changing this file and
 * how ordersApi.ts builds its headers — page components never read these
 * headers directly.
 */

export type DevRole = 'Buyer' | 'Farmer' | 'Officer' | 'Admin'

export interface DevIdentity {
  role: DevRole
  userId: string
}

const STORAGE_KEY = 'agriconnect.devIdentity'

const DEFAULT_IDENTITY: DevIdentity = {
  role: 'Officer',
  userId: '11111111-1111-1111-1111-111111111111',
}

function loadStoredIdentity(): DevIdentity {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return DEFAULT_IDENTITY
    const parsed = JSON.parse(raw) as Partial<DevIdentity>
    if (!parsed.role || !parsed.userId) return DEFAULT_IDENTITY
    return { role: parsed.role, userId: parsed.userId }
  } catch {
    return DEFAULT_IDENTITY
  }
}

interface AuthContextValue {
  identity: DevIdentity
  setIdentity: (identity: DevIdentity) => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [identity, setIdentityState] = useState<DevIdentity>(loadStoredIdentity)

  const setIdentity = (next: DevIdentity) => {
    setIdentityState(next)
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    } catch {
      // Private browsing / storage disabled — identity still works for this
      // session via React state, it just won't persist across reloads.
    }
  }

  const value = useMemo(() => ({ identity, setIdentity }), [identity])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return ctx
}
