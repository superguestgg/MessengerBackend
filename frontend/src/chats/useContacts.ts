import { useEffect, useMemo, useState } from 'react'
import { api, isAbort, unwrap, type Bot } from '../api/client'
import { useMe } from '../auth/context'
import { collectContacts, useChats } from './model'

export function useContacts() {
  const me = useMe()
  const { chats } = useChats()
  const [bots, setBots] = useState<Bot[]>([])

  useEffect(() => {
    const controller = new AbortController()
    unwrap(api.GET('/api/bots', { signal: controller.signal }))
      .then(setBots)
      .catch((err: unknown) => {
        // Contacts are only suggestions: without bots the user can still paste an id.
        if (!isAbort(err)) setBots([])
      })
    return () => controller.abort()
  }, [])

  return useMemo(() => collectContacts(chats ?? [], bots, me.accountId), [chats, bots, me.accountId])
}
