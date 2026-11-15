import { REPORT_MAX_RANGE_DAYS } from '../../../lib/constants'
import type { DailyDeliveryCountDto } from '../types/reporting.types'

export type DateRange = { from: string; to: string }
export type RangeValidity = 'valid' | 'empty' | 'reversed' | 'too-long' | 'future'

function dateString(date: Date) { return date.toISOString().slice(0, 10) }
function utcDate(value: string) { return new Date(`${value}T00:00:00.000Z`) }

export function presetRange(preset: 'last7' | 'last30' | 'thisMonth', now = new Date()): DateRange {
  const today = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()))
  const from = new Date(today)
  if (preset === 'last7') from.setUTCDate(from.getUTCDate() - 6)
  if (preset === 'last30') from.setUTCDate(from.getUTCDate() - 29)
  if (preset === 'thisMonth') from.setUTCDate(1)
  return { from: dateString(from), to: dateString(today) }
}

export function previousPeriod(range: DateRange): DateRange {
  const days = Math.round((utcDate(range.to).getTime() - utcDate(range.from).getTime()) / 86_400_000) + 1
  const to = new Date(utcDate(range.from))
  to.setUTCDate(to.getUTCDate() - 1)
  const from = new Date(to)
  from.setUTCDate(from.getUTCDate() - days + 1)
  return { from: dateString(from), to: dateString(to) }
}

export function validateRange(range: DateRange, today = dateString(new Date())): RangeValidity {
  if (!range.from || !range.to) return 'empty'
  if (range.from > range.to) return 'reversed'
  if (range.to > today) return 'future'
  const days = Math.round((utcDate(range.to).getTime() - utcDate(range.from).getTime()) / 86_400_000) + 1
  return days > REPORT_MAX_RANGE_DAYS ? 'too-long' : 'valid'
}

export function rangeDays(range: DateRange) { return Math.round((utcDate(range.to).getTime() - utcDate(range.from).getTime()) / 86_400_000) + 1 }
export function percentChange(current: number, previous: number): string | null { return previous === 0 ? null : `${current >= previous ? '+' : '−'}${Math.abs((current - previous) / previous * 100).toFixed(1)}%` }
export function percentagePointChange(current: number | null, previous: number | null): string | null { return current == null || previous == null ? null : `${current >= previous ? '+' : '−'}${Math.abs((current - previous) * 100).toFixed(1)} pp` }
export function formatRate(rate: number | null) { return rate == null ? 'No data' : `${(rate * 100).toFixed(1)}%` }
export function rowRate(day: DailyDeliveryCountDto) { const total = day.delivered + day.failed; return total ? formatRate(day.delivered / total) : '—' }
export function formatReportDate(value: string, options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' }) { return new Intl.DateTimeFormat('en', { ...options, timeZone: 'UTC' }).format(utcDate(value)) }
export function isEmptyReport(days: DailyDeliveryCountDto[]) { return days.every((day) => day.created === 0 && day.delivered === 0 && day.failed === 0) }

export function niceTicks(values: number[]) {
  const max = Math.max(0, ...values)
  if (!max) return [0, 1, 2, 3, 4]
  const rough = max / 4
  const magnitude = 10 ** Math.floor(Math.log10(rough))
  const fraction = rough / magnitude
  const niceStep = (fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10) * magnitude
  const top = Math.ceil(max / niceStep) * niceStep
  return Array.from({ length: Math.round(top / niceStep) + 1 }, (_, index) => Math.round(index * niceStep))
}
export function barHeight(value: number, top: number) { return top > 0 ? Math.max(value > 0 ? 1 : 0, value / top * 100) : 0 }
export function labelStride(dayCount: number) { return dayCount <= 14 ? 1 : dayCount <= 45 ? 3 : dayCount <= 120 ? 7 : Math.ceil(dayCount / 12) }
export function pageText(page: number, pageSize: number, total: number) { return total === 0 ? 'Showing 0 entries' : `Showing ${(page - 1) * pageSize + 1} to ${Math.min(page * pageSize, total)} of ${total} entries` }
export function pageNumbers(page: number, count: number) { const start = Math.max(1, Math.min(page - 2, count - 4)); return Array.from({ length: Math.max(0, Math.min(5, count)) }, (_, index) => start + index) }
export function csvFileName(range: DateRange) { return `swiftdrop-deliveries_${range.from}_to_${range.to}.csv` }
export function buildCsv(days: DailyDeliveryCountDto[]) {
  const rows = ['date,created,delivered,failed,success_rate']
  for (const day of [...days].sort((a, b) => a.date.localeCompare(b.date))) {
    const total = day.delivered + day.failed
    rows.push(`${day.date},${day.created},${day.delivered},${day.failed},${total ? (day.delivered / total * 100).toFixed(1) : ''}`)
  }
  return rows.join('\r\n')
}
