import { apiClient } from '../lib/axios'
import { storageService } from './storage.service'

export const authService = {
  getAccessToken: storageService.getAccessToken,
  clearAccessToken(): void {
    storageService.clearAccessToken()
    storageService.clearUser()
  },
  async login(email: string, password: string): Promise<void> {
    const response = await apiClient.post<{ accessToken: string; user: { fullName: string; role: string } }>('/auth/login', {
      username: email,
      password,
    })
    storageService.setAccessToken(response.data.accessToken)
    storageService.setUser(response.data.user)
  },
}
