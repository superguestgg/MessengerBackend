import { useCallback, useEffect, useRef } from 'react'
import { api, unwrap } from '../api/client'

// Scrolling reports many seqs in a row: send the largest one at most this often.
const sendDelayMs = 1000

// Tells the server how far the user has read. The server keeps the larger value,
// so a lost or late request does no harm and is not retried.
export function useReadMarker(chatId: string, onMarked: () => void) {
  const sent = useRef(0)
  const wanted = useRef(0)
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const onMarkedRef = useRef(onMarked)

  useEffect(() => {
    onMarkedRef.current = onMarked
  }, [onMarked])

  const flush = useCallback(() => {
    timer.current = null
    const seq = wanted.current
    if (seq <= sent.current) return
    sent.current = seq
    unwrap(api.POST('/api/chats/{chatId}/read', { params: { path: { chatId } }, body: { seq } }))
      .then(() => onMarkedRef.current())
      .catch(() => {})
  }, [chatId])

  // Leaving the chat sends what is still waiting.
  useEffect(
    () => () => {
      if (timer.current != null) {
        clearTimeout(timer.current)
        flush()
      }
    },
    [flush],
  )

  return useCallback(
    (seq: number) => {
      if (seq <= wanted.current) return
      wanted.current = seq
      timer.current ??= setTimeout(flush, sendDelayMs)
    },
    [flush],
  )
}
