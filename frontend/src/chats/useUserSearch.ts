import { useEffect, useState } from 'react'
import { api, errorMessage, isAbort, unwrap, type UserSearchResult } from '../api/client'
import { limits } from '../limits'

const debounceMs = 300

interface Found {
  text: string
  results: UserSearchResult[]
  error: string | null
}

// Searches by an account id, an exact email or the start of a word in a name, as the user types.
export function useUserSearch(query: string) {
  const text = query.trim()
  const active = text.length >= limits.searchMin
  const [found, setFound] = useState<Found | null>(null)

  useEffect(() => {
    if (!active) return
    const controller = new AbortController()
    const timer = setTimeout(() => {
      unwrap(api.GET('/api/users/search', { params: { query: { query: text } }, signal: controller.signal }))
        .then((results) => setFound({ text, results, error: null }))
        .catch((err: unknown) => {
          if (!isAbort(err)) setFound({ text, results: [], error: errorMessage(err) })
        })
    }, debounceMs)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [text, active])

  // An answer for an older query is not shown while the current one is in flight.
  const current = active && found?.text === text ? found : null
  return {
    active,
    loading: active && current == null,
    results: current?.results ?? [],
    error: current?.error ?? null,
  }
}
