import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api, errorMessage, unwrap } from '../api/client'
import { useMe } from '../auth/context'
import { BotBadge, CopyButton, ErrorText, Modal } from '../components/common'
import { displayName } from '../format'
import { limits } from '../limits'
import { useChats, type Contact } from './model'
import { UserSearch } from './UserSearch'
import { useContacts } from './useContacts'

export function NewChatDialog({ onClose }: { onClose: () => void }) {
  const me = useMe()
  const navigate = useNavigate()
  const { refreshChats } = useChats()
  const contacts = useContacts()
  const [tab, setTab] = useState<'direct' | 'group'>('direct')
  const [title, setTitle] = useState('')
  const [selected, setSelected] = useState<string[]>([])
  // People found by search who are not among the contacts yet.
  const [found, setFound] = useState<Contact[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const candidates = [...contacts, ...found]

  async function open(create: () => Promise<{ chatId: string }>) {
    setBusy(true)
    setError(null)
    try {
      const { chatId } = await create()
      refreshChats()
      onClose()
      navigate(`/chats/${chatId}`)
    } catch (err) {
      setError(errorMessage(err))
      setBusy(false)
    }
  }

  function openDirect(id: string) {
    void open(() => unwrap(api.POST('/api/chats/direct', { body: { userId: id.trim() } })))
  }

  function submitGroup(event: FormEvent) {
    event.preventDefault()
    const memberIds = selected.filter((id) => id !== me.accountId)
    void open(() => unwrap(api.POST('/api/chats/group', { body: { title: title.trim(), memberIds } })))
  }

  function toggle(id: string) {
    setSelected((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]))
  }

  function addMember(contact: Contact) {
    if (!candidates.some((x) => x.userId === contact.userId)) {
      setFound((current) => [...current, contact])
    }
    setSelected((current) => (current.includes(contact.userId) ? current : [...current, contact.userId]))
  }

  return (
    <Modal title="Новый чат" onClose={onClose}>
      <div className="tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'direct'}
          className={tab === 'direct' ? 'tab active' : 'tab'}
          onClick={() => setTab('direct')}
        >
          Личный
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'group'}
          className={tab === 'group' ? 'tab active' : 'tab'}
          onClick={() => setTab('group')}
        >
          Группа
        </button>
      </div>

      {tab === 'direct' ? (
        <UserSearch
          label="Найти собеседника"
          suggestions={contacts}
          exclude={[me.accountId]}
          disabled={busy}
          onPick={(contact) => openDirect(contact.userId)}
        />
      ) : (
        <form className="stack" onSubmit={submitGroup}>
          <label className="field">
            <span>Название</span>
            <input
              name="title"
              required
              maxLength={limits.chatTitleMax}
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />
          </label>
          {candidates.length > 0 && (
            <fieldset className="field">
              <legend>Участники</legend>
              <ul className="list picker" aria-label="Участники">
                {candidates.map((contact) => (
                  <li key={contact.userId}>
                    <label className="picker-item">
                      <input
                        type="checkbox"
                        checked={selected.includes(contact.userId)}
                        onChange={() => toggle(contact.userId)}
                      />
                      {displayName(contact.name, contact.userId)}
                      {contact.isBot && <BotBadge />}
                    </label>
                  </li>
                ))}
              </ul>
            </fieldset>
          )}
          <UserSearch
            label="Добавить участника"
            exclude={[me.accountId, ...selected]}
            disabled={busy}
            onPick={addMember}
          />
          <div>
            <button type="submit" className="button" disabled={busy || title.trim() === ''}>
              Создать группу
            </button>
          </div>
        </form>
      )}

      <ErrorText error={error} />

      <p className="muted small row wrap">
        Ваш ID для собеседников: <code>{me.accountId}</code> <CopyButton value={me.accountId} />
      </p>
    </Modal>
  )
}
