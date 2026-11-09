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
}
