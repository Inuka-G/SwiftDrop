import { apiClient } from '../../../lib/axios'
import { USE_MOCKS } from '../../../lib/constants'
import type { AssignmentDto, CourierDto, CourierPageDto, CourierWorkloadPageDto, DispatchDashboardSummary, DispatchFilters, RegisterCourierRequest, UnassignedParcelPageDto, UpdateCourierRequest } from '../types/dispatch.types'
import { assignMockParcel, deleteMockCourier, getMockCourierPage, getMockCourierWorkloadPage, getMockUnassigned, registerMockCourier, updateMockCourier } from './dispatch.mocks'
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

export async function getCourierWorkloadPage(): Promise<CourierWorkloadPageDto> {
  return USE_MOCKS
    ? getMockCourierWorkloadPage(100)
    : (await apiClient.get<CourierWorkloadPageDto>('/dispatch/couriers', { params: { page: 1, pageSize: 100 } })).data
}

export async function registerCourier(request: RegisterCourierRequest): Promise<CourierDto> {
  if (USE_MOCKS) return registerMockCourier(request)
  const { data } = await apiClient.post<CourierDto>('/dispatch/couriers', {
    courierId: request.userId,
    ...(request.zoneId ? { zoneId: request.zoneId } : {}),
    ...(request.vehicleType ? { vehicleType: request.vehicleType } : {}),
    ...(request.licensePlate ? { licensePlate: request.licensePlate } : {}),
  })
  return data
}

export async function updateCourier(courierId: string, request: UpdateCourierRequest): Promise<CourierDto> {
  if (USE_MOCKS) return updateMockCourier(courierId, request)
  const { data } = await apiClient.patch<CourierDto>(`/dispatch/couriers/${courierId}`, request)
  return data
}

export async function deleteCourier(courierId: string): Promise<void> {
  if (USE_MOCKS) return deleteMockCourier(courierId)
  await apiClient.delete(`/dispatch/couriers/${courierId}`)
}

export async function assignParcel(parcelId: string, courierId: string): Promise<AssignmentDto> {
  if (USE_MOCKS) return assignMockParcel(parcelId, courierId)
  const { data } = await apiClient.post<AssignmentDto>(`/dispatch/parcels/${parcelId}/assign`, { courierId })
  return data
}
