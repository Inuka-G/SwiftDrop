/** DTO returned by GET /api/v1/zones. */
export interface ZoneDto {
  id: string
  zoneName: string
  description: string | null
  isActive: boolean
}

/** Pricing rule fields returned by the Pricing module when the rule-list contract is available. */
export interface ZoneRateDto {
  id: string
  zoneId: string
  baseRate: number
  additionalKgRate: number
  serviceType: 'Standard' | 'Express'
  isActive: boolean
}

/** Wire format returned by ASP.NET, where ServiceType is serialized as its enum number. */
export interface ZoneRateApiDto extends Omit<ZoneRateDto, 'serviceType'> {
  serviceType: number | 'Standard' | 'Express'
}

/** Stable cross-feature projection for dispatch and courier zone selectors. */
export interface Zone {
  id: string
  name: string
  isActive: boolean
}

export interface CreateZoneRequest {
  zoneName: string
  description: string | null
}

export interface UpdateZoneRequest extends CreateZoneRequest {}

export interface UpdateZoneStatusRequest {
  isActive: boolean
}

export interface UpdateZoneRateRequest {
  baseRate: number
  additionalKgRate: number
}

export interface CreateZoneRateRequest extends UpdateZoneRateRequest {
  serviceType: 1 | 2
  isActive: boolean
}
