import { useQuery } from '@tanstack/react-query'
import { searchCourierUsers } from '../api/users.api'

export function useCourierUsers(search: string, enabled = true) {
  return useQuery({
    queryKey: ['identity', 'users', 'courier-search', search],
    queryFn: () => searchCourierUsers(search),
    enabled,
    staleTime: 15_000,
    retry: 1,
  })
}
