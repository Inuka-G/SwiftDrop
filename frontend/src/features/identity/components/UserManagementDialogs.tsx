import { useEffect, useRef, useState, type PropsWithChildren } from 'react'
import { createPortal } from 'react-dom'
import { ROLES } from '../../../lib/constants'
import { useChangeUserRole, useChangeUserStatus } from '../hooks/useUserManagement'
import type { UserDto } from '../types/users.types'
import { avatarTone, canConfirmRole, roleBadgeTone, roleChangeNotice, roleIcon, roleOptions, userErrorMessage } from '../utils/users.utils'

type DialogProps = { user: UserDto; opener: HTMLElement | null; onClose: () => void; onDone: (message: string) => void; onRefresh: () => void; currentUserId?: string; activeAdmins: number | null }

function statusCode(error: unknown) { return typeof error === 'object' && error !== null && 'response' in error ? (error as { response?: { status?: number } }).response?.status : undefined }

function DialogFrame({ title, opener, onClose, pending = false, children, tone = '' }: PropsWithChildren<{ title: string; opener: HTMLElement | null; onClose: () => void; pending?: boolean; tone?: string }>) {
  const ref = useRef<HTMLElement>(null)
  const latest = useRef({ onClose, opener, pending })
  useEffect(() => { latest.current = { onClose, opener, pending } }, [onClose, opener, pending])
  useEffect(() => {
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const focusFirst = () => ref.current?.querySelector<HTMLElement>('button:not(:disabled), input:not(:disabled), [tabindex="0"]')?.focus()
    const timer = window.setTimeout(focusFirst, 0)
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !latest.current.pending) { event.stopPropagation(); latest.current.onClose(); return }
      if (event.key !== 'Tab' || !ref.current) return
      const nodes = [...ref.current.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), [tabindex="0"]')]
      if (!nodes.length) return
      const first = nodes[0], last = nodes.at(-1)!
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => { window.clearTimeout(timer); document.removeEventListener('keydown', onKeyDown); document.body.style.overflow = previousOverflow; latest.current.opener?.focus() }
  }, [])

  const content = <div className="users-modal-overlay" onMouseDown={(event) => { if (event.target === event.currentTarget && !pending) onClose() }}>
    <section className={`users-dialog ${tone}`} role="dialog" aria-modal="true" aria-labelledby="users-dialog-title" ref={ref}>
      <header className="users-dialog-header"><span className="users-dialog-mark" aria-hidden="true">{tone === 'is-warning' ? '⚠' : '↻'}</span><h2 id="users-dialog-title">{title}</h2><button type="button" aria-label="Close dialog" disabled={pending} onClick={onClose}>×</button></header>
      <div className="users-dialog-content">{children}</div>
    </section>
  </div>
  const host = document.querySelector('.admin-shell') ?? document.body
  return createPortal(content, host)
}

function UserIdentityCard({ user, tag }: { user: UserDto; tag?: string }) {
  const initials = user.fullName.trim().split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toUpperCase()
  return <div className="users-identity-card"><span className={`users-avatar ${avatarTone(user.id)}`}>{initials}</span><span className="users-identity-copy"><strong>{user.fullName}</strong><small>{user.email}</small></span>{tag && <span className={`users-current-tag role-${roleBadgeTone(user.role)}`}>{roleIcon(user.role)} {tag}</span>}</div>
}

export function ChangeRoleDialog({ user, opener, onClose, onDone, onRefresh, currentUserId, activeAdmins }: DialogProps) {
  const mutation = useChangeUserRole()
  const [role, setRole] = useState<string | null>(null)
  const [error, setError] = useState('')
  const isLastActiveAdmin = user.role === ROLES.admin && user.isActive && (activeAdmins == null || activeAdmins <= 1)
  const options = roleOptions(user.role)
  const close = () => { if (!mutation.isPending) onClose() }
  async function submit() {
    if (!role || !canConfirmRole(user.role, role, isLastActiveAdmin) || mutation.isPending) return
    setError('')
    try { await mutation.mutateAsync({ id: user.id, role }); onDone('Role updated') }
    catch (failure) {
      const status = statusCode(failure)
      if (status === 404) onRefresh()
      if (status === 400 && user.id === currentUserId) { onDone('self-change-blocked'); return }
      setError(userErrorMessage(status, 'role'))
    }
  }
  return <DialogFrame title="Confirm Role Change" opener={opener} onClose={close} pending={mutation.isPending}>
    <UserIdentityCard user={user} tag={`CURRENT: ${user.role}`} />
    <fieldset className="users-role-picker"><legend>SELECT NEW SYSTEM RBAC ROLE</legend><div className="users-role-options" role="radiogroup" aria-label="Select new system role" onKeyDown={(event) => {
      const enabled = options.filter((option) => !option.disabled)
      if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key) || !enabled.length) return
      event.preventDefault()
      const at = enabled.findIndex((option) => option.value === role)
      const backwards = event.key === 'ArrowLeft' || event.key === 'ArrowUp'
      const next = at < 0 ? backwards ? enabled.length - 1 : 0 : (at + (backwards ? enabled.length - 1 : 1)) % enabled.length
      setRole(enabled[next].value)
      document.getElementById(`users-role-${enabled[next].value}`)?.focus()
    }}>{options.map((option) => <button type="button" role="radio" aria-checked={role === option.value} aria-disabled={option.disabled} disabled={option.disabled || mutation.isPending} id={`users-role-${option.value}`} key={option.value} className={`users-role-option${role === option.value ? ' is-selected' : ''}${option.disabled ? ' is-disabled' : ''}`} title={option.reason || undefined} onClick={() => { setRole(option.value); setError('') }}>
      <span className="users-radio-dot" aria-hidden="true"/><span><strong>{option.label}{option.value === user.role ? ' · Current' : ''}</strong><small>{option.value === ROLES.admin ? 'Full access to dispatch, users and reports' : option.value === ROLES.courier ? 'Delivers assigned parcels' : option.value === ROLES.merchant ? option.reason : option.value === ROLES.dispatcher ? option.reason : 'Books and tracks parcels'}</small></span>
    </button>)}</div></fieldset>
    {role && <div className="users-security-note" role="note"><strong>⚠ Security &amp; Authority Notice</strong><p>{roleChangeNotice(user.role, role)}</p></div>}
    {role && role !== ROLES.admin && user.role === ROLES.admin && user.isActive && (activeAdmins == null || activeAdmins <= 1) && <div className="users-inline-error" role="alert">{activeAdmins == null ? 'Unable to verify the active administrator count.' : 'At least one active administrator is required.'}</div>}
    {error && <div className="users-inline-error" role="alert">{error}</div>}
    <footer className="users-dialog-footer"><button type="button" className="users-button users-button-muted" disabled={mutation.isPending} onClick={close}>Cancel</button><button type="button" className="users-button users-button-primary" disabled={!role || !canConfirmRole(user.role, role, isLastActiveAdmin) || mutation.isPending} onClick={() => void submit()}>{mutation.isPending ? <><i className="users-spinner"/>Saving…</> : '✓ Confirm & Update Role'}</button></footer>
  </DialogFrame>
}

export function SuspendUserDialog({ user, opener, onClose, onDone, onRefresh, activeAdmins }: DialogProps) {
  const mutation = useChangeUserStatus()
  const [error, setError] = useState('')
  const close = () => { if (!mutation.isPending) onClose() }
  async function submit() {
    if (mutation.isPending) return
    setError('')
    try { await mutation.mutateAsync({ id: user.id, isActive: false }); onDone('Account suspended') }
    catch (failure) { const status = statusCode(failure); if (status === 404) onRefresh(); setError(userErrorMessage(status, 'status')) }
  }
  return <DialogFrame title="Suspend account" opener={opener} onClose={close} pending={mutation.isPending}>
    <UserIdentityCard user={user} tag={user.role}/><p className="users-confirm-copy">{user.fullName} will no longer be able to sign in. You can reactivate the account later.</p>
    {user.role === ROLES.admin && user.isActive && (activeAdmins == null || activeAdmins <= 1) && <div className="users-inline-error" role="alert">{activeAdmins == null ? 'Unable to verify the active administrator count.' : 'At least one active administrator is required.'}</div>}{error && <div className="users-inline-error" role="alert">{error}</div>}
    <footer className="users-dialog-footer"><button type="button" className="users-button users-button-muted" disabled={mutation.isPending} onClick={close}>Cancel</button><button type="button" className="users-button users-button-danger" disabled={mutation.isPending || (user.role === ROLES.admin && user.isActive && (activeAdmins == null || activeAdmins <= 1))} onClick={() => void submit()}>{mutation.isPending ? <><i className="users-spinner"/>Suspending…</> : 'Suspend account'}</button></footer>
  </DialogFrame>
}

export function SelfChangeBlockedDialog({ user, opener, onClose }: Pick<DialogProps, 'user' | 'opener' | 'onClose'>) {
  return <DialogFrame title="Self-change not allowed" opener={opener} onClose={onClose} tone="is-warning"><p className="users-warning-subtitle">You cannot change your own role or status</p><UserIdentityCard user={user} tag="YOU · CURRENT SESSION"/><p className="users-confirm-copy">Ask another administrator to make this change.</p><footer className="users-dialog-footer"><button type="button" className="users-button users-button-primary" onClick={onClose}>Close</button></footer></DialogFrame>
}

