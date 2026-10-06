/**
 * The signed-in user's session, as saved by AuthContext at login. Every API
 * client (marketplace, orders, analytics, quality) reads the bearer token from
 * here, so there is exactly one login and one identity — the old X-Dev-* header
 * bypass is no longer used by the web app.
 */
export const SESSION_KEY = 'agriconnect_user'

export interface StoredSession {
  id: string
  fullName: string
  email: string
  role: string
  token: string
  collectionCentreId?: string | null
}

export function getStoredSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY)
    return raw ? (JSON.parse(raw) as StoredSession) : null
  } catch {
    return null
  }
}

export function getAuthToken(): string | null {
  return getStoredSession()?.token ?? null
}

/** A 401 means the saved token is expired/invalid: drop it and return to the landing page. */
export function handleUnauthorized(): void {
  try {
    localStorage.removeItem(SESSION_KEY)
  } catch {
    // Storage unavailable — the reload below still resets in-memory state.
  }
  if (window.location.pathname !== '/') {
    window.location.assign('/')
  } else {
    window.location.reload()
  }
}
