export interface DispatchSummaryDto {
  totalCouriers: number
  availableCouriers: number
  unavailableCouriers: number
  activeDeliveries: number
}

export interface UnassignedParcelPageDto {
  items: Array<{ parcelId: string; trackingNumber: string; zoneId: string; createdAt: string }>
  page: number
  pageSize: number
  totalCount: number
}

export interface DispatchDashboardSummary extends DispatchSummaryDto {
  unassignedCount: number
  couriersWithActiveDeliveries: number | null
}
