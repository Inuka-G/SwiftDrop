import { useCallback, useEffect, useMemo, useState, type PropsWithChildren } from 'react'
import { SESSION_EXPIRED_EVENT } from '../../lib/axios'
import { authService, type AuthResult, type AuthUser, type SignupData } from '../../services/auth.service'
import { storageService } from '../../services/storage.service'
import { AuthContext, type AuthContextValue } from './AuthContext'

export function AuthProvider({ children }: PropsWithChildren) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(() => storageService.getAccessToken() !== null)

  // On page load, check the saved token with the server and get the user's details.
  useEffect(() => {
    if (!storageService.getAccessToken()) return
    let cancelled = false
    authService
      .me()
      .then((me) => !cancelled && setUser(me))
      .catch(() => storageService.clearAccessToken())
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  // The API client fires this when the server rejects the token, for example after it expires.
  useEffect(() => {
    const onExpired = () => setUser(null)
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired)
  }, [])

  const accept = useCallback((result: AuthResult, remember: boolean) => {
    storageService.setAccessToken(result.accessToken, remember)
    setUser(result.user)
    return result.user
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      login: async (username, password, remember) => accept(await authService.login(username, password), remember),
      signup: async (data: SignupData) => accept(await authService.signup(data), true),
      completeGoogleLogin: async (code) => accept(await authService.exchangeGoogleCode(code), true),
      logout: () => {
        storageService.clearAccessToken()
        setUser(null)
      },
    }),
    [user, isLoading, accept],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
