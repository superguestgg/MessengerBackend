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

const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function isUuid(value: string) {
  return uuidPattern.test(value.trim())
}

const plurals = new Intl.PluralRules('ru-RU')

// pluralize(3, ['участник', 'участника', 'участников']) → "3 участника"
export function pluralize(count: number, [one, few, many]: [string, string, string]) {
  const form = plurals.select(count)
  return `${count} ${form === 'one' ? one : form === 'few' ? few : many}`
}
