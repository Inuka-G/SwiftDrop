import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { authService } from '../../services/auth.service'

const navItems = [
  { label: 'Dashboard', path: '/admin', enabled: true },
  { label: 'Dispatch Board', path: '/admin/dispatch', enabled: true },
  { label: 'Couriers', path: '/admin/couriers', enabled: false },
  { label: 'Reports', path: '/admin/reports', enabled: false },
  { label: 'Users', path: '/admin/users', enabled: false },
  { label: 'Pricing & Zones', path: '/admin/pricing', enabled: false },
]

export function AdminNav({ hasConnectionIssue, user, onLogout }: { hasConnectionIssue: boolean; user: { fullName: string; role: string } | null; onLogout: () => void }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()

  function logout() {
    authService.clearAccessToken()
    onLogout()
    navigate('/')
  }

  return (
    <header className="admin-topbar">
      <div className="admin-topbar-inner">
        <Link className="admin-brand" to="/admin" aria-label="SwiftDrop dispatch and admin">
          <span className="admin-brand-mark" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M3 5h12v12H3zM15 9h4l3 4v4h-7zM7 19a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3Zm11 0a1.5 1.5 0 1 0 0-3 1.5 1.5 0 0 0 0 3Z" /></svg></span>
          <span className="admin-brand-copy"><strong>SwiftDrop</strong><small>DISPATCH &amp; ADMIN</small></span>
        </Link>
        <button className="admin-menu-toggle" aria-expanded={menuOpen} aria-label="Toggle navigation" onClick={() => setMenuOpen(!menuOpen)}>
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 7h16M4 12h16M4 17h16" /></svg>
        </button>
        <nav className={`admin-nav${menuOpen ? ' is-open' : ''}`} aria-label="Admin navigation">
          {navItems.map((item) => item.enabled ? (
            <Link key={item.path} className={`admin-nav-item${location.pathname === item.path ? ' is-active' : ''}`} to={item.path} aria-current={location.pathname === item.path ? 'page' : undefined} onClick={() => setMenuOpen(false)}>{item.label}</Link>
          ) : (
            <span key={item.path} className="admin-nav-item is-disabled" aria-disabled="true" title="Coming soon">{item.label}<span className="admin-tooltip" role="tooltip">Coming soon</span></span>
          ))}
        </nav>
        <div className="admin-account">
          <span className={`admin-live${hasConnectionIssue ? ' has-issue' : ''}`}><i />{hasConnectionIssue ? 'Connection issue' : 'Live'}</span>
          <span className="admin-avatar" aria-hidden="true"><svg viewBox="0 0 24 24"><circle cx="12" cy="8" r="3"/><path d="M5 20c.6-3.1 3.1-5 7-5s6.4 1.9 7 5"/></svg></span>
          <span className="admin-user-copy"><strong>{user?.fullName ?? 'Not signed in'}</strong><small>{user?.role ?? 'Sign in required'}</small></span>
          <button className="admin-logout" onClick={logout}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M10 17l5-5-5-5M15 12H3M13 4h6a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-6" /></svg>Logout</button>
        </div>
      </div>
    </header>
  )
}
