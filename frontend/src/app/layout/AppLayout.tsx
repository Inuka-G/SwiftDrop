import { useQueryClient } from '@tanstack/react-query'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { ROLES } from '../../lib/constants'
import { authService } from '../../services/auth.service'

export function AppLayout() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const user = authService.getCurrentUser()

  function signOut() {
    authService.logout()
    queryClient.clear()   // drop the previous user's cached data
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <Link className="brand" to="/">
          <span className="brand-mark" aria-hidden="true">S</span>
          <span>SwiftDrop</span>
        </Link>
        <nav aria-label="Main navigation">
          {user?.role === ROLES.courier && <NavLink to="/courier">My deliveries</NavLink>}
          {user ? (
            <>
              <span className="nav-user">{user.fullName}</span>
              <button className="link-button" type="button" onClick={signOut}>
                Sign out
              </button>
            </>
          ) : (
            <NavLink to="/login">Sign in</NavLink>
          )}
        </nav>
      </header>
      <main className="page-content">
        <Outlet />
      </main>
    </div>
  )
}
