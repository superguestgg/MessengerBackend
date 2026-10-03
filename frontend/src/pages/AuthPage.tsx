import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, errorMessage } from '../api/client'
import { useAuth } from '../auth/context'
import { ErrorText } from '../components/common'
import { limits } from '../limits'

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { login, register, notice } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const isLogin = mode === 'login'

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      if (isLogin) await login(email, password)
      else await register(email, password)
    } catch (err) {
      if (err instanceof ApiError && isLogin && err.status === 401) setError('Неверный email или пароль')
      else if (err instanceof ApiError && !isLogin && err.status === 409) setError('Этот email уже зарегистрирован')
      else setError(errorMessage(err))
      setBusy(false)
    }
  }

  return (
    <main className="auth-page">
      <form className="card auth-card" onSubmit={submit}>
        <h1>Messenger</h1>
        <h2>{isLogin ? 'Вход' : 'Регистрация'}</h2>
        {notice != null && isLogin && <p className="notice">{notice}</p>}

        <label className="field">
          <span>Email</span>
          <input
            type="email"
            name="email"
            autoComplete="email"
            required
            maxLength={limits.emailMax}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </label>

        <label className="field">
          <span>Пароль</span>
          <input
            type="password"
            name="password"
            autoComplete={isLogin ? 'current-password' : 'new-password'}
            required
            minLength={isLogin ? undefined : limits.passwordMin}
            maxLength={limits.passwordMax}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
          {!isLogin && <small className="muted">Не короче {limits.passwordMin} символов</small>}
        </label>

        <ErrorText error={error} />

        <button type="submit" className="button" disabled={busy}>
          {isLogin ? 'Войти' : 'Зарегистрироваться'}
        </button>

        <p className="muted center">
          {isLogin ? (
            <>
              Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
            </>
          ) : (
            <>
              Уже есть аккаунт? <Link to="/login">Войти</Link>
            </>
          )}
        </p>
      </form>
    </main>
  )
}
