import { useState } from 'react'
import { api, errorMessage, unwrap, type Chat, type ChatRole } from '../api/client'
import { BotBadge, ErrorText } from '../components/common'
import { displayName } from '../format'
import { UserSearch } from './UserSearch'
import { useContacts } from './useContacts'

const roleNames: Record<ChatRole, string> = {
  Owner: 'владелец',
  Admin: 'админ',
  Member: 'участник',
}

export function MembersPanel({ chat, myId, onChanged }: { chat: Chat; myId: string; onChanged: () => void }) {
  const contacts = useContacts()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const myRole = chat.members.find((member) => member.userId === myId)?.role
  const canAdd = chat.type === 'Group' && (myRole === 'Owner' || myRole === 'Admin')

  async function add(userId: string) {
    setBusy(true)
    setError(null)
    try {
      await unwrap(
        api.POST('/api/chats/{chatId}/members', {
          params: { path: { chatId: chat.chatId } },
          body: { userId },
        }),
      )
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
        <div className="stack">
          <UserSearch
            label="Добавить участника"
            suggestions={contacts}
            exclude={chat.members.map((member) => member.userId)}
            disabled={busy}
            onPick={(contact) => void add(contact.userId)}
          />
          <ErrorText error={error} />
        </div>
      )}
    </aside>
  )
}
