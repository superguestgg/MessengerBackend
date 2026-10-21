const time = new Intl.DateTimeFormat('ru-RU', { hour: '2-digit', minute: '2-digit' })
const date = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'short' })
const dateTime = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'medium', timeStyle: 'short' })

function isToday(value: Date) {
  const now = new Date()
  return value.toDateString() === now.toDateString()
}

export function formatTime(iso: string) {
  return time.format(new Date(iso))
}

// Today's activity shows the time, older — the date.
export function formatShort(iso: string) {
  const value = new Date(iso)
  return isToday(value) ? time.format(value) : date.format(value)
}

export function formatDateTime(iso: string) {
  return dateTime.format(new Date(iso))
}

export function shortId(id: string) {
  return id.slice(0, 8)
}

export function displayName(name: string | null | undefined, id: string) {
  return name != null && name !== '' ? name : `Без имени · ${shortId(id)}`
}

// formatDuration(75) → "1:15"
export function formatDuration(seconds: number) {
  const whole = Math.max(0, Math.round(seconds))
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`
}

const decimal = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 1 })

export function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} Б`
  if (bytes < 1024 * 1024) return `${decimal.format(bytes / 1024)} КБ`
  return `${decimal.format(bytes / (1024 * 1024))} МБ`
}

const plurals = new Intl.PluralRules('ru-RU')

// pluralize(3, ['участник', 'участника', 'участников']) → "3 участника"
export function pluralize(count: number, [one, few, many]: [string, string, string]) {
  const form = plurals.select(count)
  return `${count} ${form === 'one' ? one : form === 'few' ? few : many}`
}

// How long ago, roughly: "только что", "5 мин назад", "2 ч назад".
export function formatAgo(iso: string, now: number) {
  const seconds = Math.max(0, Math.round((now - Date.parse(iso)) / 1000))
  if (seconds < 30) return 'только что'
  if (seconds < 60) return `${seconds} с назад`
  if (seconds < 3600) return `${Math.floor(seconds / 60)} мин назад`
  return `${Math.floor(seconds / 3600)} ч назад`
}

// Time left: "40 мин", "1 ч 20 мин".
export function formatLeft(iso: string, now: number) {
  const minutes = Math.max(1, Math.ceil((Date.parse(iso) - now) / 60_000))
  if (minutes < 60) return `${minutes} мин`
  const hours = Math.floor(minutes / 60)
  return minutes % 60 === 0 ? `${hours} ч` : `${hours} ч ${minutes % 60} мин`
}

// formatDistance(12.4) → "12 м", formatDistance(2500) → "2,5 км"
export function formatDistance(meters: number) {
  return meters < 1000 ? `${Math.round(meters)} м` : `${decimal.format(meters / 1000)} км`
}
