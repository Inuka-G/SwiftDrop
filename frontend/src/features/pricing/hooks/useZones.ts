import { useQuery } from '@tanstack/react-query'
import { zonesApi } from '../api/zones.api'

export const ZONES_QUERY_KEY = ['pricing', 'zones'] as const

export function useZones() {
  return useQuery({
    queryKey: ZONES_QUERY_KEY,
    queryFn: zonesApi.list,
    select: (zones) => zones.map((zone) => ({ id: zone.id, name: zone.zoneName, isActive: zone.isActive })),
    staleTime: 30_000,
  })
}
