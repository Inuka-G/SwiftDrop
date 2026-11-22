
import { Link, useNavigate } from 'react-router-dom'
import { useState, type FormEvent } from 'react'
import type { CSSProperties } from 'react'
import { useCustomerDashboard } from '../hooks/useCustomerDashboard'
import { useAuth } from '../../identity'

const formatCurrency = (amount: number) =>
    `LKR ${amount.toLocaleString('en-LK', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })}`

const formatDate = (date: string) =>
    new Intl.DateTimeFormat('en-LK', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
    }).format(new Date(date))

const isDelivered = (status: string) =>
    status.toLowerCase().includes('deliver')

const isInactive = (status: string) => {
    const value = status.toLowerCase()
    return (
        value.includes('deliver') ||
        value.includes('cancel') ||
        value.includes('fail')
    )
}

const getStatusStyle = (status: string): CSSProperties => {
    const value = status.toLowerCase()

    if (value.includes('deliver')) {
        return { background: '#dcfce7', color: '#166534' }
    }

    if (value.includes('cancel') || value.includes('fail')) {
        return { background: '#fee2e2', color: '#991b1b' }
    }

    if (value.includes('transit') || value.includes('pickup')) {
        return { background: '#dbeafe', color: '#1d4ed8' }
    }

    return { background: '#fef3c7', color: '#92400e' }
}

export function CustomerDashboardPage() {
    const { parcels, isLoading, error, refresh } = useCustomerDashboard()
    const { user } = useAuth()
    const navigate = useNavigate()
    const [trackingNumber, setTrackingNumber] = useState('')

    const handleQuickTrack = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault()

        const number = trackingNumber.trim()

        if (!number) return

        navigate(`/track/${encodeURIComponent(number)}`)
    }

    const activeParcels = parcels.filter(
        (parcel) => !isInactive(parcel.currentStatus),
    )

    const deliveredCount = parcels.filter((parcel) =>
        isDelivered(parcel.currentStatus),
    ).length

    const recentParcels = [...parcels]
        .sort(
            (a, b) =>
                new Date(b.createdAtUtc).getTime() -
                new Date(a.createdAtUtc).getTime(),
        )
        .slice(0, 5)

    return (
        <div style={styles.page}>
            <div style={styles.container}>
                <section style={styles.welcome}>
                    <div>
                        <p style={styles.eyebrow}>CUSTOMER PORTAL</p>
                        <h1 style={styles.title}>
                            Welcome, {user?.fullName || 'Customer'}!
                        </h1>
                        <p style={styles.subtitle}>
                            Manage your deliveries and keep track of your parcels in one place.
                        </p>
                    </div>

                    <Link to="/book" style={styles.primaryButton}>
                        + Book a Delivery
                    </Link>
                </section>

                <section style={styles.quickTrack}>
                    <div>
                        <h2 style={styles.quickTrackTitle}>Track Your Parcel</h2>
                        <p style={styles.quickTrackSubtitle}>
                            Enter your tracking number to check your delivery status.
                        </p>
                    </div>

                    <form onSubmit={handleQuickTrack} style={styles.quickTrackForm}>
                        <input
                            type="text"
                            placeholder="Enter tracking number (e.g. SWD-2026-001)"
                            value={trackingNumber}
                            onChange={(event) => setTrackingNumber(event.target.value)}
                            style={styles.quickTrackInput}
                        />

                        <button type="submit" style={styles.quickTrackButton}>
                            Track Parcel →
                        </button>
                    </form>
                </section>

                <section style={styles.statsGrid}>
                    <StatCard
                        label="Total Deliveries"
                        value={isLoading || error ? '—' : parcels.length}
                        icon="📦"
                    />
                    <StatCard
                        label="Active Deliveries"
                        value={isLoading || error ? '—' : activeParcels.length}
                        icon="🚚"
                    />
                    <StatCard
                        label="Delivered"
                        value={isLoading || error ? '—' : deliveredCount}
                        icon="✓"
                    />
                </section>

                <section style={styles.section}>
                    <div style={styles.sectionHeading}>
                        <div>
                            <h2 style={styles.sectionTitle}>Active Deliveries</h2>
                            <p style={styles.sectionSubtitle}>
                                Keep track of your ongoing shipments.
                            </p>
                        </div>
                        <Link to="/history" style={styles.textLink}>
                            View all deliveries →
                        </Link>
                    </div>

                    {isLoading ? (
                        <div style={styles.message}>Loading your deliveries...</div>
                    ) : error ? (
                        <div style={styles.message}>
                            <p>{error}</p>
                            <button style={styles.secondaryButton} onClick={() => void refresh()}>
                                Try Again
                            </button>
                        </div>
                    ) : activeParcels.length === 0 ? (
                        <div style={styles.emptyCard}>
                            <span style={styles.emptyIcon}>📦</span>
                            <h3 style={styles.emptyTitle}>No active deliveries</h3>
                            <p style={styles.sectionSubtitle}>
                                Your ongoing deliveries will appear here.
                            </p>
                            <Link to="/book" style={styles.primaryButton}>
                                Book Your First Delivery
                            </Link>
                        </div>
                    ) : (
                        <div style={styles.activeGrid}>
                            {activeParcels.slice(0, 3).map((parcel) => (

                                <article key={parcel.id} style={styles.parcelCard}>
                                    <div style={styles.parcelTop}>
                                        <div style={styles.parcelHeader}>
                                            <span style={styles.parcelIcon}>📦</span>
                                            <div>
                                                <p style={styles.trackingLabel}>TRACKING NUMBER</p>
                                                <h3 style={styles.trackingNumber}>
                                                    {parcel.trackingNumber}
                                                </h3>
                                            </div>
                                        </div>

                                        <span
                                            style={{
                                                ...styles.statusBadge,
                                                ...getStatusStyle(parcel.currentStatus),
                                            }}
                                        >
                                            {parcel.currentStatus.replace(/[_-]/g, ' ')}
                                        </span>
                                    </div>

                                    <div style={styles.parcelDetails}>
                                        <p style={styles.parcelDetailItem}>
                                            <strong>Recipient</strong>
                                            <span>{parcel.recipientName}</span>
                                        </p>

                                        <p style={styles.parcelDetailItem}>
                                            <strong>Destination</strong>
                                            <span>{parcel.dropoffAddress}</span>
                                        </p>

                                        <p style={styles.parcelDetailItem}>
                                            <strong>Parcel Weight</strong>
                                            <span>{parcel.weightKg} kg</span>
                                        </p>

                                        <p style={styles.parcelDetailItem}>
                                            <strong>Delivery Fee</strong>
                                            <span>{formatCurrency(parcel.estimatedFee)}</span>
                                        </p>
                                    </div>

                                    <Link
                                        to={`/track/${encodeURIComponent(parcel.trackingNumber)}`}
                                        style={styles.cardLink}
                                    >
                                        Track Parcel <span>→</span>
                                    </Link>
                                </article>

                            ))}
                        </div>
                    )}
                </section>

                <section style={styles.section}>
                    <div style={styles.sectionHeading}>
                        <div>
                            <h2 style={styles.sectionTitle}>Recent Deliveries</h2>
                            <p style={styles.sectionSubtitle}>
                                Your latest delivery activity.
                            </p>
                        </div>
                        <Link to="/history" style={styles.textLink}>
                            View history →
                        </Link>
                    </div>

                    {isLoading ? (
                        <div style={styles.message}>Loading recent deliveries...</div>
                    ) : error ? (
                        <div style={styles.message}>Recent deliveries are unavailable until your delivery history loads.</div>
                    ) : !error && recentParcels.length === 0 ? (
                        <div style={styles.message}>
                            No delivery records to display yet.
                        </div>
                    ) : !error ? (
                        <div style={styles.tableWrapper}>
                            <table style={styles.table}>
                                <colgroup>
                                    <col style={{ width: '23%' }} />
                                    <col style={{ width: '23%' }} />
                                    <col style={{ width: '20%' }} />
                                    <col style={{ width: '18%' }} />
                                    <col style={{ width: '16%' }} />
                                </colgroup>
                                <thead>
                                    <tr>
                                        <th style={styles.th}>Tracking Number</th>
                                        <th style={styles.th}>Recipient</th>
                                        <th style={styles.th}>Date</th>
                                        <th style={styles.th}>Delivery Fee</th>
                                        <th style={styles.th}>Status</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {recentParcels.map((parcel) => (
                                        <tr key={parcel.id}>
                                            <td style={styles.td}>
                                                <Link
                                                    to={`/track/${encodeURIComponent(parcel.trackingNumber)}`}
                                                    style={styles.trackingLink}
                                                >
                                                    {parcel.trackingNumber}
                                                </Link>
                                            </td>
                                            <td style={styles.td}>{parcel.recipientName}</td>
                                            <td style={styles.td}>
                                                {formatDate(parcel.createdAtUtc)}
                                            </td>
                                            <td style={styles.td}>
                                                {formatCurrency(parcel.estimatedFee)}
                                            </td>
                                            <td style={styles.td}>
                                                <span
                                                    style={{
                                                        ...styles.statusBadge,
                                                        ...getStatusStyle(parcel.currentStatus),
                                                    }}
                                                >
                                                    {parcel.currentStatus.replace(/[_-]/g, ' ')}
                                                </span>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    ) : null}
                </section>
            </div>
        </div>
    )
}

function StatCard({
    label,
    value,
    icon,
}: {
    label: string
    value: number | string
    icon: string
}) {
    return (
        <article style={styles.statCard}>
            <div style={styles.statIcon}>{icon}</div>
            <div>
                <p style={styles.statLabel}>{label}</p>
                <h2 style={styles.statValue}>{value}</h2>
            </div>
        </article>
    )
}

const styles: Record<string, CSSProperties> = {
    page: {
        minHeight: '100%',
        background: '#f8fafc',
        padding: '36px 24px 60px',
    },
    container: {
        width: '100%',
        maxWidth: '1440px',
        margin: '0 auto',
    },
    welcome: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '20px',
        flexWrap: 'wrap',
        marginBottom: '30px',
    },
    eyebrow: {
        color: '#2563eb',
        fontSize: '12px',
        fontWeight: 700,
        letterSpacing: '1.2px',
        margin: '0 0 10px',
    },
    title: {
        color: '#0f172a',
        fontSize: '30px',
        fontWeight: 750,
        margin: '0 0 8px',
    },
    subtitle: {
        color: '#64748b',
        fontSize: '15px',
        margin: 0,
    },
    primaryButton: {
        display: 'inline-block',
        background: '#2563eb',
        color: '#fff',
        padding: '12px 20px',
        borderRadius: '8px',
        textDecoration: 'none',
        fontWeight: 600,
        fontSize: '14px',
    },
    secondaryButton: {
        border: '1px solid #cbd5e1',
        background: '#fff',
        padding: '9px 16px',
        borderRadius: '7px',
        cursor: 'pointer',
    },
    statsGrid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))',
        gap: '18px',
        marginBottom: '38px',
    },
    statCard: {
        background: '#fff',
        border: '1px solid #e2e8f0',
        borderRadius: '12px',
        padding: '22px',
        display: 'flex',
        alignItems: 'center',
        gap: '16px',
    },
    statIcon: {
        width: '48px',
        height: '48px',
        borderRadius: '10px',
        background: '#eff6ff',
        display: 'grid',
        placeItems: 'center',
        fontSize: '22px',
    },
    statLabel: {
        color: '#64748b',
        fontSize: '13px',
        margin: '0 0 5px',
    },
    statValue: {
        color: '#0f172a',
        fontSize: '25px',
        margin: 0,
    },
    section: {
        marginBottom: '38px',
    },
    sectionHeading: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '15px',
        flexWrap: 'wrap',
        marginBottom: '18px',
    },
    sectionTitle: {
        color: '#0f172a',
        fontSize: '20px',
        margin: '0 0 5px',
    },
    sectionSubtitle: {
        color: '#64748b',
        fontSize: '13px',
        margin: 0,
    },
    textLink: {
        color: '#2563eb',
        textDecoration: 'none',
        fontSize: '14px',
        fontWeight: 600,
    },
    message: {
        background: '#fff',
        border: '1px solid #e2e8f0',
        borderRadius: '10px',
        padding: '28px',
        color: '#64748b',
        textAlign: 'center',
    },
    emptyCard: {
        background: '#fff',
        border: '1px dashed #cbd5e1',
        borderRadius: '12px',
        padding: '40px 20px',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: '12px',
        textAlign: 'center',
    },
    emptyIcon: {
        fontSize: '36px',
    },
    emptyTitle: {
        color: '#0f172a',
        margin: 0,
    },
    activeGrid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
        gap: '18px',
    },
    parcelCard: {
        background: '#fff',
        border: '1px solid #e2e8f0',
        borderRadius: '12px',
        padding: '20px',
    },
    parcelTop: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        gap: '12px',
        marginBottom: '24px',
    },
    parcelHeader: {
        display: 'flex',
        alignItems: 'center',
        gap: '12px',
    },
    parcelIcon: {
        width: '44px',
        height: '44px',
        borderRadius: '10px',
        background: '#eff6ff',
        display: 'grid',
        placeItems: 'center',
        fontSize: '22px',
    },
    statusBadge: {
        display: 'inline-block',
        padding: '5px 10px',
        borderRadius: '20px',
        fontSize: '11px',
        fontWeight: 700,
        textTransform: 'capitalize',
        whiteSpace: 'nowrap',
    },
    trackingLabel: {
        color: '#94a3b8',
        fontSize: '10px',
        fontWeight: 700,
        letterSpacing: '1px',
        margin: '0 0 5px',
    },
    trackingNumber: {
        color: '#0f172a',
        fontSize: '17px',
        margin: 0,
    },
    parcelDetails: {
        display: 'grid',
        gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
        gap: '18px 24px',
        color: '#475569',
        fontSize: '13px',
        lineHeight: 1.6,
        overflowWrap: 'anywhere',
    },
    parcelDetailItem: {
        display: 'flex',
        flexDirection: 'column',
        gap: '4px',
        margin: 0,
    },
    cardLink: {
        display: 'inline-block',
        color: '#2563eb',
        textDecoration: 'none',
        fontWeight: 600,
        fontSize: '13px',
        marginTop: '10px',
    },
    tableWrapper: {
        overflowX: 'auto',
        background: '#fff',
        border: '1px solid #e2e8f0',
        borderRadius: '12px',
        width: '100%',
        WebkitOverflowScrolling: 'touch',
    },
    table: {
        width: '100%',
        borderCollapse: 'collapse',
        tableLayout: 'fixed',
        minWidth: '650px',
    },
    th: {
        padding: '16px 18px',
        textAlign: 'left',
        color: '#64748b',
        fontSize: '12px',
        fontWeight: 700,
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        background: '#f8fafc',
    },

    td: {
        padding: '18px',
        textAlign: 'left',
        borderBottom: '1px solid #e2e8f0',
        color: '#475569',
        fontSize: '13px',
        verticalAlign: 'middle',
    },
    quickTrack: {
        background: '#fff',
        border: '1px solid #e2e8f0',
        borderRadius: '12px',
        padding: '24px',
        marginBottom: '30px',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        gap: '20px',
        flexWrap: 'wrap',
    },
    quickTrackTitle: {
        color: '#0f172a',
        fontSize: '18px',
        margin: '0 0 6px',
    },
    quickTrackSubtitle: {
        color: '#64748b',
        fontSize: '13px',
        margin: 0,
    },
    quickTrackForm: {
        display: 'flex',
        gap: '10px',
        flex: '1 1 420px',
        maxWidth: '550px',
    },
    quickTrackInput: {
        flex: 1,
        minWidth: 0,
        padding: '12px 14px',
        border: '1px solid #cbd5e1',
        borderRadius: '8px',
        fontSize: '13px',
        outline: 'none',
    },
    quickTrackButton: {
        background: '#2563eb',
        color: '#fff',
        border: 'none',
        padding: '12px 18px',
        borderRadius: '8px',
        fontWeight: 600,
        fontSize: '13px',
        cursor: 'pointer',
        whiteSpace: 'nowrap',
    },
    trackingLink: {
        color: '#2563eb',
        fontWeight: 600,
        textDecoration: 'none',
    },
}
