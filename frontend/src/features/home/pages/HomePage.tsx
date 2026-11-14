import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { BrandMark } from '../../identity/components/BrandMark'
import { ArrowRightIcon, ClockIcon, GlobeIcon, PinIcon, SearchIcon, TruckIcon } from '../../identity/components/Icons'
import { RateCalculator } from '../components/RateCalculator'
import { SiteHeader } from '../components/SiteHeader'

const STATS = [
  { value: '120k+', label: 'Parcels Delivered Monthly', tone: 'primary' },
  { value: '25', label: 'Districts Connected', tone: 'neutral' },
  { value: '99.8%', label: 'On-Time Success', tone: 'success' },
]

const CURRENT_YEAR = new Date().getFullYear()

const FEATURES = [
  { icon: <ClockIcon />, tone: 'primary', title: 'Express Speed',
    text: 'Next-morning and same-day city delivery across major economic hubs.' },
  { icon: <GlobeIcon />, tone: 'secondary', title: 'Island Coverage',
    text: 'Doorstep delivery across all 25 districts with guaranteed tracking milestones.' },
  { icon: <PinIcon />, tone: 'success', title: 'Live Tracking',
    text: 'Instant status updates, SMS alerts, and clear digital proof of delivery.' },
]

export function HomePage() {
  const navigate = useNavigate()
  const [trackingNumber, setTrackingNumber] = useState('')

  function handleTrack(event: FormEvent) {
    event.preventDefault()
    const value = trackingNumber.trim().toUpperCase()
    if (value) navigate(`/track/${encodeURIComponent(value)}`)
  }

  return (
    <div className="sd-site">
      <SiteHeader />

      <section className="sd-hero" id="track">
        <span className="sd-pill sd-pill--success">Nationwide courier delivery across Sri Lanka</span>
        <h1 className="sd-display">Fast, reliable parcel delivery across the island</h1>
        <p className="sd-hero__lead">
          Effortless door-to-door shipping for individuals and growing businesses. Real-time updates,
          upfront pricing, guaranteed safe handover.
        </p>

        <form className="sd-track-box" onSubmit={handleTrack} role="search">
          <label className="sd-track-box__input">
            <SearchIcon />
            <span className="sd-visually-hidden">Tracking number</span>
            <input className="sd-mono" placeholder="ENTER TRACKING NUMBER (E.G. SD-9042)" value={trackingNumber}
              onChange={(e) => setTrackingNumber(e.target.value)} />
          </label>
          <button type="submit" className="sd-btn sd-btn--primary" disabled={!trackingNumber.trim()}>
            Track <ArrowRightIcon />
          </button>
        </form>

        <div className="sd-hero__actions">
          <Link to="/send-parcel" className="sd-btn sd-btn--tonal"><TruckIcon /> Send a Parcel</Link>
          <Link to="/login" className="sd-link sd-link--strong">Sign In to Merchant Hub</Link>
        </div>
      </section>

      <section className="sd-stats" aria-label="SwiftDrop in numbers">
        {STATS.map((stat) => (
          <div key={stat.label} className="sd-stat">
            <strong className={`sd-stat__value sd-tone-${stat.tone}`}>{stat.value}</strong>
            <span>{stat.label}</span>
          </div>
        ))}
      </section>

      <section className="sd-section" id="rates">
        <h2 className="sd-headline">Instant Rate Calculator</h2>
        <p className="sd-section__lead">Simple, transparent pricing with no hidden charges across all destinations.</p>
        <RateCalculator />
      </section>

      <section className="sd-section sd-section--tinted" id="coverage">
        <h2 className="sd-headline">Designed for peace of mind</h2>
        <p className="sd-section__lead">A straightforward, modern logistics service built around reliability.</p>
        <div className="sd-features">
          {FEATURES.map((feature) => (
            <article key={feature.title} className="sd-card sd-feature">
              <span className={`sd-feature__icon sd-feature__icon--${feature.tone}`}>{feature.icon}</span>
              <h3>{feature.title}</h3>
              <p>{feature.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="sd-cta">
        <h2 className="sd-headline">Ready to send your parcel?</h2>
        <p>Get started in minutes. No paperwork, no minimum order volumes.</p>
        <div className="sd-cta__actions">
          <a href="#rates" className="sd-btn sd-btn--white">Estimate Rate</a>
          <Link to="/signup" className="sd-btn sd-btn--bright">Create Account</Link>
        </div>
      </section>

      <footer className="sd-footer">
        <Link to="/" className="sd-brand"><BrandMark /> <strong>SwiftDrop</strong></Link>
        <nav aria-label="Footer">
          <a href="#track">Track</a>
          <a href="#rates">Rates</a>
          <a href="#coverage">Coverage</a>
          <a href="mailto:support@swiftdrop.local">Support</a>
        </nav>
        <small>© {CURRENT_YEAR} SwiftDrop. All rights reserved.</small>
      </footer>
    </div>
  )
}
