import { apiClient } from '../../../lib/axios'
import type { CreateZoneRequest, UpdateZoneRateRequest, UpdateZoneRequest, UpdateZoneStatusRequest, ZoneDto, ZoneRateDto } from '../types/zones.types'

export const zonesApi = {
  async list(): Promise<ZoneDto[]> {
    const response = await apiClient.get<ZoneDto[]>('/zones')
    return response.data
  },
  async create(request: CreateZoneRequest): Promise<ZoneDto> {
    const response = await apiClient.post<ZoneDto>('/zones', request)
    return response.data
  },
  async update(id: string, request: UpdateZoneRequest): Promise<ZoneDto> {
    const response = await apiClient.put<ZoneDto>(`/zones/${id}`, request)
    return response.data
  },
  async setActive(id: string, request: UpdateZoneStatusRequest): Promise<ZoneDto> {
    const response = await apiClient.patch<ZoneDto>(`/zones/${id}/status`, request)
    return response.data
  },
  async updateRate(zoneId: string, ruleId: string, request: UpdateZoneRateRequest): Promise<ZoneRateDto> {
    const response = await apiClient.put<ZoneRateDto>(`/zones/${zoneId}/pricing-rules/${ruleId}`, request)
    return response.data
  },
}
