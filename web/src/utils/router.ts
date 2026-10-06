import { useSyncExternalStore } from 'react'

/** Hash routes (#/anomalies) — enough for four pages and works on any static host. */
export const ROUTES = ['price-trends', 'shortages', 'anomalies', 'reports'] as const
export type Route = (typeof ROUTES)[number]

const DEFAULT_ROUTE: Route = 'price-trends'

function currentRoute(): Route {
  const name = window.location.hash.replace(/^#\/?/, '')
  return (ROUTES as readonly string[]).includes(name) ? (name as Route) : DEFAULT_ROUTE
}

export function useRoute(): Route {
  return useSyncExternalStore(
    (notify) => {
      window.addEventListener('hashchange', notify)
      return () => window.removeEventListener('hashchange', notify)
    },
    currentRoute,
  )
}

export const routeHref = (route: Route) => `#/${route}`
