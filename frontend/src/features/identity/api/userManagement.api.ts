import { apiClient } from '../../../lib/axios'
import { USE_MOCKS } from '../../../lib/constants'
import type { UserDto, UserPageDto } from '../types/users.types'
import type { UserCounts, UserDirectoryFilters } from '../types/userManagement.types'
import { getUserCountsMock, getUsersMock, updateUserRoleMock, updateUserStatusMock } from './userManagement.mocks'

export async function getUserDirectory(filters: UserDirectoryFilters, page: number, pageSize = filters.pageSize ?? 10) {
  if (USE_MOCKS) return getUsersMock(filters, page, pageSize)
  const { data } = await apiClient.get<UserPageDto>('/users', { params: {
    search: filters.search || undefined, role: filters.role || undefined,
    isActive: filters.isActive === '' ? undefined : filters.isActive === 'true', page, pageSize,
  } })
  return data
}

export async function getUserCounts(): Promise<UserCounts> {
  if (USE_MOCKS) {
    return getUserCountsMock()
  }
  const [all, admins] = await Promise.allSettled([
    apiClient.get<UserPageDto>('/users', { params: { page: 1, pageSize: 1 } }),
    apiClient.get<UserPageDto>('/users', { params: { role: 'ADMIN', isActive: true, page: 1, pageSize: 1 } }),
  ])
  return { total: all.status === 'fulfilled' ? all.value.data.totalCount : null, activeAdmins: admins.status === 'fulfilled' ? admins.value.data.totalCount : null }
}

export async function changeUserRole(id: string, role: string): Promise<UserDto> {
  if (USE_MOCKS) return updateUserRoleMock(id, role)
  const { data } = await apiClient.patch<UserDto>(`/users/${id}/role`, { role })
  return data
}

export async function changeUserStatus(id: string, isActive: boolean): Promise<UserDto> {
  if (USE_MOCKS) return updateUserStatusMock(id, isActive)
  const { data } = await apiClient.patch<UserDto>(`/users/${id}/status`, { isActive })
  return data
}
