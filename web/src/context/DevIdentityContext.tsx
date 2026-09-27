import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'

/**
 * Client-side counterpart to the backend's dev-auth fallback
 * (backend/src/config/FakeClaimsPrincipal.cs). Real login now exists
 * (Component A — see context/AuthContext.tsx, the real `useAuth`/`AuthProvider`)
 * and is what the marketplace pages use; this dev-role picker still drives the
 * Order/Scheduling pages via X-Dev-Role/X-Dev-UserId headers, since rewiring
 * them onto the real bearer token is a separate follow-up (see PROGRESS.md),
 * not something this integration pass did. Renamed from AuthContext during
 * that integration once the name "Auth" became Component A's real one.
 */

export type DevRole = 'Buyer' | 'Farmer' | 'Officer' | 'Administrator'

export interface DevIdentity {
  role: DevRole
  userId: string
}

const STORAGE_KEY = 'agriconnect.devIdentity'

// The real seeded "Officer Demo" user (officer@agriconnect.lk) — not a
// placeholder GUID. Component C's InspectionsController/InspectionService
// looks up the acting officer as a real User row (FR13's audit-trail
// requirement), so the default identity must resolve to a real seeded user,
// unlike Order/Schedule's actorId which is only ever recorded, never looked up.
const DEFAULT_IDENTITY: DevIdentity = {
  role: 'Officer',
  userId: 'f0000000-0000-0000-0000-000000000050',
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

interface DevIdentityContextValue {
  identity: DevIdentity
  setIdentity: (identity: DevIdentity) => void
}

const DevIdentityContext = createContext<DevIdentityContextValue | undefined>(undefined)

export function DevIdentityProvider({ children }: { children: ReactNode }) {
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

  return <DevIdentityContext.Provider value={value}>{children}</DevIdentityContext.Provider>
}

export function useDevIdentity(): DevIdentityContextValue {
  const ctx = useContext(DevIdentityContext)
  if (!ctx) {
    throw new Error('useDevIdentity must be used within an DevIdentityProvider')
  }
  return ctx
}
