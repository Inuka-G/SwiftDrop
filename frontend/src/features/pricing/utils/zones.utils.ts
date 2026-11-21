import type { ZoneDto } from '../types/zones.types'

export type ZoneStatusFilter = 'all' | 'active' | 'inactive'

export function zoneInitials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean)
  if (words.length === 0) return '?'
  if (words.length === 1) return [...words[0]].slice(0, 2).join('').toUpperCase()
  return words.slice(0, 2).map((word) => [...word][0]).join('').toUpperCase()
}

export function filterZones(zones: ZoneDto[], search: string, status: ZoneStatusFilter): ZoneDto[] {
  const term = search.trim().toLocaleLowerCase()
  return zones.filter((zone) => (!term || zone.zoneName.toLocaleLowerCase().includes(term))
    && (status === 'all' || zone.isActive === (status === 'active')))
}

export function zoneStats(zones: ZoneDto[]) {
  const active = zones.filter((zone) => zone.isActive).length
  return { total: zones.length, active, inactive: zones.length - active }
}

export function zoneErrorMessage(status?: number): string {
  if (status === 400) return 'Check the highlighted fields.'
  if (status === 403) return "You don't have permission to manage zones."
  if (status === 404) return 'This zone no longer exists.'
  if (status === 409) return 'A zone with this name already exists.'
  return "Couldn't complete the request. Try again."
}

export function getHttpStatus(error: unknown): number | undefined {
  if (typeof error !== 'object' || error === null || !('response' in error)) return undefined
  const response = (error as { response?: { status?: number } }).response
  return response?.status
}
