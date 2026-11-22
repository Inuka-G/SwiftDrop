import { apiClient } from '../../../lib/axios'
import type { DeliveriesPerDayDto, ReportingSummaryDto, SuccessFailureRateDto } from '../types/reporting.types'

export async function getReportingSummary(): Promise<ReportingSummaryDto> {
  const { data } = await apiClient.get<ReportingSummaryDto>('/reports/summary')
  return data
}

export async function getDeliveriesPerDay(from: string, to: string): Promise<DeliveriesPerDayDto> {
  const { data } = await apiClient.get<DeliveriesPerDayDto>('/reports/deliveries-per-day', { params: { from, to } })
  return data
}

export async function getSuccessRate(from: string, to: string): Promise<SuccessFailureRateDto> {
  const { data } = await apiClient.get<SuccessFailureRateDto>('/reports/success-failure-rate', { params: { from, to } })
  return data
}
