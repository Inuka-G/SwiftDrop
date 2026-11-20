import axios, { type InternalAxiosRequestConfig } from 'axios'
import { storageService } from '../services/storage.service'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5017/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
  // Lets the browser send and receive the HttpOnly refresh-token cookie.
  withCredentials: true,
})

/** Fired when the session cannot be renewed (refresh token expired or revoked), so the app can log out. */
export const SESSION_EXPIRED_EVENT = 'swiftdrop:session-expired'

// Requests that must never trigger an automatic refresh (they are the login and refresh calls themselves).
const NO_REFRESH_PATHS = ['/auth/login', '/auth/signup', '/auth/refresh', '/auth/logout', '/auth/google/exchange']

// Send the logged-in user's token with every request.
apiClient.interceptors.request.use((config) => {
  const token = storageService.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// One refresh at a time: if several requests fail together, they all wait for the same refresh.
let refreshing: Promise<string | null> | null = null

function refreshAccessToken(): Promise<string | null> {
  refreshing ??= apiClient
    .post<{ accessToken: string }>('/auth/refresh')
    .then((response) => {
      storageService.replaceAccessToken(response.data.accessToken)
      return response.data.accessToken
    })
    .catch(() => null)
    .finally(() => {
      refreshing = null
    })
  return refreshing
}

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

// When the access token has expired (401), quietly get a new one and repeat the request once.
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const config = error.config as RetriableConfig | undefined
    const canRefresh =
      axios.isAxiosError(error) &&
      error.response?.status === 401 &&
      config !== undefined &&
      !config._retried &&
      !!config.headers?.Authorization &&
      !NO_REFRESH_PATHS.some((path) => config.url?.endsWith(path))

    if (!canRefresh) return Promise.reject(error)

    config._retried = true
    const newToken = await refreshAccessToken()
    if (!newToken) {
      storageService.clearAccessToken()
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
      return Promise.reject(error)
    }

    config.headers.Authorization = `Bearer ${newToken}`
    return apiClient(config)
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
