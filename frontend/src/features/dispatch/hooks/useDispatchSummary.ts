import { useQuery } from '@tanstack/react-query'
import { DASHBOARD_REFRESH_INTERVAL_MS, USE_MOCKS } from '../../../lib/constants'
import { getDispatchSummary, getUnassignedParcels } from '../api/dispatch.api'
import { couriersWithActiveDeliveriesMock, dispatchSummaryMock, unassignedCountMock } from '../dispatch.mocks'
import type { DispatchDashboardSummary } from '../types/dispatch.types'

export function useDispatchSummary() {
  return useQuery<DispatchDashboardSummary>({
    queryKey: ['dispatch', 'summary'],
    queryFn: async () => {
      if (USE_MOCKS) return { ...dispatchSummaryMock, unassignedCount: unassignedCountMock, couriersWithActiveDeliveries: couriersWithActiveDeliveriesMock }
      const [summary, unassigned] = await Promise.all([getDispatchSummary(), getUnassignedParcels()])
      return { ...summary, unassignedCount: unassigned.totalCount, couriersWithActiveDeliveries: null }
    },
    refetchInterval: DASHBOARD_REFRESH_INTERVAL_MS,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}
