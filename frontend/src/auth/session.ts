// The token lives in localStorage and goes into the Authorization header: the API doesn't use cookies.
export interface Session {
  token: string
  accountId: string
  expiresAt: string
}

export const sessionKey = 'messenger.session'

export function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(sessionKey)
    if (raw == null) return null
    const session = JSON.parse(raw) as Session
    if (Date.parse(session.expiresAt) <= Date.now()) {
      localStorage.removeItem(sessionKey)
      return null
    }
    return session
  } catch {
    return null
  }
}

export function saveSession(session: Session) {
  try {
    localStorage.setItem(sessionKey, JSON.stringify(session))
  } catch {
    // Private mode: the session lasts until the tab is closed.
  }
}

export function clearSession() {
  try {
    localStorage.removeItem(sessionKey)
  } catch {
    // Nothing to clear.
  }
}
