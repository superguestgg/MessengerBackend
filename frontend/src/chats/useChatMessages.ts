import { useCallback, useEffect, useState } from 'react'
import { api, ApiError, errorMessage, isAbort, unwrap, type Message } from '../api/client'
import { limits } from '../limits'
import { mergeMessages, resumeAfter } from './model'

// Below the server's 50 s limit and the usual 60 s proxy timeout.
const waitSeconds = 30

function sleep(ms: number, signal: AbortSignal) {
  return new Promise<void>((resolve) => {
    const timer = setTimeout(resolve, ms)
    signal.addEventListener(
      'abort',
      () => {
        clearTimeout(timer)
        resolve()
      },
      { once: true },
    )
  })
}

function hasMoreBefore(page: Message[]) {
  return page.length === limits.pageSize && page[0].seq > 1
}

// History plus long polling for new messages. Mount once per chat (key by chatId).
export function useChatMessages(chatId: string) {
  const [messages, setMessages] = useState<Message[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reconnecting, setReconnecting] = useState(false)
  const [hasOlder, setHasOlder] = useState(false)
  const [loadingOlder, setLoadingOlder] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller
    const path = { chatId }

    async function run() {
      let after: number
      try {
        const page = await unwrap(
          api.GET('/api/chats/{chatId}/messages', { params: { path, query: { limit: limits.pageSize } }, signal }),
        )
        setMessages((current) => mergeMessages(current, page))
        setHasOlder(hasMoreBefore(page))
        after = resumeAfter(page)
      } catch (err) {
        if (!isAbort(err)) setError(errorMessage(err))
        return
      } finally {
        if (!signal.aborted) setLoading(false)
      }

      let failures = 0
      while (!signal.aborted) {
        try {
          const result = await unwrap(
            api.GET('/api/chats/{chatId}/messages/wait', {
              params: { path, query: { after, timeout: waitSeconds } },
              signal,
            }),
          )
          failures = 0
          setReconnecting(false)
          setMessages((current) => mergeMessages(current, result.messages))
          after = result.nextAfterSeq
        } catch (err) {
          if (isAbort(err)) return
          // 401 signs the user out; 403/404 — the chat is gone or the user is no longer a member.
          if (err instanceof ApiError && [401, 403, 404].includes(err.status)) {
            if (err.status !== 401) setError('Чат недоступен')
            return
          }
          failures++
          setReconnecting(true)
          await sleep(Math.min(1000 * 2 ** failures, 15000), signal)
        }
      }
    }

    void run()
    return () => controller.abort()
  }, [chatId])

  const firstSeq = messages.length > 0 ? messages[0].seq : null

  const loadOlder = useCallback(async () => {
    if (firstSeq == null) return
    setLoadingOlder(true)
    try {
      const page = await unwrap(
        api.GET('/api/chats/{chatId}/messages', {
          params: { path: { chatId }, query: { before: firstSeq, limit: limits.pageSize } },
        }),
      )
      setMessages((current) => mergeMessages(current, page))
      setHasOlder(hasMoreBefore(page))
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoadingOlder(false)
    }
  }, [chatId, firstSeq])

  // The sender sees the message at once; the copy from long polling replaces it by seq.
  const addMessage = useCallback((message: Message) => {
    setMessages((current) => mergeMessages(current, [message]))
  }, [])

  return { messages, loading, error, reconnecting, hasOlder, loadingOlder, loadOlder, addMessage }
}
