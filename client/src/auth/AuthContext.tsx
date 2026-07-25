import { createContext, use, useCallback, useEffect, useState, type ReactNode } from 'react'
import { ApiError } from '../api/client'
import { identityApi, type CurrentUser } from '../api/identity'

interface AuthContextValue {
  user: CurrentUser | null
  status: 'loading' | 'authenticated' | 'unauthenticated'
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [status, setStatus] = useState<AuthContextValue['status']>('loading')

  useEffect(() => {
    identityApi
      .me()
      .then((currentUser) => {
        setUser(currentUser)
        setStatus('authenticated')
      })
      .catch((error) => {
        if (!(error instanceof ApiError && error.status === 401)) {
          console.error('Failed to load the current session', error)
        }
        setStatus('unauthenticated')
      })
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const currentUser = await identityApi.login(email, password)
    setUser(currentUser)
    setStatus('authenticated')
  }, [])

  const logout = useCallback(async () => {
    await identityApi.logout()
    setUser(null)
    setStatus('unauthenticated')
  }, [])

  return <AuthContext value={{ user, status, login, logout }}>{children}</AuthContext>
}

export function useAuth() {
  const context = use(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
