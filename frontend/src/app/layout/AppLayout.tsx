import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/identity'

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
          <Link to="/courier">Courier dashboard</Link>
          {user ? (
            <button type="button" className="app-header__logout" onClick={handleLogout}>
              Log out ({user.fullName})
            </button>
          ) : (
            <Link to="/login">Log in</Link>
          )}
        </nav>
      </header>
      <main className="page-content">
        <Outlet />
      </main>
    </div>
  )
}
