import type { PropsWithChildren } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import type { Role } from '../lib/constants'
import { authService } from '../services/auth.service'

/**
 * Sends signed-out users to /login and hides pages from the wrong role.
 * This is only for navigation: the API enforces the same rules on every request.
 */
export function RequireRole({ roles, children }: PropsWithChildren<{ roles: Role[] }>) {
  const location = useLocation()
  const user = authService.getCurrentUser()

  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (!roles.includes(user.role)) {
    return (
      <section className="auth-card" role="alert">
        <h1>No access</h1>
        <p className="intro">Your account ({user.role.toLowerCase()}) cannot open this page.</p>
      </section>
    )
  }
  return children
}
