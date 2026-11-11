export interface ReportingSummaryDto {
  date: string
  createdCount: number
  deliveredCount: number
  failedCount: number
  successRate: number | null
}

export interface DailyDeliveryCountDto {
  date: string
  createdCount: number
  deliveredCount: number
  failedCount: number
}

export interface DeliveriesPerDayDto {
  from: string
  to: string
  days: DailyDeliveryCountDto[]
}
