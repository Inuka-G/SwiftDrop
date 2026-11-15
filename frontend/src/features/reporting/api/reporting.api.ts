import { apiClient } from '../../../lib/axios'
import type { DeliveriesPerDayDto, ReportingSummaryDto } from '../types/reporting.types'

export async function getReportingSummary(): Promise<ReportingSummaryDto> {
  const { data } = await apiClient.get<ReportingSummaryDto>('/reports/summary')
  return data
}

export async function getDeliveriesPerDay(from: string, to: string): Promise<DeliveriesPerDayDto> {
  const { data } = await apiClient.get<DeliveriesPerDayDto>('/reports/deliveries-per-day', { params: { from, to } })
  return data
}
