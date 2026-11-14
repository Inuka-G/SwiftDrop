import { apiClient } from '../lib/axios'
import { storageService } from './storage.service'

export interface AuthUser {
  id: string
  fullName: string
  email: string
  username: string | null
  role: string
}

export interface AuthResult {
  accessToken: string
  expiresAtUtc: string
  user: AuthUser
}

export interface SignupData {
  fullName: string
  email: string
  password: string
  phoneNumber?: string
}

export const authService = {
  getAccessToken: storageService.getAccessToken,
  clearAccessToken: storageService.clearAccessToken,

  /** `username` can be a username such as "admin" or an email address. */
  async login(username: string, password: string): Promise<AuthResult> {
    const response = await apiClient.post<AuthResult>('/auth/login', { username, password })
    return response.data
  },

  async signup(data: SignupData): Promise<AuthResult> {
    const response = await apiClient.post<AuthResult>('/auth/signup', data)
    return response.data
  },

  /** Swaps the one-time code from Google login for a normal login result. */
  async exchangeGoogleCode(code: string): Promise<AuthResult> {
    const response = await apiClient.post<AuthResult>('/auth/google/exchange', { code })
    return response.data
  },

  async me(): Promise<AuthUser> {
    const response = await apiClient.get<AuthUser>('/auth/me')
    return response.data
  },
}
