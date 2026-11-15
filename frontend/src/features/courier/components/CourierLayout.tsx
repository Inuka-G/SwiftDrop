import { NavLink, Outlet } from 'react-router-dom'
import { Icon, type IconName } from './Icon'
import '../courier.css'

const TABS: { to: string; label: string; icon: IconName; end?: boolean }[] = [
  { to: '/courier', label: 'Runs', icon: 'truck', end: true },
  { to: '/courier/completed', label: 'Completed', icon: 'checkCircle' },
  { to: '/courier/shift', label: 'Shift', icon: 'user' },
]

/** Phone-style frame for the courier screens: page content plus a bottom tab bar. */
export function CourierLayout() {
  return (
    <div className="courier-app">
      <div className="courier-screen">
        <Outlet />
      </div>
      <nav className="bottom-nav" aria-label="Courier navigation">
        {TABS.map((tab) => (
          <NavLink key={tab.to} to={tab.to} end={tab.end} className="bottom-nav-item">
            <Icon name={tab.icon} size={22} />
            <span>{tab.label}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
