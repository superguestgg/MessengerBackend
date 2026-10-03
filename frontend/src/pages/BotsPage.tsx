import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { api, apiUrl, errorMessage, isAbort, unwrap, type Bot } from '../api/client'
import { CopyButton, ErrorText, SecretReveal } from '../components/common'
import { displayName, formatDateTime, shortId } from '../format'
import { limits } from '../limits'

interface Revealed {
  title: string
  token: string
}

function mcpCommand(token: string) {
  const origin = apiUrl !== '' ? apiUrl : window.location.origin
  return `claude mcp add --transport http messenger ${origin}/mcp --header "Authorization: Bearer ${token}"`
}

export function BotsPage() {
  const navigate = useNavigate()
  const [bots, setBots] = useState<Bot[] | null>(null)
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [revealed, setRevealed] = useState<Revealed | null>(null)
  const [editing, setEditing] = useState<{ botId: string; name: string } | null>(null)

  const load = useCallback(async () => {
    setBots(await unwrap(api.GET('/api/bots')))
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    unwrap(api.GET('/api/bots', { signal: controller.signal }))
      .then(setBots)
      .catch((err: unknown) => {
        if (!isAbort(err)) setError(errorMessage(err))
      })
    return () => controller.abort()
  }, [])

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await action()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  function create(event: FormEvent) {
    event.preventDefault()
    void run(async () => {
      const displayName = name.trim()
      const result = await unwrap(api.POST('/api/bots', { body: { displayName } }))
      setName('')
      setRevealed({ title: `Токен бота «${displayName}»`, token: result.token })
      await load()
    })
  }

  function reissue(bot: Bot) {
    if (!window.confirm('Выпустить новый токен? Старый сразу перестанет работать.')) return
    void run(async () => {
      const result = await unwrap(
        api.POST('/api/bots/{botId}/token', { params: { path: { botId: bot.botId } } }),
      )
      setRevealed({ title: `Новый токен бота «${displayName(bot.displayName, bot.botId)}»`, token: result.token })
    })
  }

  function remove(bot: Bot) {
    if (!window.confirm(`Удалить бота «${displayName(bot.displayName, bot.botId)}»? Его токен перестанет работать.`)) return
    void run(async () => {
      await unwrap(api.DELETE('/api/bots/{botId}', { params: { path: { botId: bot.botId } } }))
      await load()
    })
  }

  function rename(event: FormEvent) {
    event.preventDefault()
    if (editing == null) return
    const { botId, name: newName } = editing
    void run(async () => {
      await unwrap(
        api.PUT('/api/profiles/{userId}', {
          params: { path: { userId: botId } },
          body: { displayName: newName.trim() },
        }),
      )
      setEditing(null)
      await load()
    })
  }

  function openChat(bot: Bot) {
    void run(async () => {
      const result = await unwrap(api.POST('/api/chats/direct', { body: { userId: bot.botId } }))
      navigate(`/chats/${result.chatId}`)
    })
  }

  return (
    <div className="page">
      <h1>Боты</h1>
      <p className="muted">
        Бот входит по токену — например, агент через MCP. Первым бот может написать только вам, своему владельцу.
      </p>

      <form className="card row wrap" onSubmit={create}>
        <input
          name="botName"
          placeholder="Имя нового бота"
          aria-label="Имя нового бота"
          required
          maxLength={limits.displayNameMax}
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
        <button type="submit" className="button" disabled={busy || name.trim() === ''}>
          Создать бота
        </button>
      </form>

      <ErrorText error={error} />

      {revealed != null && (
        <SecretReveal title={revealed.title} token={revealed.token} onClose={() => setRevealed(null)}>
          <p className="muted small">Подключение к Claude Code:</p>
          <div className="secret-value">
            <code>{mcpCommand(revealed.token)}</code>
            <CopyButton value={mcpCommand(revealed.token)} />
          </div>
        </SecretReveal>
      )}

      {bots == null ? (
        <p className="muted">Загрузка…</p>
      ) : bots.length === 0 ? (
        <p className="muted">Ботов пока нет.</p>
      ) : (
        <ul className="list card" aria-label="Мои боты">
          {bots.map((bot) => (
            <li key={bot.botId} className="list-item">
              {editing?.botId === bot.botId ? (
                <form className="row wrap grow" onSubmit={rename}>
                  <input
                    aria-label="Новое имя бота"
                    required
                    maxLength={limits.displayNameMax}
                    value={editing.name}
                    onChange={(event) => setEditing({ botId: bot.botId, name: event.target.value })}
                  />
                  <button type="submit" className="button small" disabled={busy || editing.name.trim() === ''}>
                    Сохранить
                  </button>
                  <button type="button" className="button small ghost" onClick={() => setEditing(null)}>
                    Отмена
                  </button>
                </form>
              ) : (
                <div className="grow">
                  <div className="item-title">{displayName(bot.displayName, bot.botId)}</div>
                  <div className="muted small row">
                    <code title={bot.botId}>{shortId(bot.botId)}…</code>
                    <CopyButton value={bot.botId} label="ID" />
                    <span>создан {formatDateTime(bot.createdAt)}</span>
                  </div>
                </div>
              )}
              <div className="row wrap actions">
                <button type="button" className="button small" disabled={busy} onClick={() => openChat(bot)}>
                  Написать
                </button>
                <button
                  type="button"
                  className="button small secondary"
                  disabled={busy}
                  onClick={() => setEditing({ botId: bot.botId, name: bot.displayName ?? '' })}
                >
                  Переименовать
                </button>
                <button type="button" className="button small secondary" disabled={busy} onClick={() => reissue(bot)}>
                  Новый токен
                </button>
                <button type="button" className="button small danger" disabled={busy} onClick={() => remove(bot)}>
                  Удалить
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
