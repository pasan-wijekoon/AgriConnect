import { useSyncExternalStore } from 'react'

export type StaffRole = 'Officer' | 'Administrator'

/** The role switcher only exists in development, where the API accepts X-Dev-Role. */
export const DEV_ROLE_SWITCH = import.meta.env.DEV

const STORAGE_KEY = 'agriconnect.devRole'
const listeners = new Set<() => void>()

function readStoredRole(): StaffRole {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'Administrator' ? 'Administrator' : 'Officer'
  } catch {
    return 'Officer'
  }
}

let role: StaffRole = readStoredRole()

export function setRole(next: StaffRole) {
  role = next
  try {
    localStorage.setItem(STORAGE_KEY, next)
  } catch {
    // Storage unavailable (private mode); the choice just won't survive a reload.
  }
  listeners.forEach((notify) => notify())
}

export function useRole(): StaffRole {
  return useSyncExternalStore(
    (notify) => {
      listeners.add(notify)
      return () => listeners.delete(notify)
    },
    () => role,
  )
}

// TODO: send the signed-in user's JWT once the shared login flow exists.
export function authHeaders(): Record<string, string> {
  return DEV_ROLE_SWITCH ? { 'X-Dev-Role': role } : {}
}
