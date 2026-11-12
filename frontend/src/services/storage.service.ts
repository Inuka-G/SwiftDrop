import { AUTH_TOKEN_KEY, AUTH_USER_KEY } from '../lib/constants'
import type { AuthUser } from '../types/common.types'

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
  getUser(): AuthUser | null {
    const raw = localStorage.getItem(AUTH_USER_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as AuthUser
    } catch {
      return null
    }
  },
  setUser(user: AuthUser): void {
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(user))
  },
  clearSession(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(AUTH_USER_KEY)
  },
}
