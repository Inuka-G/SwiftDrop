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

export function useDeliveriesPerDay() {
  const { from, to } = getUtcRange()
  return useQuery({
    queryKey: ['reporting', 'deliveriesPerDay', from, to],
    queryFn: () => USE_MOCKS ? Promise.resolve(getDeliveriesPerDayMock()) : getDeliveriesPerDay(from, to),
    refetchInterval: DASHBOARD_REFRESH_INTERVAL_MS,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}
