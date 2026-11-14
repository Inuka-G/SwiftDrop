import { apiClient } from '../../../lib/axios'
import type { DispatchDashboardSummary, UnassignedParcelPageDto } from '../types/dispatch.types'

export async function getDispatchSummary(): Promise<DispatchDashboardSummary> {
  const { data } = await apiClient.get<DispatchDashboardSummary>('/dispatch/summary')
  return data
}

export async function getUnassignedParcels(): Promise<UnassignedParcelPageDto> {
  const { data } = await apiClient.get<UnassignedParcelPageDto>('/dispatch/parcels/unassigned', {
    params: { page: 1, pageSize: 1 },
  })
  return data
}
