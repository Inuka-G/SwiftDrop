import { apiClient } from '../lib/axios'
import type { AuthResult, AuthUser } from '../types/common.types'
import { storageService } from './storage.service'

export const authService = {
  getAccessToken: storageService.getAccessToken,
  getCurrentUser(): AuthUser | null {
    return storageService.getAccessToken() ? storageService.getUser() : null
  },
  async login(username: string, password: string): Promise<AuthUser> {
    const response = await apiClient.post<AuthResult>('/auth/login', { username, password })
    storageService.setAccessToken(response.data.accessToken)
    storageService.setUser(response.data.user)
    return response.data.user
  },
  logout(): void {
    storageService.clearSession()
  },
}
