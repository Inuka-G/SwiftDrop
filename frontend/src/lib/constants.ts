export const APP_NAME = 'SwiftDrop'
export const AUTH_TOKEN_KEY = 'swiftdrop.accessToken'
export const DASHBOARD_REFRESH_INTERVAL_MS = 30_000
export const DASHBOARD_CHART_DAYS = 7
export const DISPATCH_OVERDUE_MINUTES = 90
export const DISPATCH_REFRESH_INTERVAL_MS = 30_000
export const DISPATCH_PAGE_SIZES = [10, 25, 50] as const
export const DISPATCH_TOAST_DURATION_MS = 5_000
export const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === 'true'

