import { COURIER_LICENSE_PLATE_MAX_LENGTH, COURIER_VEHICLE_TYPES } from '../../../lib/constants'
import type { CourierDto, RegisterCourierRequest, UpdateCourierRequest } from '../types/dispatch.types'

export interface CourierFormValues {
  zoneId: string
  isAvailable: boolean
  vehicleType: string
  licensePlate: string
}

export type CourierAvailabilityFilter = 'all' | 'available' | 'unavailable'
export type CourierMutationError = 'duplicate' | 'not-found' | 'ineligible' | 'conflict' | 'unauthorized' | 'generic'

export function courierInitials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean)
  if (!words.length) return '?'
  return words.length === 1 ? words[0].slice(0, 2).toLocaleUpperCase() : `${words[0][0]}${words[words.length - 1][0]}`.toLocaleUpperCase()
}

export function courierStats(couriers: CourierDto[]) {
  const total = couriers.length
  const available = couriers.filter((courier) => courier.isAvailable).length
  return {
    total,
    available,
    availablePercent: total ? Math.round((available / total) * 100) : null,
    peakWorkload: couriers.reduce((peak, courier) => Math.max(peak, courier.activeDeliveryCount), 0),
  }
}

export function filterAndSortCouriers(couriers: CourierDto[], search: string, availability: CourierAvailabilityFilter, mostActiveFirst: boolean): CourierDto[] {
  const needle = search.trim().toLocaleLowerCase()
  return couriers.filter((courier) => {
    const matchesSearch = !needle || `${courier.name} ${courier.licensePlate ?? ''}`.toLocaleLowerCase().includes(needle)
    const matchesAvailability = availability === 'all' || courier.isAvailable === (availability === 'available')
    return matchesSearch && matchesAvailability
  }).sort((a, b) => {
    const load = a.activeDeliveryCount - b.activeDeliveryCount
    return (mostActiveFirst ? -load : load) || a.name.localeCompare(b.name)
  })
}

export function normalizeLicensePlate(value: string): string {
  return value.trim().toLocaleUpperCase()
}

export function isLicensePlateValid(value: string): boolean {
  const plate = value.trim()
  return plate.length <= COURIER_LICENSE_PLATE_MAX_LENGTH && /^[A-Za-z0-9 -]*$/.test(plate)
}

export function vehicleOptions(currentValue = ''): string[] {
  return currentValue && !COURIER_VEHICLE_TYPES.includes(currentValue as typeof COURIER_VEHICLE_TYPES[number])
    ? [currentValue, ...COURIER_VEHICLE_TYPES]
    : [...COURIER_VEHICLE_TYPES]
}

export function isDuplicateCourier(userId: string, couriers: CourierDto[]): boolean {
  return couriers.some((courier) => courier.courierId === userId)
}

export function buildCourierPatch(original: CourierDto, values: CourierFormValues): UpdateCourierRequest {
  const patch: UpdateCourierRequest = {}
  if (values.zoneId && values.zoneId !== (original.zoneId ?? '')) patch.zoneId = values.zoneId
  if (values.isAvailable !== original.isAvailable) patch.isAvailable = values.isAvailable
  if (values.vehicleType !== (original.vehicleType ?? '')) patch.vehicleType = values.vehicleType
  const licensePlate = normalizeLicensePlate(values.licensePlate)
  if (licensePlate !== normalizeLicensePlate(original.licensePlate ?? '')) patch.licensePlate = licensePlate
  return patch
}

export function hasCourierChanges(original: CourierDto, values: CourierFormValues): boolean {
  return Object.keys(buildCourierPatch(original, values)).length > 0
}

export function mapCourierMutationError(status: number | undefined, action: 'register' | 'update'): CourierMutationError {
  if (status === 401 || (status === 403 && action === 'update')) return 'unauthorized'
  if (status === 409) return action === 'register' ? 'duplicate' : 'conflict'
  if (status === 404 && action === 'update') return 'not-found'
  if ((status === 400 || status === 403) && action === 'register') return 'ineligible'
  return 'generic'
}

export function registerCourierPayload(userId: string, values: CourierFormValues): RegisterCourierRequest {
  const licensePlate = normalizeLicensePlate(values.licensePlate)
  return {
    userId,
    ...(values.zoneId ? { zoneId: values.zoneId } : {}),
    ...(values.vehicleType ? { vehicleType: values.vehicleType } : {}),
    ...(licensePlate ? { licensePlate } : {}),
  }
}
