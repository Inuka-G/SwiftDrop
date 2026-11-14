import { apiClient } from '../../../lib/axios'
import {
  STANDARD_SERVICE_TYPE,
  type CreateParcelRequest,
  type ParcelCreatedResponse,
  type ParcelSummaryDto,
  type PublicTrackingDto,
  type FeeEstimateDto,
  type Zone,
} from '../types/parcel.types'

export const parcelsApi = {
  // Get all delivery zones
  async getZones(): Promise<Zone[]> {
    const response = await apiClient.get<Zone[]>('/zones')
    return response.data.filter((zone) => zone.isActive)
  },

  // Get Standard delivery fee estimate
  async estimateFee(
    zoneId: string,
    weightKg: number,
  ): Promise<FeeEstimateDto> {
    const response = await apiClient.get<FeeEstimateDto>(
      `/zones/${zoneId}/estimate`,
      {
        params: {
          weightKg,
          serviceType: STANDARD_SERVICE_TYPE,
        },
      },
    )
    return response.data
  },

  // Create a parcel booking
  async createParcel(
    request: CreateParcelRequest,
  ): Promise<ParcelCreatedResponse> {
    const response = await apiClient.post<ParcelCreatedResponse>(
      '/parcels',
      request,
    )
    return response.data
  },

  // Get logged-in customer's booking history
  async getMyHistory(): Promise<ParcelSummaryDto[]> {
    const response = await apiClient.get<ParcelSummaryDto[]>(
      '/parcels/my-history',
    )
    return response.data
  },

  // Public parcel tracking (no login required)
  async trackParcel(
    trackingNumber: string,
  ): Promise<PublicTrackingDto> {
    const response = await apiClient.get<PublicTrackingDto>(
      `/parcels/track/${encodeURIComponent(trackingNumber)}`,
    )
    return response.data
  },

  // Download a parcel receipt as PDF
  async downloadReceipt(parcelId: string): Promise<void> {
    const response = await apiClient.get(
      `/parcels/${parcelId}/receipt`,
      {
        responseType: 'blob',
      },
    )

    const blobUrl = URL.createObjectURL(response.data)
    const link = document.createElement('a')

    link.href = blobUrl
    link.download = `SwiftDrop-Receipt-${parcelId}.pdf`
    document.body.appendChild(link)
    link.click()
    link.remove()

    setTimeout(() => URL.revokeObjectURL(blobUrl), 1000)
  },
}