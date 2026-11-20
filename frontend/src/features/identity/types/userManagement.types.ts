import type { UserDto, UserPageDto } from './users.types'

export type UserRoleFilter = '' | 'ADMIN' | 'DISPATCHER' | 'COURIER' | 'MERCHANT' | 'CUSTOMER'
export type UserStatusFilter = '' | 'true' | 'false'

export interface UserDirectoryFilters {
  search: string
  role: UserRoleFilter
  isActive: UserStatusFilter
  pageSize?: number
}

export interface UserCounts {
  total: number | null
  activeAdmins: number | null
}

export interface ChangeUserRoleRequest { role: string }
export interface ChangeUserStatusRequest { isActive: boolean }

export type ManagedUser = UserDto
export type ManagedUserPage = UserPageDto
