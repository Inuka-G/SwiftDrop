import { Link, Outlet } from 'react-router-dom'

export function AppLayout() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <Link className="brand" to="/">
          <span className="brand-mark" aria-hidden="true">S</span>
          <span>SwiftDrop</span>
        </Link>
        <nav aria-label="Main navigation">
          <Link to="/">Overview</Link>
          <Link to="/courier">Courier dashboard</Link>
          <Link to="/track">Track Parcel</Link>
          <Link to="/book">Book Delivery</Link>
          <Link to="/history">My Deliveries</Link>
        </nav>
      </header>
      <main className="page-content">
        <Outlet />
      </main>
    </div>
  )
}
