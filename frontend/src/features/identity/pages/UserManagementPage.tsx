import { useEffect, useMemo, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from 'react'
import axios from 'axios'
import { useQueryClient } from '@tanstack/react-query'
import { USE_MOCKS, USER_MANAGEMENT_FILTER_ROLES, USER_MANAGEMENT_PAGE_SIZE, USER_MANAGEMENT_ROLE_LABELS, USER_SEARCH_DEBOUNCE_MS, USER_TOAST_DURATION_MS } from '../../../lib/constants'
import { MerchantReviewDialog, usePendingMerchants } from '../../merchants'
import type { MerchantApprovalQueueItemDto } from '../../merchants'
import { useAuth } from '../useAuth'
import { useChangeUserStatus, useUserCounts, useUserDirectory } from '../hooks/useUserManagement'
import type { UserDto } from '../types/users.types'
import type { UserDirectoryFilters } from '../types/userManagement.types'
import { avatarTone, emailForMerchant, lastUsersPage, matchPendingMerchant, registrationDate, roleBadgeTone, roleIcon, roleLabel, userErrorMessage, userInitials, userRowRules, usersPageList, usersShowing } from '../utils/users.utils'
import { ChangeRoleDialog, SelfChangeBlockedDialog, SuspendUserDialog } from '../components/UserManagementDialogs'
import './UserManagementPage.css'

type Tab = 'all' | 'pending'
type DialogState = { kind: 'role' | 'suspend' | 'self'; user: UserDto } | { kind: 'merchant'; merchant: MerchantApprovalQueueItemDto; reject: boolean; contactEmail?: string | null }

function UserAvatar({ id, name }: { id: string; name: string }) { return <span className={`users-avatar ${avatarTone(id)}`} aria-hidden="true">{userInitials(name)}</span> }
function RoleBadge({ role }: { role: string }) { return <span className={`users-role-badge is-${roleBadgeTone(role)}`}><i aria-hidden="true">{roleIcon(role)}</i>{roleLabel(role)}</span> }
function StatusPill({ active, pending = false }: { active: boolean; pending?: boolean }) { return pending ? <span className="users-status is-pending"><i/>Pending Approval</span> : active ? <span className="users-status is-active"><i/>Active</span> : <span className="users-status is-suspended"><i/>Suspended</span> }
function SkeletonRows() { return <>{Array.from({ length: USER_MANAGEMENT_PAGE_SIZE }, (_, index) => <tr className="users-skeleton-row" key={index}><td colSpan={5}><i/></td></tr>)}</> }

function UserStats({ counts, pending }: { counts: ReturnType<typeof useUserCounts>; pending: ReturnType<typeof usePendingMerchants> }) {
  return <div className="users-stats-grid">
    <section className="users-stat-card" aria-busy={counts.isLoading}><span className="users-stat-icon is-blue" aria-hidden="true">♙</span><div><small>TOTAL ACCOUNTS</small>{counts.isLoading ? <i className="users-stat-skeleton"/> : <strong>{counts.data?.total?.toLocaleString() ?? '—'}</strong>}</div></section>
    <section className="users-stat-card" aria-busy={pending.isLoading}><span className="users-stat-icon is-amber" aria-hidden="true">◷</span><div><small>PENDING APPROVALS</small>{pending.isLoading ? <i className="users-stat-skeleton is-short"/> : <strong className="is-amber-text">{pending.isError ? '—' : `${pending.data?.length ?? 0} Pending`}</strong>}</div></section>
    <section className="users-stat-card" aria-busy={counts.isLoading}><span className="users-stat-icon is-shield" aria-hidden="true">⬡</span><div><small>ACTIVE ADMINS</small>{counts.isLoading ? <i className="users-stat-skeleton"/> : <strong>{counts.data?.activeAdmins == null ? '—' : `${counts.data.activeAdmins} Users`}</strong>}</div></section>
  </div>
}

function UsersToast({ toast, onClose }: { toast: { message: string; error: boolean } | null; onClose: () => void }) {
  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(onClose, USER_TOAST_DURATION_MS)
    return () => window.clearTimeout(timer)
  }, [toast, onClose])
  if (!toast) return null
  return <div className={`users-toast${toast.error ? ' is-error' : ''}`} role={toast.error ? 'alert' : 'status'}><span aria-hidden="true">{toast.error ? '!' : '✓'}</span><strong>{toast.message}</strong><button type="button" aria-label="Dismiss notification" onClick={onClose}>×</button></div>
}

export function UserManagementPage() {
  const { user: viewer } = useAuth()
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<Tab>('all')
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [role, setRole] = useState<UserDirectoryFilters['role']>('')
  const [status, setStatus] = useState<UserDirectoryFilters['isActive']>('')
  const [page, setPage] = useState(1)
  const [menuUserId, setMenuUserId] = useState<string | null>(null)
  const menuItemRef = useRef<HTMLButtonElement>(null)
  const [dialog, setDialog] = useState<DialogState | null>(null)
  const [dialogOpener, setDialogOpener] = useState<HTMLElement | null>(null)
  const [toast, setToast] = useState<{ message: string; error: boolean } | null>(null)
  const statusMutation = useChangeUserStatus()
  const pendingQuery = usePendingMerchants()
  const countsQuery = useUserCounts()
  const filters = useMemo<UserDirectoryFilters>(() => ({ search: debouncedSearch, role, isActive: status, pageSize: USER_MANAGEMENT_PAGE_SIZE }), [debouncedSearch, role, status])
  const usersQuery = useUserDirectory(filters, page)
  const merchantUsersFilters = useMemo<UserDirectoryFilters>(() => ({ search: '', role: 'MERCHANT', isActive: '', pageSize: 100 }), [])
  const merchantUsersQuery = useUserDirectory(merchantUsersFilters, 1, tab === 'pending')
  const rows = tab === 'all' ? usersQuery.data?.items ?? [] : [...(pendingQuery.data ?? [])].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
  const total = tab === 'all' ? usersQuery.data?.totalCount ?? 0 : pendingQuery.data?.length ?? 0
  const pageCount = Math.max(1, Math.ceil(total / USER_MANAGEMENT_PAGE_SIZE))
  const safePage = lastUsersPage(page, USER_MANAGEMENT_PAGE_SIZE, total)
  const visibleRows = tab === 'all' ? rows : rows.slice((safePage - 1) * USER_MANAGEMENT_PAGE_SIZE, safePage * USER_MANAGEMENT_PAGE_SIZE)
  const activeAdminCount = countsQuery.data?.activeAdmins ?? null
  const activeFilters = Boolean(search.trim() || role || status)
  const listQuery = tab === 'all' ? usersQuery : pendingQuery
  const pendingMerchants = pendingQuery.data ?? []

  useEffect(() => {
    const timer = window.setTimeout(() => { setDebouncedSearch(search.trim()); setPage(1) }, USER_SEARCH_DEBOUNCE_MS)
    return () => window.clearTimeout(timer)
  }, [search])
  useEffect(() => {
    if (page <= pageCount) return
    const timer = window.setTimeout(() => setPage(pageCount), 0)
    return () => window.clearTimeout(timer)
  }, [page, pageCount])
  useEffect(() => { if (menuUserId) menuItemRef.current?.focus() }, [menuUserId])

  function changeTab(value: Tab) { setTab(value); setPage(1); setMenuUserId(null) }
  function onTabKey(event: ReactKeyboardEvent<HTMLDivElement>) {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
    event.preventDefault()
    const next: Tab = event.key === 'Home' || event.key === 'ArrowLeft' ? 'all' : 'pending'
    changeTab(next)
    document.getElementById(`users-tab-${next}`)?.focus()
  }
  function refreshAll() { void Promise.all([queryClient.invalidateQueries({ queryKey: ['identity', 'users'] }), queryClient.invalidateQueries({ queryKey: ['merchants', 'pendingApprovals'] })]) }
  function openDialog(value: DialogState, opener: HTMLElement) { setDialog(value); setDialogOpener(opener); setMenuUserId(null) }
  function finishDialog(message: string) {
    if (message === 'self-change-blocked' && dialog && 'user' in dialog) { setDialog({ kind: 'self', user: dialog.user }); return }
    setDialog(null)
    setToast({ message, error: false })
  }
  async function reactivate(user: UserDto) {
    try { await statusMutation.mutateAsync({ id: user.id, isActive: true }); setToast({ message: 'Account reactivated', error: false }) }
    catch (failure) { const code = axios.isAxiosError(failure) ? failure.response?.status : undefined; if (code === 404) refreshAll(); setToast({ message: userErrorMessage(code, 'status'), error: true }) }
  }
  function clearFilters() { setSearch(''); setDebouncedSearch(''); setRole(''); setStatus(''); setPage(1) }
  function startMenuKey(event: ReactKeyboardEvent<HTMLButtonElement>, userId: string) {
    if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setMenuUserId(userId) }
  }

  const isLoading = tab === 'all' ? usersQuery.isLoading : pendingQuery.isLoading
  const isError = tab === 'all' ? usersQuery.isError : pendingQuery.isError
  const retry = () => { void (tab === 'all' ? usersQuery.refetch() : pendingQuery.refetch()) }
  return <main className="users-management-page">
    <div className="users-page-content" aria-hidden={Boolean(dialog)} inert={Boolean(dialog)}>
      <header className="users-page-header"><div><h1>User Management &amp; Role Permissions</h1><p>Manage user accounts, roles and merchant approvals.</p></div><UserStats counts={countsQuery} pending={pendingQuery}/></header>
      {USE_MOCKS && import.meta.env.DEV && <span className="users-sample-tag">Sample data</span>}
      <div className="users-tabs" role="tablist" aria-label="User management views" onKeyDown={onTabKey}>
        <button id="users-tab-all" role="tab" aria-selected={tab === 'all'} aria-controls="users-panel" tabIndex={tab === 'all' ? 0 : -1} className={tab === 'all' ? 'is-active' : ''} onClick={() => changeTab('all')}>All Accounts <span>{countsQuery.data?.total ?? '—'}</span></button>
        <button id="users-tab-pending" role="tab" aria-selected={tab === 'pending'} aria-controls="users-panel" tabIndex={tab === 'pending' ? 0 : -1} className={tab === 'pending' ? 'is-active is-pending-tab' : 'is-pending-tab'} onClick={() => changeTab('pending')}>Pending Approvals <span>{pendingQuery.isError ? '—' : `${pendingQuery.data?.length ?? 0} Pending`}</span></button>
      </div>
      {tab === 'all' && <section className="users-filter-bar" aria-label="Filter user accounts"><label className="users-search"><span aria-hidden="true">⌕</span><input type="search" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder="Search user by name or email address…" aria-label="Search user by name or email address" /></label><label><span className="users-filter-label">Role</span><select aria-label="Filter by role" value={role} onChange={(event) => { setRole(event.target.value as UserDirectoryFilters['role']); setPage(1) }}><option value="">All Roles</option>{USER_MANAGEMENT_FILTER_ROLES.map((value) => <option value={value} key={value}>{USER_MANAGEMENT_ROLE_LABELS[value]}</option>)}</select></label><label><span className="users-filter-label">Status</span><select aria-label="Filter by account status" value={status} onChange={(event) => { setStatus(event.target.value as UserDirectoryFilters['isActive']); setPage(1) }}><option value="">All Statuses</option><option value="true">Active</option><option value="false">Suspended</option></select></label></section>}
      <section className="users-table-card" id="users-panel" role="tabpanel" aria-labelledby={`users-tab-${tab}`} aria-busy={isLoading}>
        <div className="users-table-scroll"><table className="users-table"><thead><tr><th scope="col">User identity</th><th scope="col">System RBAC role</th><th scope="col">Account status</th><th scope="col">Registration date</th><th scope="col" className="users-actions-heading">Actions &amp; authority</th></tr></thead><tbody>
          {isLoading && <SkeletonRows/>}
          {isError && <tr><td colSpan={5}><div className="users-table-state" role="alert"><strong>{(axios.isAxiosError(listQuery.error) && listQuery.error.response?.status === 403) ? 'You do not have permission to manage users.' : 'We could not load this list.'}</strong><button type="button" onClick={retry}>Try again</button></div></td></tr>}
          {!isLoading && !isError && tab === 'all' && visibleRows.length === 0 && <tr><td colSpan={5}><div className="users-table-state"><strong>{activeFilters ? 'No users match your filters' : 'No users yet'}</strong>{activeFilters && <button type="button" onClick={clearFilters}>Clear filters</button>}</div></td></tr>}
          {!isLoading && !isError && tab === 'pending' && visibleRows.length === 0 && <tr><td colSpan={5}><div className="users-table-state"><strong>No merchant applications are waiting for approval.</strong></div></td></tr>}
          {!isLoading && !isError && tab === 'all' && (visibleRows as UserDto[]).map((user) => {
            const merchant = matchPendingMerchant(user.id, pendingMerchants)
            const rules = userRowRules(user, viewer?.id, viewer?.role, activeAdminCount, Boolean(merchant))
            const describedBy = rules.roleDisabledReason ? `users-role-reason-${user.id}` : undefined
            return <tr key={user.id}>
              <td><div className="users-person-cell"><UserAvatar id={user.id} name={user.fullName}/><span className="users-person-copy"><strong>{user.fullName}{rules.isSelf && <i className="users-you-tag">You (Session)</i>}</strong><small>{user.email}</small></span></div></td>
              <td><RoleBadge role={user.role}/></td><td><StatusPill active={user.isActive} pending={Boolean(merchant)}/></td><td>{registrationDate(user.createdAtUtc)}</td>
              <td><div className="users-row-actions">
                {merchant ? <><button type="button" className="users-approve-row" onClick={(event) => { setDialog({ kind: 'merchant', merchant, reject: false, contactEmail: user.email }); setDialogOpener(event.currentTarget); setMenuUserId(null) }}>✓ Review &amp; Approve</button><button type="button" className="users-reject-row" onClick={(event) => { setDialog({ kind: 'merchant', merchant, reject: true, contactEmail: user.email }); setDialogOpener(event.currentTarget); setMenuUserId(null) }}>⊘ Reject</button></> : user.isActive ? <>
                  <span className="users-disabled-wrap"><button type="button" className="users-outline-action" disabled={Boolean(rules.roleDisabledReason)} title={rules.roleDisabledReason ?? undefined} aria-describedby={describedBy} onClick={(event) => openDialog({ kind: 'role', user }, event.currentTarget)}>↔ Change Role</button>{rules.roleDisabledReason && <span className="users-sr-only" id={describedBy}>{rules.roleDisabledReason}</span>}</span>
                  {rules.showMenu && <div className="users-row-menu-wrap"><button id={`users-menu-trigger-${user.id}`} type="button" className="users-menu-trigger" aria-label={`More actions for ${user.fullName}`} aria-haspopup="menu" aria-expanded={menuUserId === user.id} onKeyDown={(event) => startMenuKey(event, user.id)} onClick={() => setMenuUserId(menuUserId === user.id ? null : user.id)}>⋮</button>{menuUserId === user.id && <div className="users-row-menu" role="menu" onKeyDown={(event) => { if (event.key === 'Escape') { setMenuUserId(null); document.getElementById(`users-menu-trigger-${user.id}`)?.focus() } else if (event.key === 'ArrowDown' || event.key === 'ArrowUp') { event.preventDefault(); menuItemRef.current?.focus() } }}><button type="button" role="menuitem" ref={menuItemRef} disabled={Boolean(rules.statusDisabledReason)} aria-label={rules.statusDisabledReason ? `Suspend account. ${rules.statusDisabledReason}` : 'Suspend account'} title={rules.statusDisabledReason ?? undefined} onClick={(event) => { setMenuUserId(null); openDialog({ kind: 'suspend', user }, event.currentTarget) }}>Suspend account</button></div>}</div>}
                </> : <><button type="button" className="users-outline-action" disabled={statusMutation.isPending} onClick={() => void reactivate(user)}>⟳ Reactivate</button>{rules.showMenu && <div className="users-row-menu-wrap"><button id={`users-menu-trigger-${user.id}`} type="button" className="users-menu-trigger" aria-label={`More actions for ${user.fullName}`} aria-haspopup="menu" aria-expanded={menuUserId === user.id} onKeyDown={(event) => startMenuKey(event, user.id)} onClick={() => setMenuUserId(menuUserId === user.id ? null : user.id)}>⋮</button>{menuUserId === user.id && <div className="users-row-menu" role="menu" onKeyDown={(event) => { if (event.key === 'Escape') { setMenuUserId(null); document.getElementById(`users-menu-trigger-${user.id}`)?.focus() } else if (event.key === 'ArrowDown' || event.key === 'ArrowUp') { event.preventDefault(); menuItemRef.current?.focus() } }}><button type="button" role="menuitem" ref={menuItemRef} disabled={statusMutation.isPending} onClick={() => { setMenuUserId(null); void reactivate(user) }}>Reactivate account</button></div>}</div>}</>}
              </div></td>
            </tr>
          })}
          {!isLoading && !isError && tab === 'pending' && (visibleRows as MerchantApprovalQueueItemDto[]).map((merchant) => <tr key={merchant.id}>
            <td><div className="users-person-cell"><UserAvatar id={merchant.id} name={merchant.businessName}/><span className="users-person-copy"><strong>{merchant.businessName}</strong><small>{emailForMerchant(merchant, merchantUsersQuery.data?.items ?? []) ?? 'Contact email unavailable'}</small></span></div></td>
            <td><RoleBadge role="MERCHANT"/></td><td><StatusPill active={false} pending/></td><td>{registrationDate(merchant.createdAtUtc)}</td>
            <td><div className="users-row-actions"><button type="button" className="users-approve-row" onClick={(event) => openDialog({ kind: 'merchant', merchant, reject: false }, event.currentTarget)}>✓ Review &amp; Approve</button><button type="button" className="users-reject-row" onClick={(event) => openDialog({ kind: 'merchant', merchant, reject: true }, event.currentTarget)}>⊘ Reject</button></div></td>
          </tr>)}
        </tbody></table></div>
        {!isLoading && !isError && total > 0 && <footer className="users-table-footer"><span>{tab === 'all' ? usersShowing(safePage, USER_MANAGEMENT_PAGE_SIZE, total) : `Showing ${(safePage - 1) * USER_MANAGEMENT_PAGE_SIZE + 1} - ${Math.min(safePage * USER_MANAGEMENT_PAGE_SIZE, total)} of ${total} pending applications`}</span><nav aria-label="Pagination"><button type="button" disabled={safePage <= 1} onClick={() => setPage(Math.max(1, safePage - 1))}>Previous</button>{usersPageList(safePage, pageCount).map((number, index) => number === 'ellipsis' ? <span className="users-page-ellipsis" key={`ellipsis-${index}`}>…</span> : <button type="button" key={number} className={number === safePage ? 'is-current' : ''} aria-current={number === safePage ? 'page' : undefined} onClick={() => setPage(number)}>{number}</button>)}<button type="button" disabled={safePage >= pageCount} onClick={() => setPage(Math.min(pageCount, safePage + 1))}>Next</button></nav></footer>}
      </section>
    </div>
    {dialog?.kind === 'role' && <ChangeRoleDialog user={dialog.user} opener={dialogOpener} onClose={() => setDialog(null)} onDone={finishDialog} onRefresh={refreshAll} currentUserId={viewer?.id} activeAdmins={activeAdminCount}/>}
    {dialog?.kind === 'suspend' && <SuspendUserDialog user={dialog.user} opener={dialogOpener} onClose={() => setDialog(null)} onDone={finishDialog} onRefresh={refreshAll} currentUserId={viewer?.id} activeAdmins={activeAdminCount}/>}
    {dialog?.kind === 'self' && <SelfChangeBlockedDialog user={dialog.user} opener={dialogOpener} onClose={() => setDialog(null)}/>}
    {dialog?.kind === 'merchant' && <MerchantReviewDialog merchant={dialog.merchant} contactEmail={dialog.contactEmail ?? emailForMerchant(dialog.merchant, merchantUsersQuery.data?.items ?? [])} initialReject={dialog.reject} opener={dialogOpener} onClose={() => setDialog(null)} onDone={finishDialog} onRefresh={refreshAll}/>}
    <UsersToast toast={toast} onClose={() => setToast(null)}/>
  </main>
}
