import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { SiteHeader } from '../../home/components/SiteHeader'
import { FormAlert } from '../../identity/components/FormAlert'
import { merchantApi } from '../api/merchant.api'
import './MerchantRegistrationPage.css'

export function MerchantRegistrationPage() {
  const navigate = useNavigate()

  // Form State
  const [businessName, setBusinessName] = useState('')
  const [businessRegNumber, setBusinessRegNumber] = useState('')
  const [webhookUrl, setWebhookUrl] = useState('')
  const [category, setCategory] = useState('Fashion & Apparel')
  const [contactName, setContactName] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [contactPhone, setContactPhone] = useState('')

  // UI State
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (!businessName.trim()) {
      setError('Business or store name is required.')
      return
    }

    if (!businessRegNumber.trim()) {
      setError('Business registration or tax number is required.')
      return
    }

    if (webhookUrl.trim()) {
      try {
        const parsed = new URL(webhookUrl.trim())
        if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
          setError('Webhook URL must start with http:// or https://')
          return
        }
      } catch {
        setError('Please enter a valid absolute Webhook URL (e.g. https://api.yourstore.com/webhooks).')
        return
      }
    }

    setSubmitting(true)

    try {
      await merchantApi.registerMerchant({
        businessName: businessName.trim(),
        businessRegNumber: businessRegNumber.trim().toUpperCase(),
        webhookUrl: webhookUrl.trim() || null,
      })

      // On successful registration, navigate to the live status review tracker
      navigate('/merchant/status', {
        state: {
          registered: true,
          businessName: businessName.trim(),
          businessRegNumber: businessRegNumber.trim().toUpperCase(),
        },
      })
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to submit registration. Please verify your details.'))
      setSubmitting(false)
    }
  }

  return (
    <div className="sd-merchant-reg">
      <SiteHeader />

      <main className="sd-merchant-reg__container">
        {/* Header Meta Context */}
        <div className="sd-merchant-reg__header">
          <div>
            <div className="sd-merchant-reg__tag">
              <span className="sd-merchant-reg__tag-dot" />
              <span>Merchant Registration</span>
            </div>
            <h1 className="sd-merchant-reg__title">Partner with SwiftDrop</h1>
            <p className="sd-merchant-reg__lead">
              Fast, reliable nationwide courier delivery for your business with automated COD settlements and simple integrations.
            </p>
          </div>

          <div className="sd-merchant-reg__badge">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
              <path d="m9 12 2 2 4-4" />
            </svg>
            <span>Quick Onboarding • Active in 2-4 Hours</span>
          </div>
        </div>

        {/* 2-Column Grid */}
        <div className="sd-merchant-reg__grid">
          {/* Left Column: Partner Value & Social Proof */}
          <aside className="sd-merchant-reg__benefits">
            <div className="sd-benefit-card">
              <h3 className="sd-benefit-card__title">Why Sell with SwiftDrop?</h3>
              <p className="sd-benefit-card__text">
                Designed for online stores and retailers who need dependable next-day delivery and hassle-free payouts.
              </p>

              <div className="sd-benefit-items">
                <div className="sd-benefit-item">
                  <div className="sd-benefit-item__icon">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <rect x="2" y="5" width="20" height="14" rx="2" />
                      <line x1="2" y1="10" x2="22" y2="10" />
                    </svg>
                  </div>
                  <div className="sd-benefit-item__content">
                    <h4>Daily COD Remittances</h4>
                    <p>Next-day bank clearance directly into your account with real-time transfer notifications.</p>
                  </div>
                </div>

                <div className="sd-benefit-item">
                  <div className="sd-benefit-item__icon">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M3 6h11v10H3zM14 9h4l3 3v4h-7" />
                      <circle cx="7" cy="17.5" r="1.8" />
                      <circle cx="17" cy="17.5" r="1.8" />
                    </svg>
                  </div>
                  <div className="sd-benefit-item__content">
                    <h4>Islandwide Pickup &amp; Delivery</h4>
                    <p>Free scheduled pickups from your warehouse or store with end-to-end parcel tracking.</p>
                  </div>
                </div>

                <div className="sd-benefit-item">
                  <div className="sd-benefit-item__icon">
                    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <polyline points="16 18 22 12 16 6" />
                      <polyline points="8 6 2 12 8 18" />
                    </svg>
                  </div>
                  <div className="sd-benefit-item__content">
                    <h4>Simple Store Integrations</h4>
                    <p>One-click connections for Shopify, WooCommerce, or direct API &amp; Webhook tracking.</p>
                  </div>
                </div>
              </div>

              {/* Testimonial Card */}
              <div className="sd-testimonial-card">
                <div className="sd-testimonial-stars" aria-hidden="true">
                  {'★★★★★'.split('').map((star, i) => (
                    <span key={i}>{star}</span>
                  ))}
                </div>
                <span className="sd-testimonial-count">Trusted by 1,200+ local businesses</span>
                <p className="sd-testimonial-quote">
                  “Onboarding was completely painless. We started scheduling pickups within hours of sign-up.”
                </p>
                <div className="sd-testimonial-author">
                  <div className="sd-testimonial-avatar">SK</div>
                  <div>
                    <p className="sd-testimonial-author-name">Sanjeewa Kulathunga</p>
                    <p className="sd-testimonial-author-role">UrbanLuxe Footwear</p>
                  </div>
                </div>
              </div>
            </div>
          </aside>

          {/* Right Column: Registration Form */}
          <section className="sd-merchant-form-card">
            <div className="sd-merchant-form-card__header">
              <h2 className="sd-merchant-form-card__title">Merchant Application</h2>
              <p className="sd-merchant-form-card__subtitle">Complete the details below to begin shipping. No upfront fees required.</p>
            </div>

            <form onSubmit={handleSubmit} noValidate>
              {/* Section 1: Business Details */}
              <div className="sd-form-section">
                <div className="sd-form-section__title">
                  <span className="sd-form-section__number">1</span>
                  <span>Business Details</span>
                </div>

                <div className="sd-form-grid sd-form-grid--two-col">
                  <div className="sd-form-field sd-form-field--full">
                    <label className="sd-form-label" htmlFor="businessName">
                      Business or Store Name <span className="sd-form-required">*</span>
                    </label>
                    <input
                      id="businessName"
                      className="sd-form-input"
                      type="text"
                      placeholder="e.g. Ceylon Threads Apparel"
                      value={businessName}
                      onChange={(e) => setBusinessName(e.target.value)}
                      required
                    />
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="businessRegNumber">
                      Business Reg / Tax ID <span className="sd-form-required">*</span>
                    </label>
                    <input
                      id="businessRegNumber"
                      className="sd-form-input"
                      type="text"
                      placeholder="e.g. PV-00284918-LK"
                      value={businessRegNumber}
                      onChange={(e) => setBusinessRegNumber(e.target.value)}
                      style={{ textTransform: 'uppercase' }}
                      required
                    />
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="category">
                      Business Category <span className="sd-form-required">*</span>
                    </label>
                    <select
                      id="category"
                      className="sd-form-select"
                      value={category}
                      onChange={(e) => setCategory(e.target.value)}
                    >
                      <option value="Fashion & Apparel">Fashion &amp; Apparel</option>
                      <option value="Consumer Electronics & Tech">Consumer Electronics &amp; Tech</option>
                      <option value="Health, Beauty & Cosmetics">Health, Beauty &amp; Cosmetics</option>
                      <option value="Food & Groceries">Food &amp; Groceries</option>
                      <option value="General Retail">General Retail</option>
                    </select>
                  </div>

                  <div className="sd-form-field sd-form-field--full">
                    <label className="sd-form-label" htmlFor="webhookUrl">
                      Webhook URL <span className="sd-optional">(Optional)</span>
                    </label>
                    <input
                      id="webhookUrl"
                      className="sd-form-input"
                      type="url"
                      placeholder="https://api.yourstore.com/webhooks"
                      value={webhookUrl}
                      onChange={(e) => setWebhookUrl(e.target.value)}
                    />
                    <p className="sd-form-hint">
                      Receive real-time parcel delivery status updates directly on your server.
                    </p>
                  </div>
                </div>
              </div>

              {/* Section 2: Contact Person */}
              <div className="sd-form-section">
                <div className="sd-form-section__title">
                  <span className="sd-form-section__number">2</span>
                  <span>Contact Information</span>
                </div>

                <div className="sd-form-grid sd-form-grid--two-col">
                  <div className="sd-form-field sd-form-field--full">
                    <label className="sd-form-label" htmlFor="contactName">
                      Contact Person Full Name
                    </label>
                    <input
                      id="contactName"
                      className="sd-form-input"
                      type="text"
                      placeholder="e.g. Anushka Senanayake"
                      value={contactName}
                      onChange={(e) => setContactName(e.target.value)}
                    />
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="contactEmail">
                      Email Address
                    </label>
                    <input
                      id="contactEmail"
                      className="sd-form-input"
                      type="email"
                      placeholder="operations@ceylonthreads.lk"
                      value={contactEmail}
                      onChange={(e) => setContactEmail(e.target.value)}
                    />
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="contactPhone">
                      Phone Number
                    </label>
                    <input
                      id="contactPhone"
                      className="sd-form-input"
                      type="tel"
                      placeholder="+94 77 123 4567"
                      value={contactPhone}
                      onChange={(e) => setContactPhone(e.target.value)}
                    />
                  </div>
                </div>
              </div>

              {/* Review Time Banner */}
              <div className="sd-review-notice">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <circle cx="12" cy="12" r="9" />
                  <path d="M12 7v5l3 2" />
                </svg>
                <p>
                  Accounts are reviewed within <strong>2–4 business hours</strong>. You will receive real-time updates on your onboarding portal.
                </p>
              </div>

              {/* Error display */}
              {error && <FormAlert message={error} />}

              {/* Submit CTA */}
              <button
                type="submit"
                className="sd-submit-btn"
                disabled={submitting}
              >
                <span>{submitting ? 'Submitting Application...' : 'Submit Merchant Application'}</span>
                {!submitting && (
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <path d="M5 12h14M13 6l6 6-6 6" />
                  </svg>
                )}
              </button>

              <div className="sd-login-footer">
                Already registered with SwiftDrop?{' '}
                <Link to="/login">Login to Merchant Portal</Link>
              </div>
            </form>
          </section>
        </div>
      </main>
    </div>
  )
}

