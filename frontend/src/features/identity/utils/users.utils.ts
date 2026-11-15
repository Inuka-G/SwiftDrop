import { USER_MANAGEMENT_ROLE_LABELS, USER_MANAGEMENT_ROLE_OPTIONS } from '../../../lib/constants'
import type { MerchantApprovalQueueItemDto } from '../../merchants'
import type { UserDto } from '../types/users.types'

export const USER_ROLE_LABELS = USER_MANAGEMENT_ROLE_LABELS
export const USER_ROLE_ICONS: Record<string, string> = { ADMIN: '◯', DISPATCHER: '◉', MERCHANT: '▤', COURIER: 'ϟ', CUSTOMER: '♙' }

export function userInitials(name: string) { return name.trim().split(/\s+/).slice(0, 2).map((part) => part[0] ?? '').join('').toUpperCase() || '?' }
export function avatarTone(userId: string) { let hash = 0; for (const char of userId) hash = (hash * 31 + char.charCodeAt(0)) >>> 0; return `tone-${hash % 6}` }
export function roleLabel(role: string) { return USER_ROLE_LABELS[role.toUpperCase()] ?? role }
export function roleIcon(role: string) { return USER_ROLE_ICONS[role.toUpperCase()] ?? '•' }
export function roleBadgeTone(role: string) { return ['ADMIN', 'DISPATCHER'].includes(role.toUpperCase()) ? 'admin' : ['MERCHANT', 'COURIER', 'CUSTOMER'].includes(role.toUpperCase()) ? role.toLowerCase() : 'unknown' }

export interface UserRowRules {
  isSelf: boolean
  roleDisabledReason: string | null
  statusDisabledReason: string | null
  showMenu: boolean
}

export function userRowRules(user: UserDto, currentUserId: string | undefined, viewerRole: string | undefined, activeAdminCount: number | null, pendingMerchant: boolean): UserRowRules {
  const isSelf = Boolean(currentUserId && user.id === currentUserId)
  const isAdmin = user.role.toUpperCase() === 'ADMIN' && user.isActive
  const lastAdmin = isAdmin && activeAdminCount != null && activeAdminCount <= 1
  const unknownAdminCount = isAdmin && activeAdminCount == null
  const roleDisabledReason = isSelf ? 'You cannot change your own account'
    : pendingMerchant ? 'Merchant accounts are managed through merchant approval'
      : user.role.toUpperCase() === 'MERCHANT' ? 'Merchant accounts are managed through merchant approval'
        : viewerRole?.toUpperCase() !== 'ADMIN' ? 'Only administrators can change roles'
      : lastAdmin ? 'At least one active administrator is required' : unknownAdminCount ? 'Unable to verify the active administrator count' : null
  const statusDisabledReason = isSelf ? 'You cannot change your own account' : lastAdmin ? 'At least one active administrator is required' : unknownAdminCount ? 'Unable to verify the active administrator count' : null
  return { isSelf, roleDisabledReason, statusDisabledReason, showMenu: !isSelf }
}

export function roleOptions(currentRole: string) {
  const options: Array<{ value: string; label: string; disabled: boolean; reason: string }> = USER_MANAGEMENT_ROLE_OPTIONS.map((option) => ({ ...option, disabled: option.value === currentRole || option.value === 'MERCHANT', reason: option.value === currentRole ? 'This is the current role' : option.value === 'MERCHANT' ? 'Managed through merchant approval' : '' }))
  if (currentRole === 'DISPATCHER') options.splice(1, 0, { value: 'DISPATCHER', label: 'Dispatcher', disabled: true, reason: 'Current role' })
  return options
}

export function canConfirmRole(currentRole: string, selectedRole: string | null, isLastActiveAdmin: boolean) { return Boolean(selectedRole && selectedRole !== currentRole && !(currentRole === 'ADMIN' && isLastActiveAdmin)) }

export function roleChangeNotice(oldRole: string, newRole: string) {
  const sentences = [`Changing this user from ${roleLabel(oldRole)} to ${roleLabel(newRole)} changes what they can access.`]
  if (newRole === 'ADMIN' && oldRole !== 'ADMIN') sentences.push('Promoting to Admin gives full administrator access.')
  if (oldRole === 'ADMIN' && newRole !== 'ADMIN') sentences.push('This removes administrator access.')
  sentences.push('Existing sessions may keep the old role until they expire or the user signs in again.')
  return sentences.join(' ')
}

export function registrationDate(value: string, now = new Date()) {
  const date = new Date(value)
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const yesterday = new Date(today); yesterday.setDate(yesterday.getDate() - 1)
  const dateDay = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const time = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', hour12: false }).format(date)
  if (dateDay.getTime() === today.getTime()) return `Today, ${time}`
  if (dateDay.getTime() === yesterday.getTime()) return `Yesterday, ${time}`
  return new Intl.DateTimeFormat(undefined, { month: 'short', day: '2-digit', year: 'numeric' }).format(date)
}

export function usersPageList(page: number, pageCount: number): Array<number | 'ellipsis'> {
  if (pageCount <= 5) return Array.from({ length: pageCount }, (_, index) => index + 1)
  if (page <= 3) return [1, 2, 3, 4, 'ellipsis', pageCount]
  if (page >= pageCount - 2) return [1, 'ellipsis', pageCount - 3, pageCount - 2, pageCount - 1, pageCount]
  return [1, 'ellipsis', page - 1, page, page + 1, 'ellipsis', pageCount]
}

export function usersShowing(page: number, pageSize: number, total: number) { return total ? `Showing ${Math.min((page - 1) * pageSize + 1, total)} - ${Math.min(page * pageSize, total)} of ${total} registered users` : 'Showing 0 registered users' }
export function buildUsersQuery(filters: { search: string; role: string; isActive: string }, page: number, pageSize: number) { return { search: filters.search || undefined, role: filters.role || undefined, isActive: filters.isActive === '' ? undefined : filters.isActive === 'true', page, pageSize } }
export function lastUsersPage(page: number, pageSize: number, total: number) { return Math.max(1, Math.min(page, Math.ceil(total / pageSize) || 1)) }
export function userErrorMessage(status: number | undefined, action: 'role' | 'status' | 'merchant') {
  if (status === 400) return action === 'merchant' ? 'This application was already reviewed.' : "This change isn't allowed for this account."
  if (status === 403) return action === 'role' ? 'Only administrators can change roles.' : action === 'merchant' ? 'Only administrators can review merchant applications.' : 'You do not have permission to change account status.'
  if (status === 404) return action === 'merchant' ? 'This application no longer exists.' : 'This user no longer exists.'
  if (status === 409) return action === 'merchant' ? 'This application was already reviewed.' : 'At least one active administrator is required.'
  return action === 'merchant' ? "Couldn't complete the review. Try again." : "Couldn't complete the request. Try again."
}
export function matchPendingMerchant(userId: string | undefined, pending: MerchantApprovalQueueItemDto[]) { return userId ? pending.find((merchant) => merchant.userId === userId) : undefined }
export function emailForMerchant(merchant: MerchantApprovalQueueItemDto, users: UserDto[]) { return merchant.userId ? users.find((user) => user.id === merchant.userId)?.email ?? null : null }
