import { useMemo, useState } from 'react'
import { USE_MOCKS, REPORT_MAX_RANGE_DAYS, REPORT_PRESETS, REPORT_TABLE_PAGE_SIZE } from '../../../lib/constants'
import { useDeliveriesPerDay } from '../hooks/useDeliveriesPerDay'
import { useSuccessRate } from '../hooks/useSuccessRate'
import type { DailyDeliveryCountDto } from '../types/reporting.types'
import { barHeight, buildCsv, csvFileName, formatRate, formatReportDate, isEmptyReport, labelStride, niceTicks, pageNumbers, pageText, percentagePointChange, percentChange, presetRange, previousPeriod, rangeDays, rowRate, validateRange } from '../utils/reports.utils'
import './ReportsPage.css'

type Preset = typeof REPORT_PRESETS[number]
const PRESET_LABELS: Record<Preset, string> = { last7: 'Last 7 days', last30: 'Last 30 days', thisMonth: 'This month' }

function utcToday() { return new Date().toISOString().slice(0, 10) }
function totals(days: DailyDeliveryCountDto[]) { return days.reduce((result, day) => ({ created: result.created + day.created, delivered: result.delivered + day.delivered, failed: result.failed + day.failed }), { created: 0, delivered: 0, failed: 0 }) }
function SectionError({ retry }: { retry: () => void }) { return <div className="reports-error" role="alert"><span>We could not load this section.</span><button type="button" onClick={retry}>Try again</button></div> }
function Skeleton({ className = '' }: { className?: string }) { return <div className={`reports-skeleton ${className}`} aria-hidden="true" /> }

function ReportsChart({ days }: { days: DailyDeliveryCountDto[] }) {
  const points = useMemo(() => niceTicks(days.flatMap((day) => [day.delivered, day.failed])), [days])
  const top = points.at(-1) ?? 1
  const stride = labelStride(days.length)
  const sum = totals(days)
  const summary = `Deliveries per day from ${formatReportDate(days[0]?.date ?? '')} to ${formatReportDate(days.at(-1)?.date ?? '')}: ${sum.delivered.toLocaleString()} delivered, ${sum.failed.toLocaleString()} failed.`
  const plotHeight = 190
  const plotTop = 14
  const plotBottom = plotTop + plotHeight
  const width = Math.max(700, days.length * 84)
  const groupWidth = width / Math.max(1, days.length)
  const barWidth = Math.max(3, Math.min(18, groupWidth * 0.25))
  return <section className="reports-panel reports-chart-panel" aria-labelledby="reports-chart-title">
    <div className="reports-section-heading"><div><h2 id="reports-chart-title">Deliveries per day</h2><p>Grouped volume by completion outcome</p></div><div className="reports-legend"><span><i className="is-delivered"/>Delivered</span><span><i className="is-failed"/>Failed</span></div></div>
    <div className="reports-chart-scroll" role="img" aria-label={summary}>
      <svg className="reports-chart" viewBox={`0 0 ${width + 48} 250`} aria-hidden="true">
        {points.map((tick) => {
          const y = plotBottom - (tick / top) * plotHeight
          return <g key={tick}><line x1="44" x2={width + 38} y1={y} y2={y} className="reports-gridline"/><text x="35" y={y + 4} textAnchor="end" className="reports-axis-label">{tick.toLocaleString()}</text></g>
        })}
        {days.map((day, index) => {
          const center = 48 + groupWidth * index + groupWidth / 2
          const deliveredY = plotBottom - barHeight(day.delivered, top) / 100 * plotHeight
          const failedY = plotBottom - barHeight(day.failed, top) / 100 * plotHeight
          const completed = day.delivered + day.failed
          const label = `${formatReportDate(day.date)}: ${day.delivered} delivered, ${day.failed} failed${completed ? ` (${(day.delivered / completed * 100).toFixed(1)}%)` : ''}`
          return <g key={day.date} tabIndex={0} aria-label={label} className="reports-day-group">
            <title>{label}</title>
            <rect x={center - barWidth - 2} y={deliveredY} width={barWidth} height={plotBottom - deliveredY} rx="3" className="reports-delivered-bar"/>
            <rect x={center + 2} y={failedY} width={barWidth} height={plotBottom - failedY} rx="3" className="reports-failed-bar"/>
            {(index % stride === 0 || index === days.length - 1) && <text x={center} y="230" textAnchor="middle" className="reports-day-label">{formatReportDate(day.date)}</text>}
          </g>
        })}
      </svg>
    </div>
    <p className="reports-chart-note">{summary}</p>
  </section>
}

function StatCard({ title, value, caption, icon, tone = '', captionTone = '', loading = false }: { title: string; value: string; caption: string; icon: string; tone?: string; captionTone?: string; loading?: boolean }) {
  return <section className={`reports-stat-card ${tone}`} aria-label={title} aria-busy={loading}>
    <div className="reports-stat-heading"><span>{title}</span><span className="reports-stat-icon" aria-hidden="true">{icon}</span></div>
    {loading ? <Skeleton className="reports-stat-value"/> : <strong className="reports-stat-value">{value}</strong>}
    <p className={captionTone}>{caption}</p>
  </section>
}

export function ReportsPage() {
  const [range, setRange] = useState(() => presetRange('last7'))
  const [selectedPreset, setSelectedPreset] = useState<Preset | null>('last7')
  const [page, setPage] = useState(1)
  const [exportError, setExportError] = useState(false)
  const today = utcToday()
  const validity = validateRange(range, today)
  const valid = validity === 'valid'
  const previous = useMemo(() => valid ? previousPeriod(range) : { from: '', to: '' }, [range, valid])
  const daysQuery = useDeliveriesPerDay(range.from, range.to, valid)
  const rateQuery = useSuccessRate(range.from, range.to, valid)
  const previousRateQuery = useSuccessRate(previous.from, previous.to, valid)
  const days = daysQuery.data ?? []
  const total = totals(days)
  const isEmpty = !daysQuery.isLoading && !daysQuery.isError && (days.length === 0 || isEmptyReport(days))
  const dayCount = valid ? rangeDays(range) : 0
  const pageCount = Math.max(1, Math.ceil(days.length / REPORT_TABLE_PAGE_SIZE))
  const visibleDays = [...days].sort((a, b) => b.date.localeCompare(a.date)).slice((page - 1) * REPORT_TABLE_PAGE_SIZE, page * REPORT_TABLE_PAGE_SIZE)
  const rangeLabel = dayCount === 1 ? 'previous day' : `previous ${dayCount} days`
  const errorStatus = (error: unknown) => typeof error === 'object' && error !== null && 'response' in error ? (error as { response?: { status?: number } }).response?.status : undefined
  const rangeError = validity === 'reversed' || validity === 'too-long' || validity === 'empty' || validity === 'future' || [errorStatus(daysQuery.error), errorStatus(rateQuery.error)].includes(400)
  const rangeErrorContent = validity === 'reversed'
    ? 'Invalid Date Range Selected. The start date must be on or before the end date. Please adjust your selection to continue reporting.'
    : validity === 'too-long'
      ? `Range Exceeds Limit. The date range cannot be longer than ${REPORT_MAX_RANGE_DAYS} days.`
      : validity === 'future' ? 'Invalid Date Range Selected. Dates cannot be later than today (UTC).'
        : validity === 'empty' ? 'Select both a start date and an end date to view reports.'
          : 'Invalid Date Range Selected. Please adjust the dates to continue reporting.'

  function setPreset(preset: Preset) { setRange(presetRange(preset)); setSelectedPreset(preset); setPage(1) }
  function editDate(which: 'from' | 'to', value: string) { setRange((current) => ({ ...current, [which]: value })); setSelectedPreset(null); setPage(1) }
  async function refresh() { await Promise.all([daysQuery.refetch(), rateQuery.refetch(), previousRateQuery.refetch()]) }
  function exportCsv() {
    try {
      setExportError(false)
      const blob = new Blob([buildCsv(days)], { type: 'text/csv;charset=utf-8' })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = csvFileName(range)
      link.click()
      window.setTimeout(() => URL.revokeObjectURL(url), 0)
    } catch { setExportError(true) }
  }

  const currentRate = rateQuery.data?.successRate ?? null
  const previousRate = previousRateQuery.data?.successRate ?? null
  const currentRateTotals = rateQuery.data
  const previousTotals = previousRateQuery.data
  const noRows = days.length === 0
  const loading = daysQuery.isLoading || rateQuery.isLoading
  const countsFromRate = currentRateTotals ? { delivered: currentRateTotals.deliveredCount, failed: currentRateTotals.failedCount } : total
  const previousCountFor = (field: 'deliveredCount' | 'failedCount') => previousTotals?.[field] ?? 0

  return <main className="reports-page">
    <header className="reports-header reports-panel">
      <div><p className="reports-eyebrow">OPERATIONAL ANALYTICS</p><h1>Delivery Reports &amp; Performance</h1></div>
      {USE_MOCKS && import.meta.env.DEV && <span className="reports-sample-tag">Sample data</span>}
    </header>
    <section className="reports-controls reports-panel" aria-label="Report date range">
      <div className="reports-control-top">
        <div className="reports-presets" role="radiogroup" aria-label="Date range preset" onKeyDown={(event) => {
          if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key)) return
          event.preventDefault()
          const current = Math.max(0, REPORT_PRESETS.indexOf(selectedPreset ?? 'last7'))
          const next = (current + (event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? REPORT_PRESETS.length - 1 : 1)) % REPORT_PRESETS.length
          setPreset(REPORT_PRESETS[next])
          document.getElementById(`reports-preset-${REPORT_PRESETS[next]}`)?.focus()
        }}>
          {REPORT_PRESETS.map((preset) => <button id={`reports-preset-${preset}`} type="button" role="radio" aria-checked={selectedPreset === preset} className={selectedPreset === preset ? 'is-selected' : ''} key={preset} onClick={() => setPreset(preset)}>{PRESET_LABELS[preset]}</button>)}
        </div>
        <div className="reports-actions"><button type="button" className="reports-secondary-button" onClick={() => void refresh()} disabled={!valid || daysQuery.isFetching || rateQuery.isFetching}><span className={daysQuery.isFetching || rateQuery.isFetching ? 'reports-refresh-icon is-spinning' : 'reports-refresh-icon'} aria-hidden="true">↻</span>Refresh</button><button type="button" className="reports-primary-button" onClick={exportCsv} disabled={!valid || loading || isEmpty || noRows}>⇩ Export CSV</button></div>
      </div>
      <div className="reports-date-controls">
        <label><span>From</span><input type="date" value={range.from} max={today} aria-invalid={validity !== 'valid'} aria-describedby={rangeError ? 'reports-range-error' : undefined} onChange={(event) => editDate('from', event.target.value)}/></label>
        <span className="reports-date-to">to</span>
        <label><span>To</span><input type="date" value={range.to} max={today} aria-invalid={validity !== 'valid'} aria-describedby={rangeError ? 'reports-range-error' : undefined} onChange={(event) => editDate('to', event.target.value)}/></label>
        <small>Days are counted in UTC.</small>
      </div>
    </section>
    {rangeError ? <div className="reports-range-banner" id="reports-range-error" role="alert">{rangeErrorContent}</div> : <>
      <div className="reports-stat-grid" aria-busy={loading}>
        {rateQuery.isError ? <div className="reports-stat-error"><SectionError retry={() => void rateQuery.refetch()}/></div> : <>
          <StatCard title="Delivered" value={countsFromRate.delivered.toLocaleString()} icon="✓" caption={isEmpty ? 'No data recorded' : `${percentChange(countsFromRate.delivered, previousCountFor('deliveredCount')) ? `${percentChange(countsFromRate.delivered, previousCountFor('deliveredCount'))} · ` : ''}vs ${rangeLabel}`} loading={rateQuery.isLoading} />
          <StatCard title="Failed Attempts" value={countsFromRate.failed.toLocaleString()} icon="×" tone="is-failed" captionTone={countsFromRate.failed < previousCountFor('failedCount') ? 'is-good' : countsFromRate.failed > previousCountFor('failedCount') && previousCountFor('failedCount') > 0 ? 'is-bad' : ''} caption={isEmpty ? 'No data recorded' : `${percentChange(countsFromRate.failed, previousCountFor('failedCount')) ? `${percentChange(countsFromRate.failed, previousCountFor('failedCount'))} ${countsFromRate.failed <= previousCountFor('failedCount') ? 'improvement' : 'increase'} · ` : ''}vs ${rangeLabel}`} loading={rateQuery.isLoading} />
          <StatCard title="Total Completed" value={(countsFromRate.delivered + countsFromRate.failed).toLocaleString()} icon="▣" tone="is-total" caption={isEmpty ? 'No data recorded' : 'Delivered + failed'} loading={rateQuery.isLoading} />
          <StatCard title="Success Rate" value={formatRate(currentRate)} icon="◉" tone="is-success" caption={isEmpty ? '0 completed deliveries' : `${percentagePointChange(currentRate, previousRate) ? `${percentagePointChange(currentRate, previousRate)} · ` : ''}vs ${rangeLabel}`} loading={rateQuery.isLoading} />
        </>}
      </div>
      {isEmpty ? <section className="reports-empty reports-panel"><span aria-hidden="true">▦</span><h2>No deliveries in this period</h2><p>There are no delivery records for the selected dates. Try adjusting your range or selecting a recent preset.</p>{selectedPreset === 'last7' && range.from === presetRange('last7').from && range.to === presetRange('last7').to ? <button type="button" className="reports-secondary-button" onClick={() => void refresh()}>Refresh</button> : <button type="button" className="reports-primary-button" onClick={() => setPreset('last7')}>Reset to Last 7 Days</button>}</section> : <>
        <section className="reports-panel reports-chart-panel" aria-busy={daysQuery.isLoading}>
          {daysQuery.isLoading ? <><div className="reports-section-heading"><div><h2>Deliveries per day</h2><p>Grouped volume by completion outcome</p></div></div><Skeleton className="reports-chart-skeleton"/></> : daysQuery.isError ? <SectionError retry={() => void daysQuery.refetch()}/> : days.length ? <ReportsChart days={days}/> : <div className="reports-empty-inline">No daily results were returned for this range.</div>}
        </section>
        <section className="reports-panel reports-table-panel" aria-labelledby="reports-table-title" aria-busy={daysQuery.isLoading}>
          <div className="reports-table-heading"><div><h2 id="reports-table-title">Daily Breakdown</h2><p>Daily counts for the selected period</p></div><span>{days.length} of {days.length} days displayed</span></div>
          {daysQuery.isLoading ? <div className="reports-table-scroll"><table className="reports-table"><tbody>{Array.from({ length: REPORT_TABLE_PAGE_SIZE }, (_, index) => <tr key={index}><td colSpan={5}><Skeleton className="reports-row-skeleton"/></td></tr>)}</tbody></table></div> : daysQuery.isError ? <SectionError retry={() => void daysQuery.refetch()}/> : <>
            <div className="reports-table-scroll"><table className="reports-table"><thead><tr><th scope="col">Date</th><th scope="col">Created</th><th scope="col">Delivered</th><th scope="col">Failed</th><th scope="col">Success Rate</th></tr></thead><tbody>{visibleDays.map((day) => <tr key={day.date}><td>{day.date}</td><td>{day.created.toLocaleString()}</td><td className="reports-delivered-count">{day.delivered.toLocaleString()}</td><td className="reports-failed-count">{day.failed.toLocaleString()}</td><td>{rowRate(day)}</td></tr>)}</tbody></table></div>
            <footer className="reports-table-footer"><span>{pageText(page, REPORT_TABLE_PAGE_SIZE, days.length)}</span><nav aria-label="Pagination"><button type="button" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>Previous</button>{pageNumbers(page, pageCount).map((number) => <button type="button" key={number} aria-current={number === page ? 'page' : undefined} className={number === page ? 'is-current' : ''} onClick={() => setPage(number)}>{number}</button>)}<button type="button" disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}>Next</button></nav></footer>
          </>}
        </section>
      </>}
      {exportError && <p className="reports-export-error" role="alert">The CSV file could not be created. Try again.</p>}
    </>}
  </main>
}
