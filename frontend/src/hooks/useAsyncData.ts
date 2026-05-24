/* eslint-disable react-hooks/exhaustive-deps */
import { useEffect, useState } from 'react'
import type { DependencyList } from 'react'

export function useAsyncData<T>(loader: () => Promise<T>, deps: DependencyList) {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true

    const timer = window.setTimeout(() => {
      void (async () => {
        setLoading(true)
        setError(null)

        try {
          const result = await loader()
          if (active) {
            setData(result)
          }
        } catch (caughtError) {
          if (active) {
            const message = caughtError instanceof Error ? caughtError.message : 'Something went wrong.'
            setError(message)
          }
        } finally {
          if (active) {
            setLoading(false)
          }
        }
      })()
    }, 0)

    return () => {
      active = false
      window.clearTimeout(timer)
    }
  }, [...deps, reloadKey])

  return {
    data,
    loading,
    error,
    reload: () => {
      setLoading(true)
      setReloadKey((value) => value + 1)
    },
    setData,
  }
}

