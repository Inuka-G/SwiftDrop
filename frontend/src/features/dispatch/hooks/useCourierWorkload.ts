import { useQuery } from '@tanstack/react-query'
import { DISPATCH_REFRESH_INTERVAL_MS } from '../../../lib/constants'
import { getCourierWorkloadPage } from '../api/dispatch.api'

export function useCourierWorkload(dialogOpen: boolean) {
  return useQuery({
    queryKey: ['dispatch', 'couriers', 'workload'],
    queryFn: getCourierWorkloadPage,
    refetchInterval: dialogOpen ? false : DISPATCH_REFRESH_INTERVAL_MS,
    staleTime: 10_000,
    retry: 1,
  })
}
