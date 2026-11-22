import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/identity'
import { ROLES } from '../../lib/constants'

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
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
          <Link to="/">Home</Link>

          {(import.meta.env.DEV || user?.role === ROLES.customer) &&
            <Link to="/customer">Customer Dashboard</Link>
          }

          {user?.role === ROLES.courier && (
            <Link to="/courier">Courier Dashboard</Link>
          )}
          {user ? (
            <button type="button" className="app-header__logout" onClick={handleLogout}>
              Log out ({user.fullName})
            </button>
          ) : (
            <Link to="/login">Log in</Link>
          )}
          <Link to="/track">Track Parcel</Link>
          <Link to="/book">Book Delivery</Link>
          <Link to="/history">My Deliveries</Link>
          <Link to="/assistant">AI Assistant</Link>
        </nav>
      </header>
      <main className="page-content">
        <Outlet />
      </main>
    </div>
  )
}
