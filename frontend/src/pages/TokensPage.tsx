import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api, errorMessage, isAbort, unwrap, type AccessToken } from '../api/client'
import { ErrorText, SecretReveal } from '../components/common'
import { formatDateTime } from '../format'
import { limits } from '../limits'

export function TokensPage() {
  const [tokens, setTokens] = useState<AccessToken[] | null>(null)
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [revealed, setRevealed] = useState<{ name: string; token: string } | null>(null)

  const load = useCallback(async () => {
    setTokens(await unwrap(api.GET('/api/tokens')))
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    unwrap(api.GET('/api/tokens', { signal: controller.signal }))
      .then(setTokens)
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

  function issue(event: FormEvent) {
    event.preventDefault()
    void run(async () => {
      const result = await unwrap(api.POST('/api/tokens', { body: { name: name.trim() } }))
      setName('')
      setRevealed({ name: result.name, token: result.token })
      await load()
    })
  }

  function revoke(token: AccessToken) {
    if (!window.confirm(`Отозвать токен «${token.name}»? Агенты с ним потеряют доступ.`)) return
    void run(async () => {
      await unwrap(api.DELETE('/api/tokens/{tokenId}', { params: { path: { tokenId: token.tokenId } } }))
      await load()
    })
  }

  return (
    <div className="page">
      <h1>Личные токены</h1>
      <p className="muted">
        Токен даёт агенту доступ от вашего имени ко всем вашим чатам. Если агенту достаточно отдельного чата, лучше
        создайте ему бота.
      </p>

      <form className="card row wrap" onSubmit={issue}>
        <input
          name="tokenName"
          placeholder="Название, например «ноутбук»"
          aria-label="Название токена"
          required
          maxLength={limits.tokenNameMax}
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
        <button type="submit" className="button" disabled={busy || name.trim() === ''}>
          Выпустить токен
        </button>
      </form>

      <ErrorText error={error} />

      {revealed != null && (
        <SecretReveal title={`Токен «${revealed.name}»`} token={revealed.token} onClose={() => setRevealed(null)} />
      )}

      {tokens == null ? (
        <p className="muted">Загрузка…</p>
      ) : tokens.length === 0 ? (
        <p className="muted">Активных токенов нет.</p>
      ) : (
        <ul className="list card" aria-label="Мои токены">
          {tokens.map((token) => (
            <li key={token.tokenId} className="list-item">
              <div className="grow">
                <div className="item-title">{token.name}</div>
                <div className="muted small">выпущен {formatDateTime(token.createdAt)}</div>
              </div>
              <button type="button" className="button small danger" disabled={busy} onClick={() => revoke(token)}>
                Отозвать
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
