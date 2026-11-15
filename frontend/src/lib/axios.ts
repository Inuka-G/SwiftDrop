import axios from 'axios'
import { storageService } from '../services/storage.service'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5017/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
})

/** Fired when the server rejects the saved token (expired or invalid), so the app can log out. */
export const SESSION_EXPIRED_EVENT = 'swiftdrop:session-expired'

// Send the logged-in user's token with every request.
apiClient.interceptors.request.use((config) => {
  const token = storageService.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401 && error.config?.headers?.Authorization) {
      storageService.clearAccessToken()
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
    }
    return Promise.reject(error)
  },
)

/** Turns any API error into one readable sentence, using the backend's ProblemDetails format. */
export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!axios.isAxiosError(error)) return fallback
  if (!error.response) return 'Cannot reach the server. Check that the backend is running.'

  const data = error.response.data as { detail?: string; title?: string; errors?: Record<string, string[]> } | undefined
  const firstValidationError = data?.errors ? Object.values(data.errors).flat()[0] : undefined
  return data?.detail ?? firstValidationError ?? data?.title ?? fallback
}
