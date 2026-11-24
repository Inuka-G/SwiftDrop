import { useState } from 'react'
import { useApproveMerchant, usePendingMerchants, useRejectMerchant } from '../hooks/useMerchantApprovals'
import type { MerchantApprovalQueueItemDto } from '../types/merchant.types'
import './AdminMerchantQueuePage.css'

export function AdminMerchantQueuePage() {
  const query = usePendingMerchants()
  const approve = useApproveMerchant()
  const reject = useRejectMerchant()
  const queue = query.data ?? []
  const actionId = (approve.isPending ? approve.variables : undefined)
    ?? (reject.isPending ? reject.variables : undefined)
    ?? null
  const loading = query.isLoading
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null)

  async function loadPendingQueue() {
    setAlert(null)
    try {
      await query.refetch()
    } catch {
      setAlert({ type: 'error', message: 'Could not load pending merchant applications. Check your admin session and backend, then try again.' })
    }
  }

  async function handleApprove(merchant: MerchantApprovalQueueItemDto) {
    setAlert(null)

    try {
      await approve.mutateAsync(merchant.id)
      setAlert({
        type: 'success',
        message: `Successfully approved "${merchant.businessName}". Their account is now active and dispatch rails are enabled.`,
      })
    } catch {
      setAlert({ type: 'error', message: `Could not approve "${merchant.businessName}". The application is still pending; refresh and try again.` })
    }
  }

  async function handleReject(merchant: MerchantApprovalQueueItemDto) {
    if (!confirm(`Are you sure you want to reject the application for "${merchant.businessName}"?`)) {
      return
    }

    setAlert(null)

    try {
      await reject.mutateAsync(merchant.id)
      setAlert({
        type: 'success',
        message: `Application for "${merchant.businessName}" has been rejected.`,
      })
    } catch {
      setAlert({ type: 'error', message: `Could not reject "${merchant.businessName}". The application is still pending; refresh and try again.` })
    }
  }

  return (
    <div className="admin-queue-page">
      {/* Header */}
      <section className="admin-queue-header">
        <div className="admin-queue-title">
          <h1>Merchant Approval Queue</h1>
          <p>Review business registrations, verify business tax IDs, and grant access to SwiftDrop shipping rails.</p>
        </div>

        <div className="admin-queue-actions">
          <button
            type="button"
            className="admin-refresh-btn"
            onClick={loadPendingQueue}
            disabled={query.isFetching}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67" />
            </svg>
            <span>{query.isFetching ? 'Refreshing...' : 'Refresh Queue'}</span>
          </button>
        </div>
      </section>

      {/* Alert Banner */}
      {alert && (
        <div
          className={`admin-alert-banner ${
            alert.type === 'success' ? 'admin-alert-banner--success' : 'admin-alert-banner--error'
          }`}
          role="status"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
            <polyline points="20 6 9 17 4 12" />
          </svg>
          <span>{alert.message}</span>
        </div>
      )}

      {query.isError && !alert && (
        <div className="admin-alert-banner admin-alert-banner--error" role="alert">
          <span>Could not load pending applications from the server. Use Refresh Queue to retry.</span>
        </div>
      )}

      {/* Queue Card */}
      <section className="admin-queue-card">
        <div className="admin-queue-card__header">
          <h3>Pending Merchant Applications</h3>
          <span className="admin-queue-count">{queue.length} Awaiting Review</span>
        </div>

        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Registered Business Name</th>
                <th>Business Reg / Tax ID</th>
                <th>Application Date</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Review Decision</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={5} className="admin-empty-queue">Loading pending applications…</td></tr>
              ) : query.isError ? (
                <tr><td colSpan={5} className="admin-empty-queue">Pending applications could not be loaded. Refresh the queue to try again.</td></tr>
              ) : queue.length === 0 ? (
                <tr>
                  <td colSpan={5} className="admin-empty-queue">
                    <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                      <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                      <polyline points="22 4 12 14.01 9 11.01" />
                    </svg>
                    <p style={{ fontWeight: 600, fontSize: '1rem', margin: '0 0 0.25rem' }}>All Caught Up!</p>
                    <p style={{ margin: 0, fontSize: '0.875rem' }}>There are no pending merchant registrations waiting for review.</p>
                  </td>
                </tr>
              ) : (
                queue.map((merchant) => (
                  <tr key={merchant.id}>
                    <td>
                      <strong style={{ fontSize: '0.95rem' }}>{merchant.businessName}</strong>
                    </td>
                    <td>
                      <code
                        style={{
                          fontFamily: "var(--font-mono, 'JetBrains Mono', monospace)",
                          background: 'var(--color-surface-container-low, #f2f3ff)',
                          padding: '0.2rem 0.5rem',
                          borderRadius: '0.25rem',
                          fontSize: '0.85rem',
                          fontWeight: 600,
                          color: 'var(--color-primary, #004ac6)',
                        }}
                      >
                        {merchant.businessRegNumber}
                      </code>
                    </td>
                    <td>{new Date(merchant.createdAtUtc).toLocaleString()}</td>
                    <td>
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '0.35rem',
                          padding: '0.25rem 0.65rem',
                          borderRadius: '9999px',
                          background: 'var(--color-secondary-fixed, #ffdbca)',
                          color: 'var(--color-on-secondary-fixed, #341100)',
                          fontSize: '0.75rem',
                          fontWeight: 700,
                          textTransform: 'uppercase',
                        }}
                      >
                        ● {merchant.status}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div className="admin-btn-group">
                        <button
                          type="button"
                          className="admin-approve-btn"
                          disabled={actionId === merchant.id || Boolean(actionId)}
                          onClick={() => handleApprove(merchant)}
                        >
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                            <polyline points="20 6 9 17 4 12" />
                          </svg>
                          <span>{approve.isPending && approve.variables === merchant.id ? 'Approving...' : 'Approve'}</span>
                        </button>

                        <button
                          type="button"
                          className="admin-reject-btn"
                          disabled={actionId === merchant.id || Boolean(actionId)}
                          onClick={() => handleReject(merchant)}
                        >
                          <span>Reject</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

