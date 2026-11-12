export const APP_NAME = 'SwiftDrop'
export const AUTH_TOKEN_KEY = 'swiftdrop.accessToken'
export const AUTH_USER_KEY = 'swiftdrop.user'

/** Role names as they appear in the JWT "role" claim. */
export const ROLES = {
  customer: 'CUSTOMER',
  merchant: 'MERCHANT',
  courier: 'COURIER',
  dispatcher: 'DISPATCHER',
  admin: 'ADMIN',
} as const

export type Role = (typeof ROLES)[keyof typeof ROLES]
