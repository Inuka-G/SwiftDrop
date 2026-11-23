import { apiClient } from '../../../lib/axios'
import type { CreateZoneRateRequest, CreateZoneRequest, UpdateZoneRateRequest, UpdateZoneRequest, UpdateZoneStatusRequest, ZoneDto, ZoneRateApiDto, ZoneRateDto } from '../types/zones.types'

function normalizePricingRule(rule: ZoneRateApiDto): ZoneRateDto {
  const serviceType = rule.serviceType === 1 || rule.serviceType === 'Standard'
    ? 'Standard'
    : rule.serviceType === 2 || rule.serviceType === 'Express'
      ? 'Express'
      : null

  if (!serviceType) {
    throw new Error(`Unsupported pricing service type: ${String(rule.serviceType)}`)
  }

  return { ...rule, serviceType }
}

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
    const response = await apiClient.put<ZoneRateApiDto>(`/zones/${zoneId}/pricing-rules/${ruleId}`, request)
    return normalizePricingRule(response.data)
  },
  async createRate(zoneId: string, request: CreateZoneRateRequest): Promise<ZoneRateDto> {
    const response = await apiClient.post<ZoneRateApiDto>(`/zones/${zoneId}/pricing-rules`, request)
    return normalizePricingRule(response.data)
  },
  async getPricingRules(zoneId: string): Promise<ZoneRateDto[]> {
    const response = await apiClient.get<ZoneRateApiDto[]>(
      `/zones/${zoneId}/pricing-rules`,
    )

    return response.data.map(normalizePricingRule)
  },
}
