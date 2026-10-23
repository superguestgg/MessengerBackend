import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router'
import { api, errorMessage, isAbort, unwrap, type Chat, type LiveLocation, type MessageLocation } from '../api/client'
import { useMe } from '../auth/context'
import { formatAgo, formatDistance, formatLeft, formatTime } from '../format'
import { broadcastKey, isLiveNow, startBroadcast, stopBroadcast, syncBroadcasts, useBroadcasts } from './liveLocation'
import { chatTitle } from './model'
import { useNow } from './useNow'

// Live locations count as stale after this long without a new point.
const staleMs = 3 * 60_000

// What the user is broadcasting is checked this often, so a stop from another device shows up.
const mineRefreshMs = 15_000

// No map library: the point opens on openstreetmap.org.
function mapUrl({ latitude, longitude }: MessageLocation) {
  return `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}#map=17/${latitude}/${longitude}`
}

interface CardProps {
  chatId: string
  seq: number
  location: MessageLocation
  mine: boolean
}

export function LocationCard({ chatId, seq, location, mine }: CardProps) {
  const now = useNow(10_000)
  const [stopping, setStopping] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const live = location.liveUntil != null
  const active = isLiveNow(location, now)
  const stale = active && now - Date.parse(location.updatedAt) > staleMs

  async function stop() {
    setStopping(true)
    setError(null)
    try {
      await stopBroadcast(chatId, seq)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setStopping(false)
    }
  }

  return (
    <div className={active ? 'location live' : 'location'}>
      <div className="location-title">
        <span aria-hidden="true">📍</span>
        {live ? (active ? 'Геопозиция в реальном времени' : 'Трансляция геопозиции завершена') : 'Геопозиция'}
      </div>
      <a href={mapUrl(location)} target="_blank" rel="noreferrer noopener">
        Открыть на карте
      </a>
      <span className="muted small">
        {location.latitude.toFixed(5)}, {location.longitude.toFixed(5)}
        {location.accuracyMeters != null && ` · ±${formatDistance(location.accuracyMeters)}`}
      </span>
      {live && (
        <span className={stale ? 'small error' : 'muted small'}>
          {active
            ? `Обновлено ${formatAgo(location.updatedAt, now)} · до ${formatTime(location.liveUntil!)}`
            : `Последняя точка в ${formatTime(location.updatedAt)}`}
        </span>
      )}
      {mine && active && (
        <button type="button" className="button small secondary" disabled={stopping} onClick={stop}>
          Остановить
        </button>
      )}
      {error != null && <span className="attachment-error">{error}</span>}
    </div>
  )
}

// What the account broadcasts in any chat, refreshed on a timer and when the tab comes back.
function useMyLiveLocations() {
  const [mine, setMine] = useState<LiveLocation[]>([])
  const [version, setVersion] = useState(0)
  const broadcasts = useBroadcasts()
  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  // Starting or stopping here changes the list too.
  useEffect(() => {
    const controller = new AbortController()
    const fetchedAt = Date.now()
    unwrap(api.GET('/api/live-locations/mine', { signal: controller.signal }))
      .then((result) => {
        setMine(result)
        syncBroadcasts(result, fetchedAt)
      })
      .catch(() => {
        // Kept as it was; the next refresh tries again.
      })
    return () => controller.abort()
  }, [version, broadcasts.size])

  useEffect(() => {
    const timer = setInterval(refresh, mineRefreshMs)
    const onVisible = () => document.visibilityState === 'visible' && refresh()
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      clearInterval(timer)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [refresh])

  return { mine, refresh }
}

// Titles of the chats the banner mentions; loaded only while there is something to show.
function useChatTitles(chatIds: string) {
  const me = useMe()
  const [chats, setChats] = useState<Chat[]>([])

  useEffect(() => {
    if (chatIds === '') return
    const controller = new AbortController()
    unwrap(api.GET('/api/chats', { signal: controller.signal }))
      .then(setChats)
      .catch((err: unknown) => {
        if (!isAbort(err)) setChats([])
      })
    return () => controller.abort()
  }, [chatIds])

  return (chatId: string) => {
    const chat = chats.find((item) => item.chatId === chatId)
    return chat != null ? chatTitle(chat, me.accountId) : 'чат'
  }
}

// On every page: what the user is sharing, from which tab, and a way to stop it.
export function LiveLocationBanner() {
  const { mine, refresh } = useMyLiveLocations()
  const broadcasts = useBroadcasts()
  const now = useNow(30_000)
  const titleOf = useChatTitles([...new Set(mine.map((live) => live.chatId))].sort().join(','))
  const [error, setError] = useState<string | null>(null)

  const shown = mine.filter((live) => isLiveNow(live.location, now))
  if (shown.length === 0) return null

  async function stop(live: LiveLocation) {
    setError(null)
    try {
      await stopBroadcast(live.chatId, live.seq)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      refresh()
    }
  }

  return (
    <div className="live-banner" role="status">
      {shown.map((live) => {
        const local = broadcasts.get(broadcastKey(live.chatId, live.seq))
        const status =
          local == null
            ? 'не отправляется из этой вкладки'
            : local.status === 'denied'
              ? 'нет доступа к геопозиции'
              : local.status === 'unavailable'
                ? 'не удаётся определить геопозицию'
                : 'отправляется из этой вкладки'
        return (
          <div key={broadcastKey(live.chatId, live.seq)} className="live-banner-item">
            <span aria-hidden="true">📍</span>
            <span className="grow">
              Вы транслируете геопозицию в{' '}
              <Link to={`/chats/${live.chatId}`}>«{titleOf(live.chatId)}»</Link>, осталось{' '}
              {formatLeft(live.location.liveUntil!, now)} <span className="muted">· {status}</span>
            </span>
            {(local == null || local.status === 'denied') && (
              <button
                type="button"
                className="button small secondary"
                onClick={() => startBroadcast(live.chatId, live.seq, live.location.liveUntil!)}
              >
                Продолжить
              </button>
            )}
            <button type="button" className="button small" onClick={() => void stop(live)}>
              Остановить
            </button>
          </div>
        )
      })}
      {error != null && <span className="error small">{error}</span>}
    </div>
  )
}
