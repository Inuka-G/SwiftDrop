import type { PropsWithChildren } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../features/identity'

/**
 * Sends signed-out users to /login and hides pages from the wrong role.
 * This is only for navigation: the API enforces the same rules on every request.
 */
export function RequireRole({ roles, children }: PropsWithChildren<{ roles: string[] }>) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return null   // the saved token is still being checked
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (!roles.includes(user.role)) {
    return (
      <section className="page-content" role="alert">
        <h1>No access</h1>
        <p>Your account ({user.role.toLowerCase()}) cannot open this page.</p>
      </section>
    )
  }
  return children
}
