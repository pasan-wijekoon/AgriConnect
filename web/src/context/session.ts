import { useSyncExternalStore } from 'react'

export type StaffRole = 'Officer' | 'Administrator'
export const DEV_ROLE_SWITCH = false

const listeners = new Set<() => void>()

function readRole(): StaffRole {
  try {
    const raw = localStorage.getItem('agriconnect_user')
    const role = raw ? JSON.parse(raw).role : null
    return role === 'Administrator' ? 'Administrator' : 'Officer'
  } catch {
    return 'Officer'
  }
}

export function useRole(): StaffRole {
  return useSyncExternalStore(
    (notify) => {
      listeners.add(notify)
      return () => listeners.delete(notify)
    },
    () => readRole(),
  )
}

export function authHeaders(): Record<string, string> {
  try {
    const raw = localStorage.getItem('agriconnect_user')
    const token = raw ? JSON.parse(raw).token : null
    return token ? { Authorization: `Bearer ${token}` } : {}
  } catch {
    return {}
  }
}
