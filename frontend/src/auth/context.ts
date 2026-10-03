import { createContext, useContext } from 'react'
import type { Me } from '../api/client'

export interface AuthValue {
  me: Me | null
  status: 'signedOut' | 'loading' | 'failed' | 'signedIn'
  // Why the user was signed out, shown on the login screen.
  notice: string | null
  login(email: string, password: string): Promise<void>
  register(email: string, password: string): Promise<void>
  logout(): void
  reloadMe(): Promise<void>
}

export const AuthContext = createContext<AuthValue | null>(null)

export function useAuth(): AuthValue {
  const value = useContext(AuthContext)
  if (value == null) throw new Error('useAuth must be used inside AuthProvider')
  return value
}

// For screens behind RequireAuth, where the account is always loaded.
export function useMe(): Me {
  const { me } = useAuth()
  if (me == null) throw new Error('useMe must be used for a signed-in account')
  return me
}
