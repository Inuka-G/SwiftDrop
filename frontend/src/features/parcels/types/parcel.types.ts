export interface CreateParcelRequest {
  zoneId: string
  pickupAddress: string
  pickupContactName: string
  pickupContactPhone: string
  dropoffAddress: string
  recipientName: string
  recipientPhone: string
  weightKg: number
  sizeCategory: string
  paymentMethod?: string
}

export interface ParcelCreatedResponse {
  parcelId: string
  trackingNumber: string
  estimatedFee: number
  status: string
  createdAtUtc: string
}

export interface ParcelSummaryDto {
  id: string
  trackingNumber: string
  recipientName: string
  dropoffAddress: string
  weightKg: number
  estimatedFee: number
  currentStatus: string
  createdAtUtc: string
}

export interface PublicTrackingDto {
  trackingNumber: string
  currentStatus: string
  createdAtUtc: string
  updatedAtUtc: string
  destinationArea: string
}

export interface FeeEstimateDto {
  zoneId: string
  zoneName: string
  baseFee: number
  weightFee: number
  total: number
}

export interface Zone {
  id: string
  zoneName: string
  description: string | null
  isActive: boolean
}

export const STANDARD_SERVICE_TYPE = 1