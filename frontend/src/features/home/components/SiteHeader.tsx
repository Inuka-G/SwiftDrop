import { Link, useLocation, useNavigate } from 'react-router-dom'
import { homePathForRole } from '../../../lib/constants'
import { BrandMark } from '../../identity/components/BrandMark'
import { useAuth } from '../../identity/useAuth'

export function SiteHeader() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const isMerchantSection = location.pathname.startsWith('/merchant')
  const dashboard = user ? (isMerchantSection ? '/merchant/portal' : homePathForRole(user.role)) : '/'

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="sd-site-header">
      <Link to={isMerchantSection && user ? '/merchant/portal' : '/'} className="sd-brand">
        <BrandMark />
        <span className="sd-brand__text">
          <strong>SwiftDrop</strong>
          <small>{isMerchantSection ? 'Merchant Hub' : 'Express Logistics'}</small>
        </span>
      </Link>
      <nav className="sd-site-nav" aria-label="Main navigation">
        {isMerchantSection ? (
          <>
            <Link to="/merchant/portal?tab=bookings">Book &amp; Track Deliveries</Link>
            <Link to="/merchant/portal?tab=api-keys">API &amp; OAuth Keys</Link>
            <Link to="/merchant/status">Application Status</Link>
            <Link to="/merchant/portal?tab=settings">Account Settings</Link>
          </>
        ) : (
          <>
            <a href="#track">Track</a>
            <a href="#rates">Rates</a>
            <a href="#coverage">Coverage</a>
          </>
        )}
      </nav>
      <div className="sd-site-header__actions">
        {user ? (
          <>
            <span className="sd-site-header__user">Hi, {user.fullName.split(' ')[0]}</span>
            {dashboard !== '/' && <Link to={dashboard} className="sd-btn sd-btn--soft">Dashboard</Link>}
            <button type="button" className="sd-btn sd-btn--soft" onClick={handleLogout}>Log Out</button>
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
