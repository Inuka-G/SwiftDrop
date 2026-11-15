import { AUTH_TOKEN_KEY } from '../lib/constants'

/**
 * "Remember me" keeps the token in localStorage (survives closing the browser).
 * Otherwise it goes in sessionStorage and is gone when the tab closes.
 */
export const storageService = {
  getAccessToken(): string | null {
    return sessionStorage.getItem(AUTH_TOKEN_KEY) ?? localStorage.getItem(AUTH_TOKEN_KEY)
  },
  setAccessToken(token: string, remember = true): void {
    storageService.clearAccessToken()
    ;(remember ? localStorage : sessionStorage).setItem(AUTH_TOKEN_KEY, token)
  },
  clearAccessToken(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY)
    sessionStorage.removeItem(AUTH_TOKEN_KEY)
  },
  getUser(): { fullName: string; role: string } | null {
    const value = localStorage.getItem('swiftdrop.user')
    if (!value) return null
    try { return JSON.parse(value) as { fullName: string; role: string } } catch { return null }
  },
  setUser(user: { fullName: string; role: string }): void {
    localStorage.setItem('swiftdrop.user', JSON.stringify(user))
  },
  clearUser(): void {
    localStorage.removeItem('swiftdrop.user')
  },
}
