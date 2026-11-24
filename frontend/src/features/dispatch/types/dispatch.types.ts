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

export interface UnassignedParcelDto {
  parcelId: string
  trackingNumber: string
  zoneId: string
  createdAt: string
  sizeCategory: string | null
  weightKg: number | null
}

export interface UnassignedParcelPageDto {
  items: UnassignedParcelDto[]
  page: number
  pageSize: number
  totalCount: number
}

export interface DispatchParcelDto {
  parcelId: string
  trackingNumber: string
  status: string
  zoneId: string
  courierId: string | null
  courierName: string | null
  assignmentState: string
  createdAt: string
  assignedAt: string | null
  weightKg: number | null
  sizeCategory: string | null
}

export interface DispatchParcelPageDto {
  items: DispatchParcelDto[]
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

export interface CourierWorkloadPageDto {
  items: CourierDto[]
  page: number
  pageSize: number
  totalCount: number
}

export interface RegisterCourierRequest {
  userId: string
  zoneId?: string
  vehicleType?: string
  licensePlate?: string
}

export interface UpdateCourierRequest {
  zoneId?: string
  isAvailable?: boolean
  vehicleType?: string
  licensePlate?: string
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
