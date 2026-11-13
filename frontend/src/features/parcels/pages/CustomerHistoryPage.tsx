
import { useEffect, useMemo, useState } from 'react'
import type { CSSProperties } from 'react'
import { parcelsApi } from '../api/parcels.api'
import type { ParcelSummaryDto } from '../types/parcel.types'

const formatDate = (date: string) =>
  new Intl.DateTimeFormat('en-LK', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))

const formatCurrency = (amount: number) =>
  `LKR ${amount.toLocaleString('en-LK', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`

const getStatusStyle = (status: string): CSSProperties => {
  const normalized = status.toLowerCase().replace(/[_-]/g, ' ')

  if (normalized.includes('deliver')) {
    return {
      background: '#dcfce7',
      color: '#166534',
    }
  }

  if (normalized.includes('fail') || normalized.includes('cancel')) {
    return {
      background: '#fee2e2',
      color: '#991b1b',
    }
  }

  if (normalized.includes('transit') || normalized.includes('pickup')) {
    return {
      background: '#dbeafe',
      color: '#1d4ed8',
    }
  }

  return {
    background: '#fef3c7',
    color: '#92400e',
  }
}

export function CustomerHistoryPage() {
  const [deliveries, setDeliveries] = useState<ParcelSummaryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [receiptError, setReceiptError] = useState('')

  useEffect(() => {
    let active = true

    const loadHistory = async () => {
      try {
        setLoading(true)
        setError('')

        const data = await parcelsApi.getMyHistory()

        if (active) {
          setDeliveries(data)
        }
      } catch {
        if (active) {
          setError(
            'Unable to load your delivery history. Please try again.',
          )
        }
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }

    void loadHistory()

    return () => {
      active = false
    }
  }, [])

  const filteredDeliveries = useMemo(() => {
    return deliveries.filter((delivery) => {
      const matchesSearch = [
        delivery.trackingNumber,
        delivery.recipientName,
        delivery.dropoffAddress,
      ].some((value) =>
        value.toLowerCase().includes(search.toLowerCase()),
      )

      const matchesStatus =
        statusFilter === 'ALL' ||
        delivery.currentStatus.toUpperCase().replace(/[\s-]/g, '_') ===
          statusFilter

      return matchesSearch && matchesStatus
    })
  }, [deliveries, search, statusFilter])

  const handleDownloadReceipt = async (parcelId: string) => {
    try {
      setReceiptError('')
      setDownloadingId(parcelId)
      await parcelsApi.downloadReceipt(parcelId)
    } catch {
      setReceiptError('Unable to download the receipt. Please try again.')
    } finally {
      setDownloadingId(null)
    }
  }

  const statusOptions = [
    'ALL',
    ...Array.from(
      new Set(
        deliveries.map((delivery) =>
          delivery.currentStatus.toUpperCase().replace(/[\s-]/g, '_'),
        ),
      ),
    ),
  ]

  return (
    <div style={styles.page}>
      <div style={styles.container}>
        {/* Page heading */}
        <div style={styles.heading}>
          <div>
            <p style={styles.breadcrumb}>
              CUSTOMER PORTAL / DELIVERY HISTORY
            </p>
            <h1 style={styles.title}>Delivery History</h1>
            <p style={styles.subtitle}>
              View your previous deliveries, check their status and download
              your receipts.
            </p>
          </div>

          <div style={styles.totalCard}>
            <span style={styles.totalLabel}>Total Deliveries</span>
            <span style={styles.totalNumber}>
              {deliveries.length}
            </span>
          </div>
        </div>

        {/* Search and filters */}
        <div style={styles.toolbar}>
          <div style={styles.searchWrapper}>
            <span style={styles.searchIcon}>⌕</span>
            <input
              type="text"
              placeholder="Search tracking number, recipient or destination..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={styles.searchInput}
            />
          </div>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            style={styles.filterSelect}
          >
            {statusOptions.map((status) => (
              <option key={status} value={status}>
                {status === 'ALL'
                  ? 'All Statuses'
                  : status.replace(/_/g, ' ')}
              </option>
            ))}
          </select>
        </div>

        {/* Receipt error */}
        {receiptError && (
          <div style={styles.errorBox}>{receiptError}</div>
        )}

        {/* Delivery table */}
        <div style={styles.tableCard}>
          {loading ? (
            <div style={styles.message}>
              <div style={styles.spinner} />
              <p>Loading your delivery history...</p>
            </div>
          ) : error ? (
            <div style={styles.message}>
              <span style={styles.messageIcon}>!</span>
              <p>{error}</p>
              <button
                style={styles.retryButton}
                onClick={() => window.location.reload()}
              >
                Try Again
              </button>
            </div>
          ) : filteredDeliveries.length === 0 ? (
            <div style={styles.message}>
              <span style={styles.emptyIcon}>📦</span>
              <h3 style={styles.emptyTitle}>
                {deliveries.length === 0
                  ? 'No deliveries yet'
                  : 'No matching deliveries'}
              </h3>
              <p style={styles.emptyText}>
                {deliveries.length === 0
                  ? 'Your delivery bookings will appear here once you create one.'
                  : 'Try changing your search or status filter.'}
              </p>
            </div>
          ) : (
            <>
              <div style={styles.tableWrapper}>
                <table style={styles.table}>
                  <thead>
                    <tr>
                      <th style={styles.th}>Tracking ID</th>
                      <th style={styles.th}>Booking Date</th>
                      <th style={styles.th}>Recipient & Destination</th>
                      <th style={styles.th}>Weight</th>
                      <th style={styles.th}>Estimated Fee</th>
                      <th style={styles.th}>Status</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>
                        Actions
                      </th>
                    </tr>
                  </thead>

                  <tbody>
                    {filteredDeliveries.map((delivery) => (
                      <tr key={delivery.id} style={styles.tableRow}>
                        <td style={styles.td}>
                          <span style={styles.trackingNumber}>
                            {delivery.trackingNumber}
                          </span>
                        </td>

                        <td style={styles.td}>
                          <span style={styles.dateText}>
                            {formatDate(delivery.createdAtUtc)}
                          </span>
                        </td>

                        <td style={styles.td}>
                          <strong style={styles.recipient}>
                            {delivery.recipientName}
                          </strong>
                          <span style={styles.destination}>
                            📍 {delivery.dropoffAddress}
                          </span>
                        </td>

                        <td style={styles.td}>
                          {delivery.weightKg} kg
                        </td>

                        <td style={styles.td}>
                          <strong style={styles.fee}>
                            {formatCurrency(delivery.estimatedFee)}
                          </strong>
                        </td>

                        <td style={styles.td}>
                          <span
                            style={{
                              ...styles.statusBadge,
                              ...getStatusStyle(delivery.currentStatus),
                            }}
                          >
                            {delivery.currentStatus.replace(/_/g, ' ')}
                          </span>
                        </td>

                        <td style={{ ...styles.td, textAlign: 'right' }}>
                          <button
                            style={styles.receiptButton}
                            onClick={() =>
                              void handleDownloadReceipt(delivery.id)
                            }
                            disabled={downloadingId === delivery.id}
                          >
                            {downloadingId === delivery.id
                              ? 'Downloading...'
                              : '↓ Receipt'}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div style={styles.tableFooter}>
                Showing {filteredDeliveries.length} of {deliveries.length}{' '}
                deliveries
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  page: {
    minHeight: 'calc(100vh - 70px)',
    background: '#f8faff',
    padding: '32px 24px 48px',
    color: '#172033',
  },
  container: {
    maxWidth: '1400px',
    margin: '0 auto',
  },
  heading: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: '20px',
    marginBottom: '28px',
  },
  breadcrumb: {
    fontSize: '11px',
    fontWeight: 700,
    letterSpacing: '1px',
    color: '#64748b',
    margin: '0 0 10px',
  },
  title: {
    fontSize: '30px',
    fontWeight: 700,
    margin: '0 0 8px',
    letterSpacing: '-0.7px',
  },
  subtitle: {
    fontSize: '14px',
    color: '#64748b',
    margin: 0,
    lineHeight: 1.6,
  },
  totalCard: {
    background: '#ffffff',
    border: '1px solid #e5eaf3',
    borderRadius: '12px',
    padding: '16px 24px',
    display: 'flex',
    flexDirection: 'column',
    gap: '6px',
    minWidth: '150px',
  },
  totalLabel: {
    fontSize: '12px',
    color: '#64748b',
    fontWeight: 500,
  },
  totalNumber: {
    fontSize: '28px',
    fontWeight: 700,
    color: '#1d4ed8',
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flexWrap: 'wrap',
    background: '#ffffff',
    padding: '18px',
    border: '1px solid #e5eaf3',
    borderRadius: '12px',
    marginBottom: '20px',
  },
  searchWrapper: {
    position: 'relative',
    flex: '1 1 300px',
  },
  searchIcon: {
    position: 'absolute',
    left: '14px',
    top: '9px',
    fontSize: '22px',
    color: '#94a3b8',
  },
  searchInput: {
    width: '100%',
    height: '42px',
    padding: '0 14px 0 42px',
    boxSizing: 'border-box',
    border: '1px solid #e2e8f0',
    borderRadius: '8px',
    background: '#f8fafc',
    fontSize: '13px',
    outlineColor: '#2563eb',
  },
  filterSelect: {
    height: '42px',
    minWidth: '170px',
    padding: '0 12px',
    border: '1px solid #e2e8f0',
    borderRadius: '8px',
    background: '#f8fafc',
    color: '#334155',
    fontSize: '13px',
    cursor: 'pointer',
  },
  tableCard: {
    background: '#ffffff',
    border: '1px solid #e5eaf3',
    borderRadius: '12px',
    overflow: 'hidden',
  },
  tableWrapper: {
    width: '100%',
    overflowX: 'auto',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    textAlign: 'left',
    minWidth: '950px',
  },
  th: {
    background: '#f1f5fb',
    color: '#64748b',
    padding: '16px 14px',
    fontSize: '11px',
    fontWeight: 700,
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    whiteSpace: 'nowrap',
  },
  tableRow: {
    borderTop: '1px solid #edf0f5',
  },
  td: {
    padding: '17px 14px',
    fontSize: '13px',
    color: '#334155',
    verticalAlign: 'middle',
  },
  trackingNumber: {
    color: '#1d4ed8',
    fontWeight: 700,
    fontSize: '12px',
    whiteSpace: 'nowrap',
  },
  dateText: {
    color: '#64748b',
    fontSize: '12px',
    whiteSpace: 'nowrap',
  },
  recipient: {
    display: 'block',
    fontSize: '13px',
    color: '#1e293b',
    marginBottom: '6px',
  },
  destination: {
    display: 'block',
    color: '#64748b',
    fontSize: '11px',
    maxWidth: '230px',
    lineHeight: 1.5,
  },
  fee: {
    color: '#1e293b',
    whiteSpace: 'nowrap',
  },
  statusBadge: {
    display: 'inline-block',
    padding: '6px 10px',
    borderRadius: '20px',
    fontSize: '10px',
    fontWeight: 700,
    whiteSpace: 'nowrap',
  },
  receiptButton: {
    background: '#eff6ff',
    color: '#1d4ed8',
    border: 'none',
    borderRadius: '7px',
    padding: '8px 11px',
    fontSize: '11px',
    fontWeight: 600,
    cursor: 'pointer',
    whiteSpace: 'nowrap',
  },
  tableFooter: {
    padding: '15px 18px',
    background: '#f8fafc',
    borderTop: '1px solid #edf0f5',
    fontSize: '12px',
    color: '#64748b',
  },
  message: {
    minHeight: '280px',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '12px',
    padding: '30px',
    textAlign: 'center',
    color: '#64748b',
    fontSize: '14px',
  },
  spinner: {
    width: '28px',
    height: '28px',
    border: '3px solid #dbeafe',
    borderTopColor: '#2563eb',
    borderRadius: '50%',
    animation: 'spin 0.8s linear infinite',
  },
  messageIcon: {
    width: '36px',
    height: '36px',
    borderRadius: '50%',
    background: '#fee2e2',
    color: '#dc2626',
    display: 'grid',
    placeItems: 'center',
    fontWeight: 700,
    fontSize: '20px',
  },
  retryButton: {
    padding: '9px 18px',
    background: '#2563eb',
    color: '#ffffff',
    border: 'none',
    borderRadius: '7px',
    cursor: 'pointer',
    fontSize: '12px',
    fontWeight: 600,
  },
  emptyIcon: {
    fontSize: '36px',
  },
  emptyTitle: {
    fontSize: '17px',
    color: '#1e293b',
    margin: 0,
  },
  emptyText: {
    fontSize: '13px',
    color: '#64748b',
    margin: 0,
  },
  errorBox: {
    background: '#fef2f2',
    border: '1px solid #fecaca',
    color: '#b91c1c',
    borderRadius: '8px',
    padding: '12px 16px',
    marginBottom: '16px',
    fontSize: '13px',
  },
}
