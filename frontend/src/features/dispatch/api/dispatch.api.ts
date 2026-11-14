import { apiClient } from '../../../lib/axios'
import { USE_MOCKS } from '../../../lib/constants'
import type { AssignmentDto, CourierDto, CourierPageDto, DispatchDashboardSummary, DispatchFilters, UnassignedParcelPageDto } from '../types/dispatch.types'
import { assignMockParcel, getMockCourierPage, getMockUnassigned } from './dispatch.mocks'
import { filtersToQueryParams } from '../utils/dispatchBoard.utils'

export async function getDispatchSummary(): Promise<DispatchDashboardSummary> {
  const { data } = await apiClient.get<DispatchDashboardSummary>('/dispatch/summary')
  return data
}

export async function getUnassignedParcels(): Promise<UnassignedParcelPageDto> {
  return getUnassignedParcelsPage({ from: '', to: '' }, 1, 10)
}

export async function getUnassignedParcelsPage(filters: DispatchFilters, page: number, pageSize: number): Promise<UnassignedParcelPageDto> {
  const params = filtersToQueryParams(filters, page, pageSize)
  if (USE_MOCKS) return getMockUnassigned({ from: params.from ?? '', to: params.to ?? '' }, page, pageSize)
  const { data } = await apiClient.get<UnassignedParcelPageDto>('/dispatch/parcels/unassigned', {
    params,
  })
  return data
}

export async function getCouriers(): Promise<CourierDto[]> {
  const pageSize = 100
  const first = USE_MOCKS
    ? getMockCourierPage(1, pageSize)
    : (await apiClient.get<CourierPageDto>('/dispatch/couriers', { params: { page: 1, pageSize } })).data
  const pages = Math.ceil(first.totalCount / pageSize)
  const all = [...first.items]
  for (let page = 2; page <= pages; page += 1) {
    const result = USE_MOCKS
      ? getMockCourierPage(page, pageSize)
      : (await apiClient.get<CourierPageDto>('/dispatch/couriers', { params: { page, pageSize } })).data
    all.push(...result.items)
  }
  return all
}

export async function assignParcel(parcelId: string, courierId: string): Promise<AssignmentDto> {
  if (USE_MOCKS) return assignMockParcel(parcelId, courierId)
  const { data } = await apiClient.post<AssignmentDto>(`/dispatch/parcels/${parcelId}/assign`, { courierId })
  return data
}
