export const APP_NAME = 'SwiftDrop'
export const AUTH_TOKEN_KEY = 'swiftdrop.accessToken'

export const DASHBOARD_REFRESH_INTERVAL_MS = 30_000
export const DASHBOARD_CHART_DAYS = 7
export const REPORT_MAX_RANGE_DAYS = 366
export const REPORT_TABLE_PAGE_SIZE = 10
export const REPORT_PRESETS = ['last7', 'last30', 'thisMonth'] as const
export const DISPATCH_OVERDUE_MINUTES = 90
export const DISPATCH_REFRESH_INTERVAL_MS = 30_000
export const DISPATCH_PAGE_SIZES = [10, 25, 50] as const
export const DISPATCH_TOAST_DURATION_MS = 5_000
export const COURIER_VEHICLE_TYPES = ['Motorbike', 'Three-Wheeler', 'Van', 'Light Truck'] as const
export const COURIER_LICENSE_PLATE_MAX_LENGTH = 20
export const COURIER_SEARCH_DEBOUNCE_MS = 250
export const COURIER_USER_SEARCH_DEBOUNCE_MS = 300
export const USER_MANAGEMENT_PAGE_SIZE = 10
export const USER_SEARCH_DEBOUNCE_MS = 300
export const USER_TOAST_DURATION_MS = 5_000
export const USER_MANAGEMENT_ROLE_LABELS: Record<string, string> = {
  ADMIN: 'Admin', DISPATCHER: 'Dispatcher', COURIER: 'Courier', MERCHANT: 'Merchant', CUSTOMER: 'Customer',
}
export const USER_MANAGEMENT_FILTER_ROLES = ['ADMIN', 'DISPATCHER', 'COURIER', 'MERCHANT', 'CUSTOMER'] as const
export const USER_MANAGEMENT_ROLE_OPTIONS = [
  { value: 'ADMIN', label: 'Admin' },
  { value: 'COURIER', label: 'Courier' },
  { value: 'MERCHANT', label: 'Merchant' },
  { value: 'CUSTOMER', label: 'Customer' },
] as const
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
  [ROLES.admin]: '/admin',
  [ROLES.dispatcher]: '/admin/dispatch',
}

export function homePathForRole(role: string): string {
  return ROLE_HOME[role] ?? '/'
}

