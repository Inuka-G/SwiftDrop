
import { useState, type FormEvent } from 'react'
import { parcelsApi } from '../api/parcels.api'
import type { PublicTrackingDto } from '../types/parcel.types'

export function PublicTrackingPage() {
  const [trackingNumber, setTrackingNumber] = useState('')
  const [parcel, setParcel] = useState<PublicTrackingDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleTrack = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const number = trackingNumber.trim()

    if (!number) {
      setError('Please enter a tracking number.')
      setParcel(null)
      return
    }

    setLoading(true)
    setError('')
    setParcel(null)

    try {
      const result = await parcelsApi.trackParcel(number)
      setParcel(result)
    } catch (err: any) {
      const message =
        err?.response?.data?.message ||
        'We could not find a parcel with that tracking number. Please check it and try again.'

      setError(message)
    } finally {
      setLoading(false)
    }
  }

  const formatDate = (date: string) => {
    const parsedDate = new Date(date)

    if (Number.isNaN(parsedDate.getTime())) {
      return 'Not available'
    }

    return new Intl.DateTimeFormat('en-LK', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(parsedDate)
  }

  const getStatusStyle = (status: string) => {
    const value = status.toLowerCase()

    if (value.includes('deliver')) {
      return { background: '#dcfce7', color: '#166534' }
    }

    if (value.includes('cancel') || value.includes('fail')) {
      return { background: '#fee2e2', color: '#991b1b' }
    }

    if (value.includes('transit') || value.includes('picked')) {
      return { background: '#dbeafe', color: '#1d4ed8' }
    }

    return { background: '#fef3c7', color: '#92400e' }
  }

  return (
    <div style={styles.page}>
      <div style={styles.container}>
        <div style={styles.heading}>
          <div style={styles.eyebrow}>SWIFTDROP DELIVERY</div>
          <h1 style={styles.title}>Track Your Package</h1>
          <p style={styles.subtitle}>
            Stay updated on your delivery. Enter your tracking number below
            to check the latest parcel status.
          </p>
        </div>

        <form onSubmit={handleTrack} style={styles.searchCard}>
          <label htmlFor="trackingNumber" style={styles.label}>
            Tracking Number
          </label>

          <div style={styles.searchRow}>
            <input
              id="trackingNumber"
              type="text"
              value={trackingNumber}
              onChange={(event) => setTrackingNumber(event.target.value)}
              placeholder="e.g. SD-XXXXXXXX"
              style={styles.input}
            />

            <button
              type="submit"
              disabled={loading}
              style={{
                ...styles.trackButton,
                opacity: loading ? 0.7 : 1,
                cursor: loading ? 'not-allowed' : 'pointer',
              }}
            >
              {loading ? 'Tracking...' : 'Track Package'}
            </button>
          </div>

          <p style={styles.helperText}>
            You can find your tracking number in your booking confirmation.
          </p>
        </form>

        {error && (
          <div role="alert" style={styles.errorBox}>
            <span style={styles.errorIcon}>!</span>
            <div>
              <strong>Unable to track parcel</strong>
              <p style={styles.errorText}>{error}</p>
            </div>
          </div>
        )}

        {parcel && (
          <div style={styles.resultCard}>
            <div style={styles.resultHeader}>
              <div>
                <p style={styles.resultLabel}>TRACKING NUMBER</p>
                <h2 style={styles.trackingNumber}>
                  {parcel.trackingNumber}
                </h2>
              </div>

              <span
                style={{
                  ...styles.statusBadge,
                  ...getStatusStyle(parcel.currentStatus),
                }}
              >
                {parcel.currentStatus}
              </span>
            </div>

            <div style={styles.divider} />

            <h3 style={styles.sectionTitle}>Delivery Information</h3>

            <div style={styles.infoGrid}>
              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>📍</span>
                <div>
                  <p style={styles.infoLabel}>Destination Area</p>
                  <p style={styles.infoValue}>
                    {parcel.destinationArea || 'Not available'}
                  </p>
                </div>
              </div>

              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>📦</span>
                <div>
                  <p style={styles.infoLabel}>Booking Date</p>
                  <p style={styles.infoValue}>
                    {formatDate(parcel.createdAtUtc)}
                  </p>
                </div>
              </div>

              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>🕒</span>
                <div>
                  <p style={styles.infoLabel}>Last Updated</p>
                  <p style={styles.infoValue}>
                    {formatDate(parcel.updatedAtUtc)}
                  </p>
                </div>
              </div>

              <div style={styles.infoItem}>
                <span style={styles.infoIcon}>🚚</span>
                <div>
                  <p style={styles.infoLabel}>Current Status</p>
                  <p style={styles.infoValue}>{parcel.currentStatus}</p>
                </div>
              </div>
            </div>

            <div style={styles.note}>
              <span>ℹ️</span>
              <p>
                This information reflects the latest parcel details available
                in the system.
              </p>
            </div>
          </div>
        )}

        {!parcel && !error && !loading && (
          <div style={styles.emptyState}>
            <div style={styles.emptyIcon}>📦</div>
            <h3 style={styles.emptyTitle}>Your parcel journey starts here</h3>
            <p style={styles.emptyText}>
              Enter your tracking number above to view your parcel's latest
              delivery information.
            </p>
          </div>
        )}

        <p style={styles.footer}>
          Need help with your delivery? Please contact the SwiftDrop support
          team.
        </p>
      </div>
    </div>
  )
}

const styles: Record<string, React.CSSProperties> = {
  page: {
    minHeight: '100vh',
    background: '#f5f7fb',
    padding: '48px 20px',
    fontFamily: 'Arial, sans-serif',
  },
  container: {
    maxWidth: '850px',
    margin: '0 auto',
  },
  heading: {
    textAlign: 'center',
    marginBottom: '32px',
  },
  eyebrow: {
    color: '#2563eb',
    fontSize: '12px',
    fontWeight: 700,
    letterSpacing: '2px',
    marginBottom: '12px',
  },
  title: {
    color: '#172554',
    fontSize: '34px',
    fontWeight: 800,
    margin: '0 0 12px',
  },
  subtitle: {
    color: '#64748b',
    fontSize: '15px',
    lineHeight: 1.7,
    maxWidth: '570px',
    margin: '0 auto',
  },
  searchCard: {
    background: '#ffffff',
    padding: '28px',
    borderRadius: '14px',
    boxShadow: '0 4px 20px rgba(15, 23, 42, 0.06)',
    border: '1px solid #e2e8f0',
  },
  label: {
    display: 'block',
    color: '#334155',
    fontSize: '14px',
    fontWeight: 700,
    marginBottom: '10px',
  },
  searchRow: {
    display: 'flex',
    gap: '12px',
    flexWrap: 'wrap',
  },
  input: {
    flex: '1 1 250px',
    minWidth: 0,
    padding: '14px 16px',
    border: '1px solid #cbd5e1',
    borderRadius: '8px',
    fontSize: '14px',
    outline: 'none',
    color: '#1e293b',
  },
  trackButton: {
    padding: '14px 24px',
    border: 'none',
    borderRadius: '8px',
    background: '#2563eb',
    color: '#ffffff',
    fontSize: '14px',
    fontWeight: 700,
  },
  helperText: {
    color: '#94a3b8',
    fontSize: '12px',
    margin: '12px 0 0',
  },
  errorBox: {
    display: 'flex',
    gap: '12px',
    alignItems: 'flex-start',
    background: '#fef2f2',
    border: '1px solid #fecaca',
    borderRadius: '10px',
    padding: '16px',
    marginTop: '20px',
    color: '#991b1b',
  },
  errorIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '22px',
    height: '22px',
    borderRadius: '50%',
    background: '#fee2e2',
    fontWeight: 800,
    flexShrink: 0,
  },
  errorText: {
    margin: '5px 0 0',
    fontSize: '13px',
    lineHeight: 1.5,
  },
  resultCard: {
    background: '#ffffff',
    border: '1px solid #e2e8f0',
    borderRadius: '14px',
    padding: '28px',
    marginTop: '24px',
    boxShadow: '0 4px 20px rgba(15, 23, 42, 0.05)',
  },
  resultHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: '16px',
    flexWrap: 'wrap',
  },
  resultLabel: {
    color: '#94a3b8',
    fontSize: '11px',
    fontWeight: 700,
    letterSpacing: '1.3px',
    margin: '0 0 8px',
  },
  trackingNumber: {
    color: '#172554',
    fontSize: '22px',
    margin: 0,
    overflowWrap: 'anywhere',
  },
  statusBadge: {
    display: 'inline-block',
    padding: '8px 14px',
    borderRadius: '20px',
    fontSize: '12px',
    fontWeight: 700,
  },
  divider: {
    height: '1px',
    background: '#e2e8f0',
    margin: '24px 0',
  },
  sectionTitle: {
    color: '#1e293b',
    fontSize: '17px',
    margin: '0 0 22px',
  },
  infoGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
    gap: '24px',
  },
  infoItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: '12px',
  },
  infoIcon: {
    fontSize: '20px',
  },
  infoLabel: {
    color: '#94a3b8',
    fontSize: '12px',
    margin: '0 0 6px',
  },
  infoValue: {
    color: '#334155',
    fontSize: '14px',
    fontWeight: 600,
    margin: 0,
    overflowWrap: 'anywhere',
  },
  note: {
    display: 'flex',
    gap: '10px',
    alignItems: 'flex-start',
    background: '#eff6ff',
    color: '#1e40af',
    padding: '14px',
    borderRadius: '8px',
    marginTop: '28px',
    fontSize: '12px',
    lineHeight: 1.6,
  },
  emptyState: {
    textAlign: 'center',
    padding: '48px 20px',
    marginTop: '20px',
  },
  emptyIcon: {
    fontSize: '42px',
    marginBottom: '12px',
  },
  emptyTitle: {
    color: '#334155',
    fontSize: '17px',
    margin: '0 0 8px',
  },
  emptyText: {
    color: '#94a3b8',
    fontSize: '13px',
    lineHeight: 1.6,
    maxWidth: '420px',
    margin: '0 auto',
  },
  footer: {
    textAlign: 'center',
    color: '#94a3b8',
    fontSize: '12px',
    marginTop: '32px',
    lineHeight: 1.6,
  },
}
