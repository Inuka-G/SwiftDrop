import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useObjectUrl } from '../../../hooks/useObjectUrl'
import { courierApi } from '../api/courierApi'
import type {
  ConfirmDeliveryInput,
  CourierDelivery,
  ParcelStatus,
  PhotoKind,
  ReportFailureInput,
} from '../types/courier.types'

// Polling keeps the list fresh (for example when a dispatcher assigns a new parcel)
// until the Realtime module's SignalR hub is available.
const REFRESH_MS = 30_000

export const courierKeys = {
  lists: ['courier', 'deliveries'] as const,
  list: (closedSince: string) => ['courier', 'deliveries', closedSince] as const,
  delivery: (id: string) => ['courier', 'delivery', id] as const,
  history: (id: string) => ['courier', 'history', id] as const,
  proof: (id: string) => ['courier', 'proof', id] as const,
  failure: (id: string) => ['courier', 'failure', id] as const,
  photo: (kind: PhotoKind, id: string) => ['courier', 'photo', kind, id] as const,
}

/** Midnight in the courier's own time zone, as an ISO string the API can compare against. */
function startOfLocalDay(): string {
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString()
}

/** Today's work: open deliveries plus those closed since local midnight, in pickup order. */
export function useCourierDeliveries() {
  const [closedSince] = useState(startOfLocalDay)
  return useQuery({
    queryKey: courierKeys.list(closedSince),
    queryFn: () => courierApi.listDeliveries(closedSince),
    refetchInterval: REFRESH_MS,
    refetchOnWindowFocus: true,
  })
}

/** One delivery, shown straight away from the cached list and then refreshed from the server. */
export function useCourierDelivery(id: string) {
  const queryClient = useQueryClient()
  return useQuery({
    queryKey: courierKeys.delivery(id),
    queryFn: () => courierApi.getDelivery(id),
    initialData: () =>
      queryClient
        .getQueriesData<CourierDelivery[]>({ queryKey: courierKeys.lists })
        .flatMap(([, list]) => list ?? [])
        .find((d) => d.id === id),
  })
}

export function useParcelHistory(id: string) {
  return useQuery({ queryKey: courierKeys.history(id), queryFn: () => courierApi.getHistory(id) })
}

export function useDeliveryProof(id: string, enabled: boolean) {
  return useQuery({ queryKey: courierKeys.proof(id), queryFn: () => courierApi.getProof(id), enabled })
}

export function useFailureReport(id: string, enabled: boolean) {
  return useQuery({ queryKey: courierKeys.failure(id), queryFn: () => courierApi.getFailureReport(id), enabled })
}

/** A displayable URL for a stored photo, or null while it loads or when there is none. */
export function usePhotoUrl(kind: PhotoKind, id: string, enabled: boolean): string | null {
  const { data } = useQuery({
    queryKey: courierKeys.photo(kind, id),
    queryFn: () => courierApi.getPhoto(kind, id),
    enabled,
    staleTime: Infinity,
  })
  return useObjectUrl(enabled ? data : null)
}

/** Puts the server's updated delivery into every cache that holds it and refreshes its history. */
function useApplyDeliveryUpdate() {
  const queryClient = useQueryClient()
  return (updated: CourierDelivery) => {
    queryClient.setQueriesData<CourierDelivery[]>({ queryKey: courierKeys.lists }, (list) =>
      list?.map((d) => (d.id === updated.id ? updated : d)),
    )
    queryClient.setQueryData(courierKeys.delivery(updated.id), updated)
    void queryClient.invalidateQueries({ queryKey: courierKeys.history(updated.id) })
  }
}

export function useUpdateStatus() {
  const apply = useApplyDeliveryUpdate()
  return useMutation({
    mutationFn: (vars: { id: string; status: ParcelStatus; note?: string }) =>
      courierApi.updateStatus(vars.id, vars.status, vars.note),
    onSuccess: apply,
  })
}

export function useConfirmDelivery() {
  const apply = useApplyDeliveryUpdate()
  return useMutation({
    mutationFn: (vars: { id: string; input: ConfirmDeliveryInput }) =>
      courierApi.confirmDelivery(vars.id, vars.input),
    onSuccess: apply,
  })
}

export function useReportFailure() {
  const apply = useApplyDeliveryUpdate()
  return useMutation({
    mutationFn: (vars: { id: string; input: ReportFailureInput }) =>
      courierApi.reportFailure(vars.id, vars.input),
    onSuccess: apply,
  })
}
