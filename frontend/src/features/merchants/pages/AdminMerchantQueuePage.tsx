import { useState } from 'react'
import { merchantApi } from '../api/merchant.api'
import { useApproveMerchant, usePendingMerchants, useRejectMerchant } from '../hooks/useMerchantApprovals'
import type { MerchantApprovalQueueItemDto, MerchantDto } from '../types/merchant.types'
import './AdminMerchantQueuePage.css'

const REG_TYPE_LABELS: Record<string, string> = {
  BRN: 'Business Registration Number (BRN)',
  CRN: 'Company Registration Number (CRN)',
  TIN: 'Tax Identification Number (TIN - 9 digits)',
}

export function AdminMerchantQueuePage() {
  const query = usePendingMerchants()
  const approve = useApproveMerchant()
  const reject = useRejectMerchant()
  const queue: MerchantApprovalQueueItemDto[] = query.data ?? []
  const actionId =
    (approve.isPending ? approve.variables : undefined) ??
    (reject.isPending ? reject.variables : undefined) ??
    null
  const loading = query.isLoading
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null)
  const [selectedApp, setSelectedApp] = useState<MerchantApprovalQueueItemDto | null>(null)
  const [selectedFullDetail, setSelectedFullDetail] = useState<MerchantDto | null>(null)

  async function openApplicationDetails(merchant: MerchantApprovalQueueItemDto) {
    setSelectedApp(merchant)
    setSelectedFullDetail(null)
    try {
      const full = await merchantApi.getMerchantById(merchant.id)
      setSelectedFullDetail(full)
    } catch {
      // Fallback to queue item details if GET /merchants/{id} fails
    }
  }

  async function loadPendingQueue() {
    setAlert(null)
    try {
      await query.refetch()
    } catch {
      setAlert({
        type: 'error',
        message: 'Could not load pending merchant applications. Check your admin session and backend, then try again.',
      })
    }
  }

  async function handleApprove(merchant: MerchantApprovalQueueItemDto) {
    setAlert(null)

    try {
      await approve.mutateAsync(merchant.id)
      setSelectedApp(null)
      setAlert({
        type: 'success',
        message: `Successfully approved "${merchant.businessName}". Their account is now active and dispatch rails are enabled.`,
      })
    } catch {
      setAlert({
        type: 'error',
        message: `Could not approve "${merchant.businessName}". The application is still pending; refresh and try again.`,
      })
    }
  }

  async function handleReject(merchant: MerchantApprovalQueueItemDto) {
    if (!confirm(`Are you sure you want to reject the application for "${merchant.businessName}"?`)) {
      return
    }

    setAlert(null)

    try {
      await reject.mutateAsync(merchant.id)
      setSelectedApp(null)
      setAlert({
        type: 'success',
        message: `Application for "${merchant.businessName}" has been rejected.`,
      })
    } catch {
      setAlert({
        type: 'error',
        message: `Could not reject "${merchant.businessName}". The application is still pending; refresh and try again.`,
      })
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
                          className="admin-view-btn"
                          onClick={() => openApplicationDetails(merchant)}
                        >
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                            <circle cx="12" cy="12" r="3" />
                          </svg>
                          <span>View Application</span>
                        </button>

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

      {/* View Application Details Modal */}
      {selectedApp && (
        <div className="admin-app-modal-backdrop" onClick={() => setSelectedApp(null)}>
          <div className="admin-app-modal" onClick={(e) => e.stopPropagation()} role="dialog" aria-modal="true">
            <div className="admin-app-modal__header">
              <div>
                <h3>Merchant Application Details</h3>
                <p>Verify business registration credentials before approving access to SwiftDrop.</p>
              </div>
              <button
                type="button"
                className="admin-app-modal__close"
                aria-label="Close application view"
                onClick={() => setSelectedApp(null)}
              >
                ×
              </button>
            </div>

            <div className="admin-app-details-grid">
              <div className="admin-app-detail-item admin-app-detail-item--full">
                <span className="admin-app-detail-label">Registered Business / Organization Name</span>
                <span className="admin-app-detail-value" style={{ fontSize: '1.1rem' }}>
                  {selectedFullDetail?.businessName ?? selectedApp.businessName}
                </span>
              </div>

              <div className="admin-app-detail-item">
                <span className="admin-app-detail-label">Registration Type</span>
                <span className="admin-app-detail-value">
                  {REG_TYPE_LABELS[(selectedFullDetail?.businessRegType ?? selectedApp.businessRegType ?? 'BRN') as string] ??
                    (selectedFullDetail?.businessRegType ?? selectedApp.businessRegType ?? 'BRN')}
                </span>
              </div>

              <div className="admin-app-detail-item">
                <span className="admin-app-detail-label">Registration / Tax Number</span>
                <span className="admin-app-detail-value">
                  <code
                    style={{
                      fontFamily: "var(--font-mono, 'JetBrains Mono', monospace)",
                      background: '#ffffff',
                      padding: '0.2rem 0.5rem',
                      borderRadius: '0.25rem',
                      color: 'var(--color-primary, #004ac6)',
                      border: '1px solid #dbe1ff',
                    }}
                  >
                    {selectedFullDetail?.businessRegNumber ?? selectedApp.businessRegNumber}
                  </code>
                </span>
              </div>

              <div className="admin-app-detail-item">
                <span className="admin-app-detail-label">Application Status</span>
                <span className="admin-app-detail-value" style={{ color: '#9d4300' }}>
                  ● {selectedFullDetail?.status ?? selectedApp.status}
                </span>
              </div>

              <div className="admin-app-detail-item">
                <span className="admin-app-detail-label">Submitted At</span>
                <span className="admin-app-detail-value">
                  {new Date(selectedFullDetail?.createdAtUtc ?? selectedApp.createdAtUtc).toLocaleString()}
                </span>
              </div>

              <div className="admin-app-detail-item admin-app-detail-item--full">
                <span className="admin-app-detail-label">Configured Webhook Endpoint URL</span>
                <span className="admin-app-detail-value">
                  {(selectedFullDetail?.webhookUrl ?? selectedApp.webhookUrl) ? (
                    <code style={{ fontFamily: "var(--font-mono, 'JetBrains Mono', monospace)", fontSize: '0.84rem' }}>
                      {selectedFullDetail?.webhookUrl ?? selectedApp.webhookUrl}
                    </code>
                  ) : (
                    <span style={{ color: '#64748b', fontWeight: 400 }}>Not configured (Optional)</span>
                  )}
                </span>
              </div>

              <div className="admin-app-detail-item">
                <span className="admin-app-detail-label">Application ID</span>
                <span className="admin-app-detail-value" style={{ fontSize: '0.78rem', fontFamily: 'monospace' }}>
                  {selectedApp.id}
                </span>
              </div>

              <div className="admin-app-detail-item">
                <span className="admin-app-detail-label">Applicant User ID</span>
                <span className="admin-app-detail-value" style={{ fontSize: '0.78rem', fontFamily: 'monospace' }}>
                  {selectedFullDetail?.userId ?? selectedApp.userId ?? 'Unlinked / Guest Submission'}
                </span>
              </div>
            </div>

            <div className="admin-app-modal__actions">
              <button
                type="button"
                className="admin-refresh-btn"
                onClick={() => setSelectedApp(null)}
              >
                Close
              </button>
              <button
                type="button"
                className="admin-reject-btn"
                disabled={actionId === selectedApp.id}
                onClick={() => handleReject(selectedApp)}
              >
                Reject Application
              </button>
              <button
                type="button"
                className="admin-approve-btn"
                disabled={actionId === selectedApp.id}
                onClick={() => handleApprove(selectedApp)}
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                  <polyline points="20 6 9 17 4 12" />
                </svg>
                <span>{actionId === selectedApp.id ? 'Approving...' : 'Approve Application'}</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

