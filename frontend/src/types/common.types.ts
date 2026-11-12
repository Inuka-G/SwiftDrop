import type { Role } from '../lib/constants'

/** RFC 7807 ProblemDetails, as returned by the backend's error middleware. */
export interface ApiError {
  title: string
  status: number
  detail?: string
}

export type AsyncStatus = 'idle' | 'loading' | 'success' | 'error'

export interface AuthUser {
  id: string
  fullName: string
  email: string
  username?: string | null
  role: Role
}

export interface AuthResult {
  accessToken: string
  expiresAtUtc: string
  user: AuthUser
}
