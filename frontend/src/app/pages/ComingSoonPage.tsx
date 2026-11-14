import { Link, useLocation } from 'react-router-dom'
import { BrandMark } from '../../features/identity'

/** Placeholder for pages other team members are still building (tracking, booking, merchant sign-up). */
export function ComingSoonPage() {
  const { pathname } = useLocation()
  return (
    <div className="sd-auth-page sd-auth-page--column">
      <BrandMark size="lg" />
      <h1 className="sd-title sd-center">Coming soon</h1>
      <p className="sd-muted sd-center">
        The page <code className="sd-mono">{pathname}</code> is not built yet.
      </p>
      <Link to="/" className="sd-btn sd-btn--primary">Back to home</Link>
    </div>
  )
}
