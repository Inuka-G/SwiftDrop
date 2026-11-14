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
