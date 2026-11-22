export interface ReportingSummaryDto {
  date: string
  created: number
  delivered: number
  failed: number
  successRate: number | null
}

export interface DailyDeliveryCountDto {
  date: string
  created: number
  delivered: number
  failed: number
}

export type DeliveriesPerDayDto = DailyDeliveryCountDto[]

// Mirrors SuccessFailureRateResponse in SwiftDrop.Reporting.Contracts.
export interface SuccessFailureRateDto {
  from: string
  to: string
  deliveredCount: number
  failedCount: number
  totalCount: number
  successRate: number | null
}
