import type { DailyDeliveryCountDto, DeliveriesPerDayDto, ReportingSummaryDto, SuccessFailureRateDto } from './types/reporting.types'

const weeklyValues = [
  { deliveredCount: 150, failedCount: 10 },
  { deliveredCount: 146, failedCount: 6 },
  { deliveredCount: 154, failedCount: 9 },
  { deliveredCount: 158, failedCount: 5 },
  { deliveredCount: 152, failedCount: 8 },
  { deliveredCount: 154, failedCount: 11 },
  { deliveredCount: 172, failedCount: 8 },
]

function utcDate(date: Date) {
  return date.toISOString().slice(0, 10)
}

function mockMode() {
  return new URLSearchParams(window.location.search).get('reportsMock')
}

function failFor(mode: string | null, endpoint: 'daily' | 'rate') {
  if (mode === 'bad-request') throw Object.assign(new Error('Invalid range'), { response: { status: 400 } })
  if (mode === `${endpoint}-error`) throw new Error('Mock server error')
}

export function getDeliveriesPerDayMock(from: string, to: string): DeliveriesPerDayDto {
  const mode = mockMode()
  failFor(mode, 'daily')
  const days: DailyDeliveryCountDto[] = []
  const start = new Date(`${from}T00:00:00.000Z`)
  const end = new Date(`${to}T00:00:00.000Z`)
  for (const date = new Date(start); date <= end; date.setUTCDate(date.getUTCDate() + 1)) {
    const index = Math.abs(Math.floor(date.getTime() / 86_400_000)) % weeklyValues.length
    const counts = mode === 'zero' ? { deliveredCount: 0, failedCount: 0 } : weeklyValues[index]
    days.push({ date: utcDate(date), created: counts.deliveredCount + counts.failedCount, delivered: counts.deliveredCount, failed: counts.failedCount })
  }
  return days
}

export function getSuccessRateMock(from: string, to: string): SuccessFailureRateDto {
  const mode = mockMode()
  failFor(mode, 'rate')
  const days: DailyDeliveryCountDto[] = []
  const start = new Date(`${from}T00:00:00.000Z`)
  const end = new Date(`${to}T00:00:00.000Z`)
  for (const date = new Date(start); date <= end; date.setUTCDate(date.getUTCDate() + 1)) {
    const index = Math.abs(Math.floor(date.getTime() / 86_400_000)) % weeklyValues.length
    const counts = mode === 'zero' ? { deliveredCount: 0, failedCount: 0 } : weeklyValues[index]
    days.push({ date: utcDate(date), created: counts.deliveredCount + counts.failedCount, delivered: counts.deliveredCount, failed: counts.failedCount })
  }
  const deliveredCount = days.reduce((total, day) => total + day.delivered, 0)
  const failedCount = days.reduce((total, day) => total + day.failed, 0)
  const totalCount = deliveredCount + failedCount
  return { from, to, deliveredCount, failedCount, totalCount, successRate: totalCount ? deliveredCount / totalCount : null }
}

export function getReportSummaryMock(now = new Date()): ReportingSummaryDto {
  return { date: utcDate(now), created: 180, delivered: 172, failed: 8, successRate: 0.955 }
}
