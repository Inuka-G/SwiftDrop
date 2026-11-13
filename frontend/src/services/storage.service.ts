import { AUTH_TOKEN_KEY } from '../lib/constants'

/**
 * "Remember me" keeps the access token in localStorage (survives closing the browser).
 * Otherwise it goes in sessionStorage and is gone when the tab closes.
 * The refresh token is never stored here: it lives in an HttpOnly cookie that JavaScript cannot read.
 */
export const storageService = {
  getAccessToken(): string | null {
    return sessionStorage.getItem(AUTH_TOKEN_KEY) ?? localStorage.getItem(AUTH_TOKEN_KEY)
  },
  setAccessToken(token: string, remember = true): void {
    storageService.clearAccessToken()
    ;(remember ? localStorage : sessionStorage).setItem(AUTH_TOKEN_KEY, token)
  },
  /** Swaps in a refreshed token, keeping it wherever the old one was stored. */
  replaceAccessToken(token: string): void {
    const remembered = sessionStorage.getItem(AUTH_TOKEN_KEY) === null
    storageService.setAccessToken(token, remembered)
  },
  clearAccessToken(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY)
    sessionStorage.removeItem(AUTH_TOKEN_KEY)
  },
}
