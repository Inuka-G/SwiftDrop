import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { homePathForRole } from '../../../lib/constants'
import { BrandMark } from '../components/BrandMark'
import { FormAlert } from '../components/FormAlert'
import { FormField } from '../components/FormField'
import { GoogleButton } from '../components/GoogleButton'
import { ArrowRightIcon, LockIcon, MailIcon, SendIcon, ShieldIcon, StoreIcon } from '../components/Icons'
import { useAuth } from '../useAuth'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  // A message passed from another page, such as a failed Google login.
  const notice = (location.state as { error?: string } | null)?.error ?? null

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [remember, setRemember] = useState(true)
  const [error, setError] = useState<string | null>(notice)
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to={homePathForRole(user.role)} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const loggedIn = await login(username.trim(), password, remember)
      navigate(homePathForRole(loggedIn.role), { replace: true })
    } catch (err) {
      setError(getErrorMessage(err))
      setSubmitting(false)
    }
  }

  return (
    <div className="sd-auth-page">
      <div className="sd-login-card">
        <aside className="sd-login-card__intro">
          <Link to="/" className="sd-brand">
            <BrandMark />
            <span className="sd-brand__text">
              <strong>SwiftDrop</strong>
              <small className="sd-mono">COURIER OS V3.8</small>
            </span>
          </Link>

          <span className="sd-pill sd-pill--live">99.94% On-Time Precision</span>
          <h2 className="sd-login-card__headline">Real-time route velocity at your fingertips.</h2>
          <p className="sd-muted">
            Centralized parcel monitoring, zero-latency manifests, and digital proof-of-delivery receipts.
          </p>

          <div className="sd-dispatch-card" aria-hidden="true">
            <div className="sd-dispatch-card__top">
              <span><SendIcon className="sd-text-secondary" /> Active Dispatch</span>
              <span className="sd-mono sd-text-primary">SW-89410</span>
            </div>
            <div className="sd-dispatch-card__route">
              <span>Sorting Hub A-4</span>
              <span className="sd-text-secondary-strong">Out for Delivery</span>
            </div>
            <div className="sd-progress"><span style={{ width: '82%' }} /></div>
            <div className="sd-dispatch-card__route">
              <span>ETA: <strong>Today, 14:20</strong></span>
              <span className="sd-text-success"><ShieldIcon /> Encrypted Manifest</span>
            </div>
          </div>

          <div className="sd-fleet-tile" aria-hidden="true">
            <span>Nationwide Fleet Active • 25 Districts</span>
          </div>
        </aside>

        <main className="sd-login-card__form">
          <h1 className="sd-title">Welcome back</h1>
          <p className="sd-muted">Access your parcel bookings, real-time dispatch dashboard, and receipts.</p>

          <GoogleButton label="Continue with Google" />
          <div className="sd-divider"><span>or continue with email</span></div>

          <form onSubmit={handleSubmit} noValidate>
            <FormAlert message={error} />
            <FormField label="Email or Username" icon={<MailIcon />} placeholder="name@example.com"
              autoComplete="username" required value={username} onChange={(e) => setUsername(e.target.value)} />
            <FormField label="Password" type="password" icon={<LockIcon />} placeholder="Your password"
              autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />

            <label className="sd-checkbox">
              <input type="checkbox" checked={remember} onChange={(e) => setRemember(e.target.checked)} />
              <span>Remember me on this device</span>
            </label>

            <button type="submit" className="sd-btn sd-btn--primary sd-btn--block"
              disabled={submitting || !username.trim() || !password}>
              {submitting ? 'Signing in…' : <>Sign In to SwiftDrop <ArrowRightIcon /></>}
            </button>
          </form>

          <p className="sd-switch">Don't have an account? <Link to="/signup">Sign Up</Link></p>

          <div className="sd-merchant-banner">
            <span className="sd-merchant-banner__icon"><StoreIcon /></span>
            <div>
              <strong>E-Commerce Business or Retailer?</strong>
              <p>Access volume rates, custom webhooks, &amp; automated dispatch.</p>
            </div>
            <Link to="/merchant/register" className="sd-link">Merchant Account <ArrowRightIcon /></Link>
          </div>

          <p className="sd-footnote"><LockIcon /> Protected by SSL and OAuth 2.0 security protocols.</p>
        </main>
      </div>
    </div>
  )
}
