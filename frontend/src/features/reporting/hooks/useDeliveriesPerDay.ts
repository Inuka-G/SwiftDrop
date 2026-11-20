import { useQuery } from '@tanstack/react-query'
import { DASHBOARD_CHART_DAYS, DASHBOARD_REFRESH_INTERVAL_MS, USE_MOCKS } from '../../../lib/constants'
import { getDeliveriesPerDay } from '../api/reporting.api'
import { getDeliveriesPerDayMock } from '../reporting.mocks'

export function getUtcRange(now = new Date()) {
  const toDate = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()))
  const fromDate = new Date(toDate)
  fromDate.setUTCDate(fromDate.getUTCDate() - (DASHBOARD_CHART_DAYS - 1))
  return { from: fromDate.toISOString().slice(0, 10), to: toDate.toISOString().slice(0, 10) }
}

export function useDeliveriesPerDay(from?: string, to?: string, enabled = true) {
  const dashboardRange = from == null || to == null ? getUtcRange() : null
  const rangeFrom = from ?? dashboardRange!.from
  const rangeTo = to ?? dashboardRange!.to
  const isDashboardQuery = from == null || to == null
  return useQuery({
    queryKey: ['reporting', 'deliveriesPerDay', rangeFrom, rangeTo],
    queryFn: () => USE_MOCKS ? Promise.resolve(getDeliveriesPerDayMock(rangeFrom, rangeTo)) : getDeliveriesPerDay(rangeFrom, rangeTo),
    enabled,
    refetchInterval: isDashboardQuery ? DASHBOARD_REFRESH_INTERVAL_MS : false,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}
