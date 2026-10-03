import { useState, type KeyboardEvent } from 'react'
import { BotBadge, ErrorText } from '../components/common'
import { displayName } from '../format'
import { limits } from '../limits'
import type { Contact } from './model'
import { useUserSearch } from './useUserSearch'

interface UserSearchProps {
  label: string
  // Shown until the user types a query.
  suggestions?: Contact[]
  exclude?: string[]
  disabled?: boolean
  onPick: (contact: Contact) => void
}

export function UserSearch({ label, suggestions = [], exclude = [], disabled = false, onPick }: UserSearchProps) {
  const [query, setQuery] = useState('')
  const search = useUserSearch(query)

  const found: Contact[] = search.results.map((result) => ({
    userId: result.userId,
    name: result.displayName ?? null,
    isBot: result.isBot,
  }))
  const shown = (search.active ? found : suggestions).filter((contact) => !exclude.includes(contact.userId))

  function pick(contact: Contact) {
    setQuery('')
    onPick(contact)
  }

  // Enter picks the first match instead of submitting the surrounding form.
  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter') return
    event.preventDefault()
    if (!disabled && shown.length > 0) pick(shown[0])
  }

  return (
    <div className="stack">
      <label className="field">
        <span>{label}</span>
        <input
          type="search"
          name="userSearch"
          autoComplete="off"
          placeholder="Имя, email или ID"
          maxLength={limits.emailMax}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={handleKeyDown}
        />
      </label>
      {search.loading && <p className="muted small">Ищем…</p>}
      {search.active && !search.loading && search.error == null && shown.length === 0 && (
        <p className="muted small">Никого не нашли. Имя ищется по началу слова, email — целиком.</p>
      )}
      <ErrorText error={search.error} />
      {shown.length > 0 && (
        <ul className="list picker" aria-label={search.active ? 'Найденные' : 'Контакты'}>
          {shown.map((contact) => (
            <li key={contact.userId}>
              <button type="button" className="picker-item" disabled={disabled} onClick={() => pick(contact)}>
                {displayName(contact.name, contact.userId)}
                {contact.isBot && <BotBadge />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
