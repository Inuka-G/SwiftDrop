import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { USE_MOCKS } from '../../../lib/constants'
import { useDispatchSummary } from '../hooks/useDispatchSummary'
import { useDeliveriesPerDay, useReportSummary } from '../../reporting'
import { formatCount, formatPercent, getBarHeight, getDayLabel, getLoadPerCourier, getRateDifference, getRelativeUpdatedAt, getWeeklyTotals } from '../../reporting/utils/dashboard.utils'
import type { DailyDeliveryCountDto } from '../../reporting/types/reporting.types'

function RetryButton({ onClick }: { onClick: () => void }) { return <button className="dashboard-retry" onClick={onClick}>Try again</button> }
function SectionError({ retry }: { retry: () => void }) { return <div className="dashboard-section-error" role="alert"><span>We could not load this section.</span><RetryButton onClick={retry} /></div> }
function Skeleton({ className = '' }: { className?: string }) { return <div className={`dashboard-skeleton ${className}`} aria-hidden="true" /> }

function KpiCard({ title, value, caption, footer, tone = '', children }: { title: string; value: string; caption: string; footer?: string; tone?: string; children?: React.ReactNode }) {
  return <section className={`dashboard-kpi ${tone}`} aria-label={title}>
    <div className="dashboard-kpi-heading"><span>{title}</span><span className="dashboard-kpi-icon" aria-hidden="true">{title === 'UNASSIGNED' ? '!' : title === 'AVAILABLE COURIERS' ? '◎' : title === 'ACTIVE DELIVERIES' ? '↯' : title === 'DELIVERED TODAY' ? '✓' : title === 'FAILED TODAY' ? '×' : '%'}</span></div>
    <strong className="dashboard-kpi-value">{value}</strong><p>{caption}</p>
    {children}{footer && <div className="dashboard-kpi-footer">{footer}</div>}
  </section>
}

function DeliveriesChart({ days, loading, error, retry }: { days: DailyDeliveryCountDto[]; loading: boolean; error: boolean; retry: () => void }) {
  const max = Math.max(1, ...days.map((day) => Math.max(day.deliveredCount, day.failedCount)))
  const totals = getWeeklyTotals(days)
  const labels = days.map((day, index) => `${getDayLabel(day.date)}${index === days.length - 1 ? ' (Today)' : ''}`)
  const summary = days.length ? `Last seven days: ${totals.delivered} delivered and ${totals.failed} failed.` : 'No deliveries in the last 7 days.'
  return <section className="dashboard-panel chart-panel" aria-labelledby="chart-title">
    <div className="chart-heading"><div><h2 id="chart-title">Deliveries, last 7 days</h2><p>Aggregated distribution of fulfilled and failed orders across {days.length ? `${getDayLabel(days[0].date)} - ${getDayLabel(days[days.length - 1].date)}` : 'the last 7 days'}</p></div><div className="chart-legend"><span><i className="legend-delivered" />Delivered</span><span><i className="legend-failed" />Failed</span></div></div>
    {loading ? <Skeleton className="chart-skeleton" /> : error ? <SectionError retry={retry} /> : <>
      <p className="visually-hidden">{summary}</p>
      <div className="chart-plot" role="img" aria-label={summary}>
        {[0, 1, 2].map((line) => <i className="chart-gridline" key={line} style={{ top: `${line * 50}%` }} />)}
        <div className="chart-days">{days.map((day, index) => <div className={`chart-day${index === days.length - 1 ? ' is-today' : ''}`} key={day.date}>
          <div className="chart-bars">
            {index === days.length - 1 && <span className="chart-today-value">{day.deliveredCount}</span>}
            <span className="chart-bar delivered-bar" tabIndex={0} title={`${labels[index]}: ${day.deliveredCount} delivered`} aria-label={`${labels[index]}: ${day.deliveredCount} delivered`} style={{ height: `${getBarHeight(day.deliveredCount, max)}%` }} />
            <span className="chart-bar failed-bar" tabIndex={0} title={`${labels[index]}: ${day.failedCount} failed`} aria-label={`${labels[index]}: ${day.failedCount} failed`} style={{ height: `${getBarHeight(day.failedCount, max)}%` }} />
          </div><span className="chart-day-label">{labels[index]}</span>
        </div>)}</div>
      </div>
      {!days.some((day) => day.deliveredCount || day.failedCount) && <p className="chart-empty">No deliveries in the last 7 days</p>}
      <table className="visually-hidden"><caption>Deliveries for the last seven days</caption><thead><tr><th>Day</th><th>Created</th><th>Delivered</th><th>Failed</th></tr></thead><tbody>{days.map((day, index) => <tr key={day.date}><th>{labels[index]}</th><td>{day.createdCount}</td><td>{day.deliveredCount}</td><td>{day.failedCount}</td></tr>)}</tbody></table>
    </>}
    <div className="chart-footer"><span>Weekly total: <strong>{formatCount(totals.delivered)}</strong> deliveries <b>•</b> <em>{formatCount(totals.failed)} failed</em></span><span className="dashboard-static-link">View reports →</span></div>
  </section>
}

function SideCards({ available, active, total, unassigned, couriersWithActive }: { available?: number; active?: number; total?: number; unassigned?: number; couriersWithActive: number | null }) {
  const percentage = total ? Math.round(((available ?? 0) / total) * 100) : 0
  const load = active == null ? null : getLoadPerCourier(active, couriersWithActive)
  return <div className="dashboard-side-column">
    <section className="dashboard-panel fleet-panel" aria-labelledby="fleet-title"><div className="side-title"><h2 id="fleet-title">Fleet Dispatch Balance</h2></div>
      <div className={`fleet-summary${total ? '' : ' no-donut'}`}>
        {total && <div className="fleet-donut" style={{ background: `conic-gradient(var(--dashboard-primary) ${percentage}%, var(--dashboard-donut-track) ${percentage}% 100%)` }} role="img" aria-label={`${percentage}% of couriers available`}><span>{percentage}%</span></div>}
        <dl><div><dt>Available couriers</dt><dd>{available ?? '—'}</dd></div><div><dt>With active deliveries</dt><dd>{couriersWithActive ?? '—'}</dd></div>{total && <div><dt>Registered couriers</dt><dd>{total}</dd></div>}</dl>
      </div>
      {load != null && <div className="load-capacity"><span className="load-icon" aria-hidden="true">▣</span><span><strong>Load Capacity</strong><small>{load.toFixed(1)} parcels / active courier</small></span></div>}
    </section>
    <section className="dashboard-panel urgent-panel" aria-labelledby="urgent-title"><div className="side-title"><h2 id="urgent-title">Urgent Dispatch Actions</h2>{(unassigned ?? 0) > 0 && <i className="urgent-dot" aria-label="Unassigned parcels need attention" />}</div>
      <div className="urgent-row"><span className="urgent-icon orange-icon" aria-hidden="true">!</span><span><strong>Unassigned Parcels Queue</strong><small>{unassigned ?? '—'} awaiting courier pairing</small></span><span className="urgent-chevron" aria-hidden="true">›</span></div>
      <div className="urgent-row"><span className="urgent-icon blue-icon" aria-hidden="true">▰</span><span><strong>Standby Courier Fleet</strong><small>{available ?? '—'} available for assignment</small></span><span className="urgent-chevron" aria-hidden="true">›</span></div>
    </section>
  </div>
}

export function AdminDashboardPage() {
  const dispatch = useDispatchSummary()
  const report = useReportSummary()
  const daily = useDeliveriesPerDay()
  const queryClient = useQueryClient()
  const fetching = useIsFetching({ queryKey: ['dispatch'] }) + useIsFetching({ queryKey: ['reporting'] })
  const latestUpdate = Math.max(report.dataUpdatedAt, daily.dataUpdatedAt, dispatch.dataUpdatedAt)
  const allDays = daily.data && 'days' in daily.data ? daily.data.days : []
  const difference = getRateDifference(allDays.at(-1), allDays.at(-2))
  const refresh = () => queryClient.refetchQueries({ predicate: (query) => ['dispatch', 'reporting'].includes(String(query.queryKey[0])) })

  return <main className="admin-dashboard">
    <header className="dashboard-header"><div><p className="dashboard-eyebrow">OPERATIONS LIVE FEED <i /></p><h1>Dashboard</h1></div><button className="last-updated" onClick={refresh} aria-label="Refresh dashboard data"><span aria-hidden="true">◷</span><span>Last updated: <strong>{latestUpdate ? getRelativeUpdatedAt(latestUpdate) : 'Waiting for data'}</strong> • Auto-refresh 30s</span><span className={fetching ? 'refresh-icon is-spinning' : 'refresh-icon'} aria-hidden="true">↻</span></button></header>
    {USE_MOCKS && import.meta.env.DEV && <span className="sample-data-tag">Sample data</span>}
    <div className="dashboard-kpis">
      {dispatch.isLoading ? <><Skeleton className="kpi-skeleton"/><Skeleton className="kpi-skeleton"/><Skeleton className="kpi-skeleton"/></> : dispatch.isError ? <SectionError retry={() => void dispatch.refetch()} /> : <>
        <KpiCard title="UNASSIGNED" value={formatCount(dispatch.data!.unassignedCount)} caption="Parcels awaiting courier" tone="is-highlighted" footer="Go to Dispatch Board →" />
        <KpiCard title="AVAILABLE COURIERS" value={formatCount(dispatch.data!.availableCouriers)} caption="Ready for assignment">{dispatch.data!.totalCouriers > 0 && <div className="courier-availability"><span>of {dispatch.data!.totalCouriers} on duty</span><div><i style={{ width: `${Math.min(100, (dispatch.data!.availableCouriers / dispatch.data!.totalCouriers) * 100)}%` }} /></div><span>{Math.round((dispatch.data!.availableCouriers / dispatch.data!.totalCouriers) * 100)}% ready</span></div>}</KpiCard>
        <KpiCard title="ACTIVE DELIVERIES" value={formatCount(dispatch.data!.activeDeliveries)} caption={`across ${dispatch.data!.couriersWithActiveDeliveries ?? '—'} couriers`} footer="In progress now" />
      </>}
      {report.isLoading ? <><Skeleton className="kpi-skeleton"/><Skeleton className="kpi-skeleton"/><Skeleton className="kpi-skeleton"/></> : report.isError ? <SectionError retry={() => void report.refetch()} /> : <>
        <KpiCard title="DELIVERED TODAY" value={formatCount(report.data!.deliveredCount)} caption={`${formatCount(report.data!.createdCount)} created today`} footer="Completed" />
        <KpiCard title="FAILED TODAY" value={formatCount(report.data!.failedCount)} caption="Failed deliveries today" footer={report.data!.failedCount > 0 ? 'Action required' : undefined} />
        <KpiCard title="SUCCESS RATE" value={formatPercent(report.data!.successRate)} caption="Delivered ÷ completed today" footer={difference == null ? undefined : `${difference >= 0 ? '↗ +' : '↘ '}${Math.abs(difference).toFixed(1)}% vs yesterday`} />
      </>}
    </div>
    <div className="dashboard-main-grid">
      <DeliveriesChart days={allDays} loading={daily.isLoading} error={daily.isError} retry={() => void daily.refetch()} />
      {dispatch.isLoading ? <div className="dashboard-side-column"><Skeleton className="side-skeleton"/><Skeleton className="side-skeleton"/></div> : dispatch.isError ? <SectionError retry={() => void dispatch.refetch()} /> : <SideCards available={dispatch.data!.availableCouriers} active={dispatch.data!.activeDeliveries} total={dispatch.data!.totalCouriers} unassigned={dispatch.data!.unassignedCount} couriersWithActive={dispatch.data!.couriersWithActiveDeliveries} />}
    </div>
  </main>
}


