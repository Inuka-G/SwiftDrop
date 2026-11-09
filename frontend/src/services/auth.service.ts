import { apiClient } from '../lib/axios'
import { storageService } from './storage.service'

export const authService = {
  getAccessToken: storageService.getAccessToken,
  clearAccessToken: storageService.clearAccessToken,
  async login(email: string, password: string): Promise<void> {
    const response = await apiClient.post<{ accessToken: string }>('/identity/login', {
      email,
      password,
    })
    storageService.setAccessToken(response.data.accessToken)
  },
}
