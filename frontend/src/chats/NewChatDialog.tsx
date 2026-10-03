import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api, errorMessage, unwrap } from '../api/client'
import { useMe } from '../auth/context'
import { BotBadge, CopyButton, ErrorText, Modal } from '../components/common'
import { displayName, isUuid } from '../format'
import { limits } from '../limits'
import { useChats } from './model'
import { useContacts } from './useContacts'

function parseIds(text: string) {
  return text
    .split(/[\s,;]+/)
    .map((part) => part.trim())
    .filter((part) => part !== '')
}

export function NewChatDialog({ onClose }: { onClose: () => void }) {
  const me = useMe()
  const navigate = useNavigate()
  const { refreshChats } = useChats()
  const contacts = useContacts()
  const [tab, setTab] = useState<'direct' | 'group'>('direct')
  const [userId, setUserId] = useState('')
  const [title, setTitle] = useState('')
  const [selected, setSelected] = useState<string[]>([])
  const [extraIds, setExtraIds] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

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

  function submitDirect(event: FormEvent) {
    event.preventDefault()
    if (!isUuid(userId)) {
      setError('Это не похоже на ID пользователя')
      return
    }
    openDirect(userId)
  }

  function submitGroup(event: FormEvent) {
    event.preventDefault()
    const extra = parseIds(extraIds)
    const invalid = extra.find((id) => !isUuid(id))
    if (invalid != null) {
      setError(`Неверный ID: ${invalid}`)
      return
    }
    const memberIds = [...new Set([...selected, ...extra])].filter((id) => id !== me.accountId)
    void open(() => unwrap(api.POST('/api/chats/group', { body: { title: title.trim(), memberIds } })))
  }

  function toggle(id: string) {
    setSelected((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]))
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
        <div className="stack">
          {contacts.length > 0 && (
            <ul className="list picker" aria-label="Контакты">
              {contacts.map((contact) => (
                <li key={contact.userId}>
                  <button
                    type="button"
                    className="picker-item"
                    disabled={busy}
                    onClick={() => openDirect(contact.userId)}
                  >
                    {displayName(contact.name, contact.userId)}
                    {contact.isBot && <BotBadge />}
                  </button>
                </li>
              ))}
            </ul>
          )}
          <form className="stack" onSubmit={submitDirect}>
            <label className="field">
              <span>ID пользователя</span>
              <input
                name="userId"
                placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                value={userId}
                onChange={(event) => setUserId(event.target.value)}
              />
            </label>
            <div>
              <button type="submit" className="button" disabled={busy || userId.trim() === ''}>
                Открыть чат
              </button>
            </div>
          </form>
        </div>
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
          {contacts.length > 0 && (
            <fieldset className="field">
              <legend>Участники</legend>
              <ul className="list picker" aria-label="Участники">
                {contacts.map((contact) => (
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
          <label className="field">
            <span>Ещё участники — ID через пробел или с новой строки</span>
            <textarea
              name="memberIds"
              rows={2}
              value={extraIds}
              onChange={(event) => setExtraIds(event.target.value)}
            />
          </label>
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
