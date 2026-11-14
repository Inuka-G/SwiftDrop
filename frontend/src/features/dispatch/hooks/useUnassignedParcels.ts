import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { DISPATCH_REFRESH_INTERVAL_MS } from '../../../lib/constants'
import { getUnassignedParcelsPage } from '../api/dispatch.api'
import type { DispatchFilters } from '../types/dispatch.types'
import { isDateRangeValid } from '../utils/dispatchBoard.utils'

export function useUnassignedParcels(filters: DispatchFilters, page: number, pageSize: number) {
  const valid = isDateRangeValid(filters)
  return useQuery({
    queryKey: ['dispatch', 'unassigned', filters, page, pageSize],
    queryFn: () => getUnassignedParcelsPage(filters, page, pageSize),
    enabled: valid,
    refetchInterval: DISPATCH_REFRESH_INTERVAL_MS,
    placeholderData: keepPreviousData,
    retry: 1,
  })
}
