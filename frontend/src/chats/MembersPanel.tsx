import { useState, type FormEvent } from 'react'
import { api, errorMessage, unwrap, type Chat, type ChatRole } from '../api/client'
import { BotBadge, ErrorText } from '../components/common'
import { displayName, isUuid } from '../format'
import { useContacts } from './useContacts'

const roleNames: Record<ChatRole, string> = {
  Owner: 'владелец',
  Admin: 'админ',
  Member: 'участник',
}

export function MembersPanel({ chat, myId, onChanged }: { chat: Chat; myId: string; onChanged: () => void }) {
  const contacts = useContacts()
  const [userId, setUserId] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const myRole = chat.members.find((member) => member.userId === myId)?.role
  const canAdd = chat.type === 'Group' && (myRole === 'Owner' || myRole === 'Admin')
  const candidates = contacts.filter((contact) => !chat.members.some((member) => member.userId === contact.userId))

  async function add(event: FormEvent) {
    event.preventDefault()
    if (!isUuid(userId)) {
      setError('Это не похоже на ID пользователя')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await unwrap(
        api.POST('/api/chats/{chatId}/members', {
          params: { path: { chatId: chat.chatId } },
          body: { userId: userId.trim() },
        }),
      )
      setUserId('')
      onChanged()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <aside className="members" aria-label="Участники">
      <h3>Участники · {chat.members.length}</h3>
      <ul className="list">
        {chat.members.map((member) => (
          <li key={member.userId} className="member">
            <span className="ellipsis">
              {displayName(member.displayName, member.userId)}
              {member.userId === myId && <span className="muted"> (вы)</span>}
            </span>
            {member.isBot && <BotBadge />}
            {chat.type === 'Group' && <span className="muted small">{roleNames[member.role]}</span>}
          </li>
        ))}
      </ul>
      {canAdd && (
        <form className="stack" onSubmit={add}>
          <label className="field">
            <span>Добавить участника</span>
            <input
              name="memberId"
              list="member-candidates"
              placeholder="ID пользователя"
              value={userId}
              onChange={(event) => setUserId(event.target.value)}
            />
            <datalist id="member-candidates">
              {candidates.map((contact) => (
                <option key={contact.userId} value={contact.userId}>
                  {displayName(contact.name, contact.userId)}
                  {contact.isBot ? ' (бот)' : ''}
                </option>
              ))}
            </datalist>
          </label>
          <ErrorText error={error} />
          <div>
            <button type="submit" className="button small" disabled={busy || userId.trim() === ''}>
              Добавить
            </button>
          </div>
        </form>
      )}
    </aside>
  )
}
