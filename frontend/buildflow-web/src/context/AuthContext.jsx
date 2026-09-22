import { useCallback, useEffect, useMemo, useState } from 'react'
import { authService } from '../services/authService.js'
import { clearSession, getSession, saveSession } from '../services/tokenService.js'
import { AuthContext } from './auth-context.js'

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => getSession()?.user ?? null)
  const [isLoading, setIsLoading] = useState(() => Boolean(getSession()))

  const clearAuth = useCallback(() => {
    clearSession()
    setUser(null)
  }, [])

  useEffect(() => {
    const session = getSession()
    if (!session) return
    authService.me()
      .then((currentUser) => {
        const latestSession = getSession()
        if (latestSession) saveSession({ ...latestSession, user: currentUser }, latestSession.remember)
        setUser(currentUser)
      })
      .catch(clearAuth)
      .finally(() => setIsLoading(false))
  }, [clearAuth])

  useEffect(() => {
    window.addEventListener('buildflow:session-expired', clearAuth)
    return () => window.removeEventListener('buildflow:session-expired', clearAuth)
  }, [clearAuth])

  const authenticate = useCallback(async (operation, payload, remember = false) => {
    const response = await operation(payload)
    saveSession(response, remember)
    setUser(response.user)
    return response.user
  }, [])
  const login = useCallback((payload, remember) => authenticate(authService.login, payload, remember), [authenticate])
  const register = useCallback((payload) => authenticate(authService.register, payload), [authenticate])
  const logout = useCallback(async () => {
    try { await authService.logout() } finally { clearAuth() }
  }, [clearAuth])

  const value = useMemo(() => ({ user, isLoading, login, register, logout }), [user, isLoading, login, register, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
