import { useState, type FormEvent } from 'react'
import { ApiError, api, errorMessage, unwrap } from '../api/client'
import { useAuth, useMe } from '../auth/context'
import { CopyButton, ErrorText } from '../components/common'
import { limits } from '../limits'

function ProfileForm({ submitLabel, onSaved }: { submitLabel: string; onSaved?: () => void }) {
  const me = useMe()
  const { reloadMe } = useAuth()
  const [name, setName] = useState(me.profile?.displayName ?? '')
  const [bio, setBio] = useState(me.profile?.bio ?? '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await unwrap(
        api.PUT('/api/profiles/{userId}', {
          params: { path: { userId: me.accountId } },
          body: { displayName: name.trim(), bio: bio.trim() === '' ? null : bio.trim() },
        }),
      )
      await reloadMe()
      onSaved?.()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="stack" onSubmit={submit}>
      <label className="field">
        <span>Имя</span>
        <input
          name="displayName"
          required
          maxLength={limits.displayNameMax}
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>
      <label className="field">
        <span>О себе</span>
        <textarea
          name="bio"
          rows={3}
          maxLength={limits.bioMax}
          value={bio}
          onChange={(event) => setBio(event.target.value)}
        />
      </label>
      <ErrorText error={error} />
      <div>
        <button type="submit" className="button" disabled={busy || name.trim() === ''}>
          {submitLabel}
        </button>
      </div>
    </form>
  )
}

function PasswordForm() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setSaved(false)
    try {
      await unwrap(api.POST('/api/account/password', { body: { currentPassword, newPassword } }))
      setCurrentPassword('')
      setNewPassword('')
      setSaved(true)
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) setError('Неверный текущий пароль')
      else setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="stack" onSubmit={submit}>
      <h2>Пароль</h2>
      <label className="field">
        <span>Текущий пароль</span>
        <input
          type="password"
          name="currentPassword"
          autoComplete="current-password"
          required
          maxLength={limits.passwordMax}
          value={currentPassword}
          onChange={(event) => setCurrentPassword(event.target.value)}
        />
      </label>
      <label className="field">
        <span>Новый пароль</span>
        <input
          type="password"
          name="newPassword"
          autoComplete="new-password"
          required
          minLength={limits.passwordMin}
          maxLength={limits.passwordMax}
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
        />
        <small className="muted">Не короче {limits.passwordMin} символов</small>
      </label>
      <ErrorText error={error} />
      <div>
        <button type="submit" className="button" disabled={busy}>
          Сменить пароль
        </button>
      </div>
      {saved && <p className="success">Пароль изменён</p>}
    </form>
  )
}

// Registration creates only the account; the profile is a separate step, like "What's your name?" in Telegram.
export function WelcomePage() {
  return (
    <main className="auth-page">
      <div className="card auth-card">
        <h1>Как вас зовут?</h1>
        <p className="muted">Имя увидят собеседники в чатах.</p>
        <ProfileForm submitLabel="Продолжить" />
      </div>
    </main>
  )
}

export function ProfilePage() {
  const me = useMe()
  const [saved, setSaved] = useState(false)

  return (
    <div className="page">
      <h1>Профиль</h1>
      <section className="card stack">
        <div className="kv">
          <span className="muted">Email</span>
          <span>{me.email}</span>
        </div>
        <div className="kv">
          <span className="muted">Ваш ID</span>
          <span className="row">
            <code data-testid="my-id">{me.accountId}</code>
            <CopyButton value={me.accountId} />
          </span>
        </div>
        <p className="muted small">Поделитесь ID, чтобы вам могли написать или добавить в группу.</p>
      </section>
      <section className="card">
        <ProfileForm submitLabel="Сохранить" onSaved={() => setSaved(true)} />
        {saved && <p className="success">Сохранено</p>}
      </section>
      <section className="card">
        <PasswordForm />
      </section>
    </div>
  )
}
