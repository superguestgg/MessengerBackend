import { useSyncExternalStore } from 'react'
import { api, ApiError, unwrap, type LiveLocation, type MessageLocation } from '../api/client'
import { limits } from '../limits'

// The server drops points closer than 2 s apart; there is no use sending them faster than this.
const sendIntervalMs = 5000

// A person standing still gets no new positions: resend the last one so viewers see it is not stale.
const heartbeatMs = 60_000

// A broadcast that started this recently may be missing from a list fetched just before it.
const startGraceMs = 15_000

// Which broadcasts this tab sends. sessionStorage survives a reload of the tab but not a new tab
// or device: there the user is asked first, they may have forgotten they share their location.
const storageKey = 'messenger.liveLocations'

export type BroadcastStatus = 'sending' | 'denied' | 'unavailable'

export interface BroadcastState {
  chatId: string
  seq: number
  status: BroadcastStatus
}

interface Broadcast extends BroadcastState {
  startedAt: number
  watchId: number | null
  position: GeolocationPosition | null
  lastSentAt: number
  inFlight: boolean
  sendTimer: ReturnType<typeof setTimeout> | null
  heartbeat: ReturnType<typeof setInterval>
  expiry: ReturnType<typeof setTimeout>
}

const broadcasts = new Map<string, Broadcast>()
const listeners = new Set<() => void>()
let snapshot: ReadonlyMap<string, BroadcastState> = new Map()

// Remembered before a reload, waiting for the server to confirm they are still active.
let toResume = loadRemembered()

export function broadcastKey(chatId: string, seq: number) {
  return `${chatId}:${seq}`
}

function changed() {
  snapshot = new Map(
    [...broadcasts].map(([key, { chatId, seq, status }]) => [key, { chatId, seq, status }] as const),
  )
  for (const listener of listeners) listener()
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

// Broadcasts this tab is sending, by broadcastKey.
export function useBroadcasts(): ReadonlyMap<string, BroadcastState> {
  return useSyncExternalStore(subscribe, () => snapshot)
}

interface Remembered {
  chatId: string
  seq: number
}

function loadRemembered(): Remembered[] {
  try {
    const raw = sessionStorage.getItem(storageKey)
    return raw != null ? (JSON.parse(raw) as Remembered[]) : []
  } catch {
    return []
  }
}

function saveRemembered() {
  try {
    const list = [
      ...toResume,
      ...[...broadcasts.values()].map(({ chatId, seq }) => ({ chatId, seq })),
    ]
    if (list.length > 0) sessionStorage.setItem(storageKey, JSON.stringify(list))
    else sessionStorage.removeItem(storageKey)
  } catch {
    // Private mode: the broadcast just won't resume after a reload.
  }
}

export const geolocationSupported = typeof navigator !== 'undefined' && 'geolocation' in navigator

export function geolocationErrorMessage(error: GeolocationPositionError) {
  if (error.code === error.PERMISSION_DENIED) return 'Нет доступа к геопозиции: разрешите его в настройках браузера'
  if (error.code === error.TIMEOUT) return 'Не удалось определить геопозицию: время ожидания истекло'
  return 'Не удалось определить геопозицию'
}

export function currentPosition(): Promise<GeolocationPosition> {
  return new Promise((resolve, reject) =>
    navigator.geolocation.getCurrentPosition(resolve, reject, {
      enableHighAccuracy: true,
      timeout: 15_000,
      maximumAge: 30_000,
    }),
  )
}

export function toPoint(position: GeolocationPosition) {
  return {
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
    accuracyMeters: Math.min(position.coords.accuracy, limits.geoAccuracyMaxMeters),
  }
}

// Starts sending points for a live location message from this tab.
export function startBroadcast(chatId: string, seq: number, until: string, position?: GeolocationPosition) {
  const key = broadcastKey(chatId, seq)
  const left = Date.parse(until) - Date.now()
  if (left <= 0) return

  const existing = broadcasts.get(key)
  if (existing != null) {
    // "Continue" after access was denied: ask again.
    if (existing.status === 'denied') watch(existing)
    return
  }

  const broadcast: Broadcast = {
    chatId,
    seq,
    status: 'sending',
    startedAt: Date.now(),
    watchId: null,
    position: null,
    // The message itself carried the first point.
    lastSentAt: position != null ? Date.now() : 0,
    inFlight: false,
    sendTimer: null,
    heartbeat: setInterval(() => send(broadcast, true), heartbeatMs),
    expiry: setTimeout(() => end(broadcast), left),
  }
  broadcast.position = position ?? null
  broadcasts.set(key, broadcast)
  watch(broadcast)
  saveRemembered()
  changed()
}

function watch(broadcast: Broadcast) {
  if (broadcast.watchId != null) navigator.geolocation.clearWatch(broadcast.watchId)
  broadcast.watchId = navigator.geolocation.watchPosition(
    (position) => {
      broadcast.position = position
      setStatus(broadcast, 'sending')
      send(broadcast, false)
    },
    (error) => {
      if (error.code === error.PERMISSION_DENIED) {
        if (broadcast.watchId != null) navigator.geolocation.clearWatch(broadcast.watchId)
        broadcast.watchId = null
        setStatus(broadcast, 'denied')
      } else {
        setStatus(broadcast, 'unavailable')
      }
    },
    { enableHighAccuracy: true, maximumAge: 10_000 },
  )
}

function setStatus(broadcast: Broadcast, status: BroadcastStatus) {
  if (broadcast.status === status) return
  broadcast.status = status
  changed()
}

function send(broadcast: Broadcast, heartbeat: boolean) {
  if (broadcast.position == null || broadcast.inFlight || !broadcasts.has(broadcastKey(broadcast.chatId, broadcast.seq))) {
    return
  }
  const wait = broadcast.lastSentAt + sendIntervalMs - Date.now()
  if (wait > 0 && !heartbeat) {
    broadcast.sendTimer ??= setTimeout(() => {
      broadcast.sendTimer = null
      send(broadcast, false)
    }, wait)
    return
  }

  broadcast.inFlight = true
  broadcast.lastSentAt = Date.now()
  unwrap(
    api.PUT('/api/chats/{chatId}/messages/{seq}/location', {
      params: { path: { chatId: broadcast.chatId, seq: broadcast.seq } },
      body: toPoint(broadcast.position),
    }),
  )
    .catch((error: unknown) => {
      // Ended (maybe stopped from another device), not a live location, or no longer a member.
      if (error instanceof ApiError && [403, 404, 409].includes(error.status)) end(broadcast)
      // Network trouble: the next position or heartbeat tries again.
    })
    .finally(() => {
      broadcast.inFlight = false
    })
}

// Stops sending from this tab; the server-side broadcast is left as it is.
function end(broadcast: Broadcast) {
  const key = broadcastKey(broadcast.chatId, broadcast.seq)
  if (broadcasts.get(key) !== broadcast) return
  if (broadcast.watchId != null) navigator.geolocation.clearWatch(broadcast.watchId)
  if (broadcast.sendTimer != null) clearTimeout(broadcast.sendTimer)
  clearInterval(broadcast.heartbeat)
  clearTimeout(broadcast.expiry)
  broadcasts.delete(key)
  saveRemembered()
  changed()
}

// Stops the broadcast for every device: this tab right after the server, others on their next point.
// This tab stops even if the request fails: the user asked to stop sharing.
export async function stopBroadcast(chatId: string, seq: number) {
  try {
    await unwrap(api.POST('/api/chats/{chatId}/messages/{seq}/location/stop', { params: { path: { chatId, seq } } }))
  } finally {
    const broadcast = broadcasts.get(broadcastKey(chatId, seq))
    if (broadcast != null) end(broadcast)
  }
}

// Signing out stops sending from this device but not the broadcast: other devices still show and stop it.
export function stopAllLocal() {
  toResume = []
  for (const broadcast of [...broadcasts.values()]) end(broadcast)
  saveRemembered()
}

// Brings this tab in line with what the server says is being broadcast: resumes what this tab
// sent before a reload and drops what has ended elsewhere.
export function syncBroadcasts(active: LiveLocation[], fetchedAt: number) {
  const byKey = new Map(active.map((live) => [broadcastKey(live.chatId, live.seq), live]))

  for (const broadcast of [...broadcasts.values()]) {
    const key = broadcastKey(broadcast.chatId, broadcast.seq)
    if (!byKey.has(key) && broadcast.startedAt < fetchedAt - startGraceMs) end(broadcast)
  }

  const remembered = toResume
  toResume = []
  for (const { chatId, seq } of remembered) {
    const live = byKey.get(broadcastKey(chatId, seq))
    if (live?.location.liveUntil != null) startBroadcast(chatId, seq, live.location.liveUntil)
  }
  saveRemembered()
}

// Checked on the client clock too: the server's isLive was true only when the message was read.
export function isLiveNow(location: MessageLocation, now: number) {
  return location.isLive && location.liveUntil != null && Date.parse(location.liveUntil) > now
}

// Seconds of the live periods the user can pick, with labels.
export const livePeriods = limits.livePeriodsSeconds.map((seconds) => ({
  seconds,
  label: seconds < 3600 ? `${seconds / 60} мин` : `${seconds / 3600} ч`,
}))
