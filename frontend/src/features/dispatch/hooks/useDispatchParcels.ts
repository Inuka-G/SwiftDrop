import { useQuery } from '@tanstack/react-query'
import { DISPATCH_REFRESH_INTERVAL_MS } from '../../../lib/constants'
import { getDispatchParcelsPage } from '../api/dispatch.api'

export function useDispatchParcels(page = 1, pageSize = 100, assignmentState = 'ASSIGNED') {
  return useQuery({
    queryKey: ['dispatch', 'parcels', assignmentState, page, pageSize],
    queryFn: () => getDispatchParcelsPage(page, pageSize, assignmentState),
    refetchInterval: DISPATCH_REFRESH_INTERVAL_MS,
    retry: 1,
  })
}
