import { useMutation, useQueryClient } from '@tanstack/react-query'
import { assignParcel } from '../api/dispatch.api'

export function useAssignParcel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ parcelId, courierId }: { parcelId: string; courierId: string }) => assignParcel(parcelId, courierId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'unassigned'] }),
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'couriers'] }),
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'summary'] }),
      ])
    },
  })
}
