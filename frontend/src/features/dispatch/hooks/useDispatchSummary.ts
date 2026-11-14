import { useQuery } from '@tanstack/react-query'
import { DASHBOARD_REFRESH_INTERVAL_MS, USE_MOCKS } from '../../../lib/constants'
import { getDispatchSummary } from '../api/dispatch.api'
import { couriersWithActiveDeliveriesMock, dispatchSummaryMock, unassignedCountMock } from '../dispatch.mocks'
import type { DispatchDashboardSummary } from '../types/dispatch.types'

export function useDispatchSummary() {
  return useQuery<DispatchDashboardSummary>({
    queryKey: ['dispatch', 'summary'],
    queryFn: async () => {
      if (USE_MOCKS) return { ...dispatchSummaryMock, unassignedCount: unassignedCountMock, couriersWithActiveDeliveries: couriersWithActiveDeliveriesMock }
      return getDispatchSummary()
    },
    refetchInterval: DASHBOARD_REFRESH_INTERVAL_MS,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}
