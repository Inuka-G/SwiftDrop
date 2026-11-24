import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { approveMerchant, getPendingMerchants, rejectMerchant } from '../api/merchantApprovals.api'

export function usePendingMerchants() {
  return useQuery({
    queryKey: ['merchants', 'pendingApprovals'],
    queryFn: getPendingMerchants,
    retry: 1,
    refetchInterval: 10_000,
  })
}

function useInvalidateApprovals() {
  const client = useQueryClient()
  return () => Promise.all([
    client.invalidateQueries({ queryKey: ['merchants', 'pendingApprovals'] }),
    client.invalidateQueries({ queryKey: ['identity', 'users'] }),
  ])
}

export function useApproveMerchant() {
  const invalidate = useInvalidateApprovals()
  return useMutation({ mutationFn: approveMerchant, onSuccess: invalidate })
}

export function useRejectMerchant() {
  const invalidate = useInvalidateApprovals()
  return useMutation({ mutationFn: rejectMerchant, onSuccess: invalidate })
}
