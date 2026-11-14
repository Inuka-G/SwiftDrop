export interface DispatchSummaryDto {
  totalCouriers: number
  availableCouriers: number
  unavailableCouriers: number
  activeDeliveries: number
}

export interface DispatchDashboardSummary {
  totalCouriers: number
  availableCouriers: number
  activeDeliveries: number
  unassignedCount: number
  couriersWithActiveDeliveries: number
}

// These fields mirror the current Dispatch API. Parcel size and weight are not exposed by it.
export interface UnassignedParcelDto {
  parcelId: string
  trackingNumber: string
  zoneId: string
  createdAt: string
  sizeCategory?: string | null
  weightKg?: number | null
}

export interface UnassignedParcelPageDto {
  items: UnassignedParcelDto[]
  page: number
  pageSize: number
  totalCount: number
}

// CourierProfileDto uses Name and ActiveDeliveryCount (not the proposed displayName/activeDeliveries).
export interface CourierDto {
  courierId: string
  name: string
  zoneId: string | null
  isAvailable: boolean
  vehicleType: string | null
  licensePlate: string | null
  activeDeliveryCount: number
}

export interface CourierPageDto {
  items: CourierDto[]
  page: number
  pageSize: number
  totalCount: number
}

export interface AssignParcelRequest {
  courierId: string
}

export interface AssignmentDto {
  parcelId: string
  courierId: string
  assignedBy: string
  assignedAt: string
}

export interface DispatchFilters {
  from: string
  to: string
}

export type AssignmentErrorKind = 'conflict' | 'not-found' | 'ineligible' | 'generic'

export interface AssignmentToastData {
  parcel: UnassignedParcelDto
  courier: CourierDto
}
