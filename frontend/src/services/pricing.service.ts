import { apiClient } from '../lib/axios'

export interface Zone {
  id: string
  zoneName: string
  description: string | null
  isActive: boolean
}

export type ServiceType = 'Standard' | 'Express'

export interface FeeEstimate {
  zoneId: string
  zoneName: string
  baseFee: number
  weightFee: number
  total: number
}

export const pricingService = {
  async getZones(): Promise<Zone[]> {
    const response = await apiClient.get<Zone[]>('/zones')
    return response.data
  },

  async estimate(zoneId: string, weightKg: number, serviceType: ServiceType): Promise<FeeEstimate> {
    const response = await apiClient.get<FeeEstimate>(`/zones/${zoneId}/estimate`, {
      params: { weightKg, serviceType },
    })
    return response.data
  },
}
