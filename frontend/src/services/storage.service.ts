import { AUTH_TOKEN_KEY } from '../lib/constants'

export const storageService = {
  getAccessToken(): string | null {
    return localStorage.getItem(AUTH_TOKEN_KEY)
  },
  setAccessToken(token: string): void {
    localStorage.setItem(AUTH_TOKEN_KEY, token)
  },
  clearAccessToken(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY)
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
