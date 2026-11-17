import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useApproveMerchant, useRejectMerchant } from '../hooks/useMerchantApprovals'
import type { MerchantApprovalQueueItemDto } from '../types/merchantApprovals.types'
import './merchantApprovals.css'

function errorStatus(error: unknown) { return typeof error === 'object' && error !== null && 'response' in error ? (error as { response?: { status?: number } }).response?.status : undefined }
function localDate(value: string) { return new Intl.DateTimeFormat(undefined, { month: 'short', day: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value)) }

export function MerchantReviewDialog({ merchant, contactEmail, initialReject = false, opener, onClose, onDone, onRefresh }: {
  merchant: MerchantApprovalQueueItemDto
  contactEmail?: string | null
  initialReject?: boolean
  opener: HTMLElement | null
  onClose: () => void
  onDone: (message: string) => void
  onRefresh: () => void
}) {
  const [confirmReject, setConfirmReject] = useState(initialReject)
  const [error, setError] = useState('')
  const approve = useApproveMerchant()
  const reject = useRejectMerchant()
  const pending = approve.isPending || reject.isPending
  const dialog = useRef<HTMLElement>(null)
  const latest = useRef({ onClose, opener, pending })
  useEffect(() => { latest.current = { onClose, opener, pending } }, [onClose, opener, pending])
  useEffect(() => {
    const oldOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const timer = window.setTimeout(() => dialog.current?.querySelector<HTMLElement>('button:not(:disabled)')?.focus(), 0)
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !latest.current.pending) latest.current.onClose()
      if (event.key !== 'Tab' || !dialog.current) return
      const buttons = [...dialog.current.querySelectorAll<HTMLElement>('button:not(:disabled)')]
      if (!buttons.length) return
      if (event.shiftKey && document.activeElement === buttons[0]) { event.preventDefault(); buttons.at(-1)?.focus() }
      else if (!event.shiftKey && document.activeElement === buttons.at(-1)) { event.preventDefault(); buttons[0].focus() }
    }
    document.addEventListener('keydown', onKey)
    return () => { window.clearTimeout(timer); document.removeEventListener('keydown', onKey); document.body.style.overflow = oldOverflow; latest.current.opener?.focus() }
  }, [])

  async function submit(decision: 'approve' | 'reject') {
    setError('')
    try {
      if (decision === 'approve') await approve.mutateAsync(merchant.id)
      else await reject.mutateAsync(merchant.id)
      onDone(decision === 'approve' ? 'Merchant approved' : 'Merchant rejected')
    } catch (failure) {
      const status = errorStatus(failure)
      if ([400, 404, 409].includes(status ?? 0)) onRefresh()
      setError(status === 404 ? 'This application no longer exists.' : status === 409 || status === 400 ? 'This application was already reviewed.' : status === 403 ? 'Only administrators can review merchant applications.' : "Couldn't complete the review. Try again.")
    }
  }

  const content = <div className="merchant-review-overlay" onMouseDown={(event) => { if (event.target === event.currentTarget && !pending) onClose() }}>
    <section className="merchant-review-dialog" role="dialog" aria-modal="true" aria-labelledby="merchant-review-title" ref={dialog}>
      <header><span className="merchant-review-icon" aria-hidden="true">▤</span><span><h2 id="merchant-review-title">Review Merchant Application</h2><p>Verify the business details, then approve or reject the account.</p></span><span className="merchant-pending-tag">● Pending Approval</span><button type="button" aria-label="Close dialog" disabled={pending} onClick={onClose}>×</button></header>
      <div className="merchant-review-body"><section className="merchant-review-card"><div className="merchant-review-business"><span className="merchant-review-avatar">{merchant.businessName.split(/\s+/).slice(0, 2).map((word) => word[0]).join('').toUpperCase()}</span><span><strong>{merchant.businessName}</strong><small>Registration No: {merchant.businessRegNumber || '—'}</small></span><span><small>Submitted</small><strong>{localDate(merchant.createdAtUtc)}</strong></span></div><div className="merchant-review-details"><div><b>Contact email</b><span>{contactEmail || 'Not provided by the approval record'}</span></div></div></section>
        {confirmReject ? <div className="merchant-reject-confirm" role="alert"><strong>Reject this application?</strong><p>The applicant will not be able to use the merchant API.</p></div> : null}
        {error && <div className="merchant-review-error" role="alert">{error}</div>}
      </div>
      <footer>{confirmReject ? <><button type="button" className="merchant-review-cancel" disabled={pending} onClick={() => setConfirmReject(false)}>Back</button><button type="button" className="merchant-review-danger" disabled={pending} onClick={() => void submit('reject')}>{pending ? <><i className="merchant-review-spinner"/>Rejecting…</> : 'Confirm rejection'}</button></> : <><button type="button" className="merchant-review-outline-danger" disabled={pending} onClick={() => setConfirmReject(true)}>⊘ Reject Application</button><button type="button" className="merchant-review-cancel" disabled={pending} onClick={onClose}>Cancel</button><button type="button" className="merchant-review-approve" disabled={pending} onClick={() => void submit('approve')}>{pending ? <><i className="merchant-review-spinner"/>Approving…</> : '✓ Approve Merchant Account'}</button></>}</footer>
    </section>
  </div>
  return createPortal(content, document.querySelector('.admin-shell') ?? document.body)
}
