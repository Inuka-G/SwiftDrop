import { Link } from 'react-router-dom'
import { homePathForRole } from '../../../lib/constants'
import { BrandMark } from '../../identity/components/BrandMark'
import { useAuth } from '../../identity/useAuth'

export function SiteHeader() {
  const { user, logout } = useAuth()
  const dashboard = user ? homePathForRole(user.role) : '/'

  return (
    <header className="sd-site-header">
      <Link to="/" className="sd-brand">
        <BrandMark />
        <span className="sd-brand__text">
          <strong>SwiftDrop</strong>
          <small>Express Logistics</small>
        </span>
      </Link>
      <nav className="sd-site-nav" aria-label="Main navigation">
        <a href="#track">Track</a>
        <a href="#rates">Rates</a>
        <a href="#coverage">Coverage</a>
      </nav>
      <div className="sd-site-header__actions">
        {user ? (
          <>
            <span className="sd-site-header__user">Hi, {user.fullName.split(' ')[0]}</span>
            {dashboard !== '/' && <Link to={dashboard} className="sd-btn sd-btn--soft">Dashboard</Link>}
            <button type="button" className="sd-btn sd-btn--soft" onClick={logout}>Log Out</button>
          </>
        ) : (
          <>
            <Link to="/login" className="sd-btn sd-btn--soft">Log In</Link>
            <Link to="/signup" className="sd-btn sd-btn--primary">Sign Up</Link>
          </>
        )}
      </div>
    </header>
  )
}
