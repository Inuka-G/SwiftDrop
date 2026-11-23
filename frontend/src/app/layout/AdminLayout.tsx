import { useIsFetching, useQueryClient } from '@tanstack/react-query'
import { Outlet } from 'react-router-dom'
import { AdminNav } from './AdminNav'
import { useAuth } from '../../features/identity'
import './AdminLayout.css'

export function AdminLayout() {
  const { user, logout } = useAuth()
  const queryClient = useQueryClient()
  const fetching = useIsFetching({ queryKey: ['dispatch'] }) + useIsFetching({ queryKey: ['reporting'] })
  const queryStates = queryClient.getQueryCache().findAll({ predicate: (query) => ['dispatch', 'reporting'].includes(String(query.queryKey[0])) })
  const hasConnectionIssue = queryStates.some((query) => query.state.status === 'error')
  return (
    <div className="admin-shell">
      <AdminNav hasConnectionIssue={Boolean(user) && hasConnectionIssue && fetching === 0} user={user} onLogout={logout} />
      <Outlet />
    </div>
  )
}
