import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteCourier, registerCourier, updateCourier } from '../api/dispatch.api'
import type { RegisterCourierRequest, UpdateCourierRequest } from '../types/dispatch.types'

export function useRegisterCourier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: RegisterCourierRequest) => registerCourier(request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'couriers'] }),
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'summary'] }),
      ])
    },
  })
}

export function useUpdateCourier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ courierId, request }: { courierId: string; request: UpdateCourierRequest }) => updateCourier(courierId, request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'couriers'] }),
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'summary'] }),
      ])
    },
  })
}

export function useDeleteCourier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (courierId: string) => deleteCourier(courierId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'couriers'] }),
        queryClient.invalidateQueries({ queryKey: ['dispatch', 'summary'] }),
      ])
    },
  })
}
