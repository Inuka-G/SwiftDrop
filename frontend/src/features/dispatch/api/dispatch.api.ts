import { apiClient } from '../../../lib/axios'
import type { DispatchSummaryDto, UnassignedParcelPageDto } from '../types/dispatch.types'

export async function getDispatchSummary(): Promise<DispatchSummaryDto> {
  const { data } = await apiClient.get<DispatchSummaryDto>('/dispatch/couriers/summary')
  return data
}

export async function getUnassignedParcels(): Promise<UnassignedParcelPageDto> {
  const { data } = await apiClient.get<UnassignedParcelPageDto>('/dispatch/parcels/unassigned', {
    params: { page: 1, pageSize: 1 },
  })
  return data
}
