import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { Outlet } from 'react-router-dom'
import { AdminNav } from './AdminNav'
import './AdminLayout.css'

/** AUTH PLACEHOLDER: current auth exposes no role lookup or route guard. */
function AdminAccessPlaceholder({ children }: { children: React.ReactNode }) {
  return <>{children}</>
}

export function AdminLayout() {
  const queryClient = useQueryClient()
  const fetching = useIsFetching({ queryKey: ['dispatch'] }) + useIsFetching({ queryKey: ['reporting'] })
  const queryStates = queryClient.getQueryCache().findAll({ predicate: (query) => ['dispatch', 'reporting'].includes(String(query.queryKey[0])) })
  const hasConnectionIssue = queryStates.some((query) => query.state.status === 'error')
  return (
    <div className="admin-shell">
      <AdminNav hasConnectionIssue={hasConnectionIssue && fetching === 0} />
      <AdminAccessPlaceholder><Outlet /></AdminAccessPlaceholder>
    </div>
  )
}
