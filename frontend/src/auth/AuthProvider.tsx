import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, ApiError, isAbort, setAccessToken, setUnauthorizedHandler, unwrap, type Me } from '../api/client'
import { AuthContext, type AuthValue } from './context'
import { clearSession, loadSession, saveSession, sessionKey, type Session } from './session'

const expiredNotice = 'Сессия истекла, войдите снова'

// setTimeout fires immediately for delays above ~24.8 days.
const maxTimeout = 2 ** 31 - 1

function restoreSession() {
  const session = loadSession()
  setAccessToken(session?.token ?? null)
  return session
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(restoreSession)
  const [me, setMe] = useState<Me | null>(null)
  const [meFailed, setMeFailed] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  const signOut = useCallback((reason: string | null) => {
    clearSession()
    setAccessToken(null)
    setSession(null)
    setMe(null)
    setMeFailed(false)
    setNotice(reason)
  }, [])

  const logout = useCallback(() => signOut(null), [signOut])

  useEffect(() => {
    setUnauthorizedHandler(() => signOut(expiredNotice))
    return () => setUnauthorizedHandler(null)
  }, [signOut])

  useEffect(() => {
    if (session == null) return
    const delay = Math.max(Date.parse(session.expiresAt) - Date.now(), 0)
    const timer = setTimeout(() => signOut(expiredNotice), Math.min(delay, maxTimeout))
    return () => clearTimeout(timer)
  }, [session, signOut])

  // Signing in or out in another tab applies here too.
  useEffect(() => {
    function onStorage(event: StorageEvent) {
      if (event.key !== sessionKey) return
      const next = restoreSession()
      setSession(next)
      setMe(null)
      setMeFailed(false)
    }
    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  const applyMe = useCallback((request: Promise<Me>) => {
    return request
      .then((result) => {
        setMe(result)
        setMeFailed(false)
      })
      .catch((error: unknown) => {
        // 401 already signed the user out.
        if (isAbort(error) || (error instanceof ApiError && error.status === 401)) return
        setMeFailed(true)
      })
  }, [])

  useEffect(() => {
    if (session == null) return
    const controller = new AbortController()
    void applyMe(unwrap(api.GET('/api/me', { signal: controller.signal })))
    return () => controller.abort()
  }, [session, applyMe])

  const login = useCallback(async (email: string, password: string) => {
    const result = await unwrap(api.POST('/api/auth/login', { body: { email, password } }))
    const next: Session = {
      token: result.accessToken,
      accountId: result.accountId,
      expiresAt: result.expiresAt,
    }
    saveSession(next)
    setAccessToken(next.token)
    setNotice(null)
    setMe(null)
    setMeFailed(false)
    setSession(next)
  }, [])

  const register = useCallback(async (email: string, password: string) => {
    await unwrap(api.POST('/api/account/register', { body: { email, password } }))
    await login(email, password)
  }, [login])

  const reloadMe = useCallback(() => applyMe(unwrap(api.GET('/api/me'))), [applyMe])

  const value = useMemo<AuthValue>(() => {
    let status: AuthValue['status'] = 'signedOut'
    if (session != null) status = me != null ? 'signedIn' : meFailed ? 'failed' : 'loading'
    return { me, status, notice, login, register, logout, reloadMe }
  }, [session, me, meFailed, notice, login, register, logout, reloadMe])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
