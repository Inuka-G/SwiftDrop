import axios, { isAxiosError } from 'axios'
import { storageService } from '../services/storage.service'
import type { ApiError } from '../types/common.types'

// No default Content-Type: axios picks JSON for objects and multipart for FormData.
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
})

apiClient.interceptors.request.use((config) => {
  const token = storageService.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

apiClient.interceptors.response.use(undefined, (error) => {
  // An expired or invalid token: forget it so the app sends the user back to sign in.
  if (isAxiosError(error) && error.response?.status === 401) {
    storageService.clearSession()
  }
  return Promise.reject(error)
})

/** The server's ProblemDetails message, or a generic one. */
export function getErrorMessage(error: unknown): string {
  if (isAxiosError<ApiError>(error)) {
    const problem = error.response?.data
    if (problem?.detail) return problem.detail
    if (problem?.title) return problem.title
    if (!error.response) return 'Cannot reach the server. Check your connection.'
  }
  return 'Something went wrong. Please try again.'
}
