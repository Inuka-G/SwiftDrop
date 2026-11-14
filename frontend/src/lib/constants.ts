export const APP_NAME = 'SwiftDrop'
export const AUTH_TOKEN_KEY = 'swiftdrop.accessToken'

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
