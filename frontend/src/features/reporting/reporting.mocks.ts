import type { DailyDeliveryCountDto, DeliveriesPerDayDto, ReportingSummaryDto } from './types/reporting.types'

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

export function getDeliveriesPerDayMock(now = new Date()): DeliveriesPerDayDto {
  const today = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()))
  const days: DailyDeliveryCountDto[] = weeklyValues.map((counts, index) => {
    const date = new Date(today)
    date.setUTCDate(today.getUTCDate() - (6 - index))
    return { date: utcDate(date), createdCount: counts.deliveredCount + counts.failedCount, ...counts }
  })
  return { from: days[0].date, to: days[days.length - 1].date, days }
}

export function getReportSummaryMock(now = new Date()): ReportingSummaryDto {
  return { date: utcDate(now), createdCount: 180, deliveredCount: 172, failedCount: 8, successRate: 0.955 }
}
