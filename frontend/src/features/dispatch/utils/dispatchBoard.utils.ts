import { DISPATCH_OVERDUE_MINUTES } from '../../../lib/constants'
import type { CourierDto, DispatchFilters, UnassignedParcelDto } from '../types/dispatch.types'

export type PageItem = number | 'ellipsis'

export function waitingMinutes(createdAt: string, now = Date.now()): number {
  return Math.max(0, Math.floor((now - new Date(createdAt).getTime()) / 60_000))
}

export function isOverdue(createdAt: string, now = Date.now()): boolean {
  return waitingMinutes(createdAt, now) > DISPATCH_OVERDUE_MINUTES
}

export function formatWaitingSince(createdAt: string, now = Date.now()): string {
  const minutes = waitingMinutes(createdAt, now)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  const remaining = minutes % 60
  return remaining ? `${hours} h ${remaining}m ago` : `${hours} h ago`
}

export function formatCreatedAt(createdAt: string, now = new Date()): string {
  const date = new Date(createdAt)
  const time = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' }).format(date)
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const yesterday = new Date(today)
  yesterday.setDate(yesterday.getDate() - 1)
  const dateOnly = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  if (dateOnly.getTime() === today.getTime()) return `${time} today`
  if (dateOnly.getTime() === yesterday.getTime()) return `${time} yesterday`
  const day = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(date)
  return `${day}, ${time}`
}

export function getShowingRange(page: number, pageSize: number, total: number): string {
  const start = total ? (page - 1) * pageSize + 1 : 0
  const end = Math.min(page * pageSize, total)
  return `Showing ${start}-${end} of ${total} parcels`
}

export function getPageItems(page: number, pageCount: number): PageItem[] {
  if (pageCount <= 5) return Array.from({ length: pageCount }, (_, index) => index + 1)
  if (page <= 3) return [1, 2, 3, 4, 'ellipsis', pageCount]
  if (page >= pageCount - 2) return [1, 'ellipsis', pageCount - 3, pageCount - 2, pageCount - 1, pageCount]
  return [1, 'ellipsis', page - 1, page, page + 1, 'ellipsis', pageCount]
}

export function isDateRangeValid(filters: DispatchFilters): boolean {
  return !filters.from || !filters.to || filters.from <= filters.to
}

function localDayStartIso(date: string): string {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day, 0, 0, 0, 0).toISOString()
}

function localDayEndIso(date: string): string {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day, 23, 59, 59, 999).toISOString()
}

export function filtersToQueryParams(filters: DispatchFilters, page: number, pageSize: number) {
  return {
    ...(filters.from ? { from: localDayStartIso(filters.from) } : {}),
    ...(filters.to ? { to: localDayEndIso(filters.to) } : {}),
    page,
    pageSize,
  }
}

export function lastValidPage(total: number, pageSize: number): number {
  return Math.max(1, Math.ceil(total / pageSize))
}

export function shortId(value: string | null | undefined): string {
  if (!value) return 'Unassigned zone'
  return value.length > 8 ? value.slice(0, 8) : value
}

export function parcelZoneLabel(parcel: UnassignedParcelDto): string {
  return shortId(parcel.zoneId)
}

export function courierName(courier: CourierDto): string {
  return courier.name?.trim() || shortId(courier.courierId)
}

export function filterCouriers(couriers: CourierDto[], query: string): CourierDto[] {
  const needle = query.trim().toLocaleLowerCase()
  if (!needle) return couriers
  return couriers.filter((courier) => `${courierName(courier)} ${shortId(courier.zoneId)}`.toLocaleLowerCase().includes(needle))
}

export function sortCouriers(couriers: CourierDto[], mostActiveFirst: boolean): CourierDto[] {
  return [...couriers].sort((a, b) => {
    const loadOrder = a.activeDeliveryCount - b.activeDeliveryCount
    return (mostActiveFirst ? -loadOrder : loadOrder) || courierName(a).localeCompare(courierName(b))
  })
}

export function isZoneMatch(parcel: UnassignedParcelDto, courier: CourierDto): boolean {
  return Boolean(parcel.zoneId && courier.zoneId && parcel.zoneId.toLowerCase() === courier.zoneId.toLowerCase())
}

export function vehicleGlyph(vehicleType: string | null): string {
  switch (vehicleType?.trim().toLowerCase()) {
    case 'motorbike': return '◉'
    case 'three-wheeler': return '▰'
    case 'light van': return '▣'
    default: return '◇'
  }
}

export function loadRatio(activeDeliveries: number, maxActiveDeliveries: number): number {
  return Math.min(100, Math.round((activeDeliveries / Math.max(1, maxActiveDeliveries)) * 100))
}

export function loadBand(ratio: number): 'blue' | 'orange' | 'brown' {
  if (ratio <= 40) return 'blue'
  if (ratio <= 75) return 'orange'
  return 'brown'
}

export function activeDeliveryLabel(count: number): string {
  return `${count} active ${count === 1 ? 'delivery' : 'deliveries'}`
}

export function assignmentErrorKind(status: number | undefined): 'conflict' | 'not-found' | 'ineligible' | 'generic' {
  if (status === 409) return 'conflict'
  if (status === 404) return 'not-found'
  if (status === 400 || status === 403) return 'ineligible'
  return 'generic'
}
