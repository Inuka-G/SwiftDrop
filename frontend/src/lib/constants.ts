export const APP_NAME = 'SwiftDrop'
export const AUTH_TOKEN_KEY = 'swiftdrop.accessToken'
export const DASHBOARD_REFRESH_INTERVAL_MS = 30_000
export const DASHBOARD_CHART_DAYS = 7
// In local development, show clearly labelled sample data unless live API mode is requested.
export const USE_MOCKS = import.meta.env.VITE_USE_MOCKS === 'true'
  || (import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS !== 'false')
