import { useEffect, useEffectEvent, useState } from 'react'

export interface AsyncState<T> {
  data: T | undefined
  error: Error | undefined
  loading: boolean
  reload: () => void
}

/**
 * Loads data whenever `key` changes. While a new load runs, the previous data stays, so a
 * chart dims instead of flashing empty. Responses for an outdated key are dropped.
 */
export function useAsync<T>(load: () => Promise<T>, key: string): AsyncState<T> {
  const [attempt, setAttempt] = useState(0)
  const requestKey = `${key}#${attempt}`
  const [result, setResult] = useState<{ key?: string; data?: T; error?: Error }>({})
  const start = useEffectEvent(() => load())

  useEffect(() => {
    let current = true
    start().then(
      (data) => current && setResult({ key: requestKey, data }),
      (error: Error) => current && setResult((r) => ({ key: requestKey, data: r.data, error })),
    )
    return () => {
      current = false
    }
  }, [requestKey])

  const settled = result.key === requestKey
  return {
    data: result.data,
    error: settled ? result.error : undefined,
    loading: !settled,
    reload: () => setAttempt((n) => n + 1),
  }
}
