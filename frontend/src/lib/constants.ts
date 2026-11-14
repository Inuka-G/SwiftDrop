export const APP_NAME = 'SwiftDrop'
export const AUTH_TOKEN_KEY = 'swiftdrop.accessToken'

export const DASHBOARD_REFRESH_INTERVAL_MS = 30_000
export const DASHBOARD_CHART_DAYS = 7
export const DISPATCH_OVERDUE_MINUTES = 90
export const DISPATCH_REFRESH_INTERVAL_MS = 30_000
export const DISPATCH_PAGE_SIZES = [10, 25, 50] as const
export const DISPATCH_TOAST_DURATION_MS = 5_000
export const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === 'true'



/** Where the backend runs. Used for browser redirects that must go to the backend itself, such as Google login. */
export const BACKEND_ORIGIN = import.meta.env.VITE_BACKEND_ORIGIN ?? 'http://localhost:5017'

/** Opening this in the browser starts "Login with Google". The backend later redirects to /auth/google/callback. */
export const GOOGLE_LOGIN_URL = `${BACKEND_ORIGIN}/api/v1/auth/google/login`

export const ROLES = {
  customer: 'CUSTOMER',
  merchant: 'MERCHANT',
  courier: 'COURIER',
  dispatcher: 'DISPATCHER',
  admin: 'ADMIN',
} as const

/** The page each role lands on after logging in. Roles without their own dashboard yet go to the home page. */
export const ROLE_HOME: Record<string, string> = {
  [ROLES.courier]: '/courier',
}

export function homePathForRole(role: string): string {
  return ROLE_HOME[role] ?? '/'
}

