import { useQuery } from '@tanstack/react-query'
import { DASHBOARD_REFRESH_INTERVAL_MS, USE_MOCKS } from '../../../lib/constants'
import { getReportingSummary } from '../api/reporting.api'
import { getReportSummaryMock } from '../reporting.mocks'

export function useReportSummary() {
  return useQuery({
    queryKey: ['reporting', 'summary'],
    queryFn: () => USE_MOCKS ? Promise.resolve(getReportSummaryMock()) : getReportingSummary(),
    refetchInterval: DASHBOARD_REFRESH_INTERVAL_MS,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}
