import { useQuery } from '@tanstack/react-query'
import { USE_MOCKS } from '../../../lib/constants'
import { getSuccessRate } from '../api/reporting.api'
import { getSuccessRateMock } from '../reporting.mocks'

export function useSuccessRate(from: string, to: string, enabled = true) {
  return useQuery({
    queryKey: ['reporting', 'successRate', from, to],
    queryFn: () => USE_MOCKS ? Promise.resolve(getSuccessRateMock(from, to)) : getSuccessRate(from, to),
    enabled,
    placeholderData: (previous) => previous,
    retry: 1,
  })
}
