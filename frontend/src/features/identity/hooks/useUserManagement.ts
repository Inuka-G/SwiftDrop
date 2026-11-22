import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { USER_MANAGEMENT_PAGE_SIZE } from '../../../lib/constants'
import { changeUserRole, changeUserStatus, getUserCounts, getUserDirectory } from '../api/userManagement.api'
import type { UserDirectoryFilters } from '../types/userManagement.types'

export function useUserDirectory(filters: UserDirectoryFilters, page: number, enabled = true) {
  return useQuery({
    queryKey: ['identity', 'users', filters, page],
    queryFn: () => getUserDirectory(filters, page, filters.pageSize ?? USER_MANAGEMENT_PAGE_SIZE),
    enabled,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}

export function useUserCounts() {
  return useQuery({ queryKey: ['identity', 'users', 'counts'], queryFn: getUserCounts, retry: 1 })
}

function useInvalidateUserManagement() {
  const client = useQueryClient()
  return () => Promise.all([
    client.invalidateQueries({ queryKey: ['identity', 'users'] }),
    client.invalidateQueries({ queryKey: ['merchants', 'pendingApprovals'] }),
  ])
}

export function useChangeUserRole() {
  const invalidate = useInvalidateUserManagement()
  return useMutation({ mutationFn: ({ id, role }: { id: string; role: string }) => changeUserRole(id, role), onSuccess: invalidate })
}

export function useChangeUserStatus() {
  const invalidate = useInvalidateUserManagement()
  return useMutation({ mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => changeUserStatus(id, isActive), onSuccess: invalidate })
}
