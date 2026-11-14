import type { DailyDeliveryCountDto } from '../types/reporting.types'

export function formatPercent(value: number | null | undefined): string {
  return value == null ? 'No data yet' : `${(value * 100).toFixed(1)}%`
}

export function formatCount(value: number): string {
  return new Intl.NumberFormat('en-US').format(value)
}

export function getWeeklyTotals(days: DailyDeliveryCountDto[]) {
  return days.reduce((totals, day) => ({ delivered: totals.delivered + day.delivered, failed: totals.failed + day.failed }), { delivered: 0, failed: 0 })
}

export function getRateDifference(today?: DailyDeliveryCountDto, yesterday?: DailyDeliveryCountDto): number | null {
  if (!today || !yesterday) return null
  const todayTotal = today.delivered + today.failed
  const yesterdayTotal = yesterday.delivered + yesterday.failed
  if (!todayTotal || !yesterdayTotal) return null
  return (today.delivered / todayTotal - yesterday.delivered / yesterdayTotal) * 100
}

export function getLoadPerCourier(activeDeliveries: number, couriers: number | null): number | null {
  return couriers ? activeDeliveries / couriers : null
}

export function getBarHeight(value: number, max: number): number {
  return max > 0 ? Math.max(2, (value / max) * 100) : 0
}

export function getDayLabel(date: string): string {
  return new Intl.DateTimeFormat('en-US', { weekday: 'short', timeZone: 'UTC' }).format(new Date(`${date}T00:00:00Z`))
}

export function getRelativeUpdatedAt(timestamp: number, now = Date.now()): string {
  const seconds = Math.max(0, Math.floor((now - timestamp) / 1000))
  if (seconds < 10) return 'Just now'
  if (seconds < 60) return `${seconds} sec ago`
  const minutes = Math.floor(seconds / 60)
  return `${minutes} min ago`
}
