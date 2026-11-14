import { useQuery } from '@tanstack/react-query'
import { getCouriers } from '../api/dispatch.api'

export function useCouriers(enabled: boolean) {
  return useQuery({
    queryKey: ['dispatch', 'couriers'],
    queryFn: getCouriers,
    enabled,
    staleTime: 15_000,
    retry: 1,
  })
}
