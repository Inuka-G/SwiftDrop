import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { SiteHeader } from '../../home/components/SiteHeader'
import { FormAlert } from '../../identity/components/FormAlert'
import { useAuth } from '../../identity/useAuth'
import { merchantApi } from '../api/merchant.api'
import type { BusinessRegType } from '../types/merchant.types'
import './MerchantRegistrationPage.css'

const REG_TYPE_META: Record<
  BusinessRegType,
  { label: string; placeholder: string; hint: string }
> = {
  BRN: {
    label: 'Business Registration Number (BRN)',
    placeholder: 'e.g. WC/12345 or BRN-2026-0142',
    hint: 'Enter your issued Business Registration Number (alphanumeric and standard separators allowed).',
  },
  CRN: {
    label: 'Company Registration Number',
    placeholder: 'e.g. PV 00284918',
    hint: 'Enter your ROC Company Registration Number (leading zeros and letters are preserved).',
  },
  TIN: {
    label: 'Tax Identification Number (TIN)',
    placeholder: 'e.g. 123456789 (9 digits)',
    hint: 'Enter your 9-digit IRD Tax Identification Number (numbers only).',
  },
}

const TIN_REGEX = /^[0-9]{9}$/
const GENERAL_REG_CHARS_REGEX = /^[A-Za-z0-9/\-._\s()]+$/
const ALLOWED_EMAIL_REGEX = /^[A-Za-z0-9._%+-]+@(gmail\.com|yahoo\.com|hotmail\.com)$/i
const SL_PHONE_REGEX = /^(?:0[0-9]{9}|\+94[0-9]{9})$/

function validateMerchantEmail(rawEmail: string): string | null {
  const trimmed = rawEmail.trim()
  if (!trimmed) {
    return 'Email address is required.'
  }
  if (!ALLOWED_EMAIL_REGEX.test(trimmed)) {
    return 'Email must be a valid address ending with @gmail.com, @yahoo.com, or @hotmail.com.'
  }
  return null
}

function validateSriLankaPhone(rawPhone: string): string | null {
  const cleaned = rawPhone.trim().replace(/[\s-]/g, '')
  if (!cleaned) {
    return 'Phone number is required.'
  }
  if (!SL_PHONE_REGEX.test(cleaned)) {
    return 'Enter a valid Sri Lanka phone number (10 digits starting with 0, e.g. 0771234567, or +94771234567).'
  }
  return null
}

function validateRegistrationNumber(regType: BusinessRegType, rawValue: string): string | null {
  const trimmed = rawValue.trim()

  if (regType === 'TIN') {
    if (!trimmed) {
      return 'Tax Identification Number (TIN) is required.'
    }
    if (!TIN_REGEX.test(trimmed)) {
      return 'Tax Identification Number (TIN) must contain exactly 9 digits (0–9).'
    }
    return null
  }

  if (regType === 'CRN') {
    if (!trimmed) {
      return 'Company Registration Number is required.'
    }
    if (trimmed.length > 60) {
      return 'Company Registration Number must not exceed 60 characters.'
    }
    if (!GENERAL_REG_CHARS_REGEX.test(trimmed)) {
      return 'Company Registration Number contains invalid special characters.'
    }
    return null
  }

  // BRN
  if (!trimmed) {
    return 'Business Registration Number (BRN) is required.'
  }
  if (trimmed.length > 60) {
    return 'Business Registration Number (BRN) must not exceed 60 characters.'
  }
  if (!GENERAL_REG_CHARS_REGEX.test(trimmed)) {
    return 'Business Registration Number (BRN) contains invalid special characters.'
  }
  return null
}

export function MerchantRegistrationPage() {
  const navigate = useNavigate()
  const { user, login, signup } = useAuth()

  // Form State
  const [businessName, setBusinessName] = useState('')
  const [businessRegType, setBusinessRegType] = useState<BusinessRegType>('BRN')
  const [businessRegNumber, setBusinessRegNumber] = useState('')
  const [regNumberError, setRegNumberError] = useState<string | null>(null)
  const [webhookUrl, setWebhookUrl] = useState('')
  const [category, setCategory] = useState('Fashion & Apparel')
  const [contactName, setContactName] = useState(user?.fullName ?? '')
  const [contactEmail, setContactEmail] = useState(user?.email ?? '')
  const [emailError, setEmailError] = useState<string | null>(null)
  const [contactPhone, setContactPhone] = useState('')
  const [phoneError, setPhoneError] = useState<string | null>(null)
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  useEffect(() => {
    if (user) {
      setContactName((prev) => prev || user.fullName)
      setContactEmail((prev) => prev || user.email)
    }
  }, [user])

  // UI State
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function handleRegTypeChange(newType: BusinessRegType) {
    setBusinessRegType(newType)
    // Clear previous input and reset validation rules when registration type changes
    setBusinessRegNumber('')
    setRegNumberError(null)
    setError(null)
  }

  function handleRegNumberChange(value: string) {
    setBusinessRegNumber(value)
    if (regNumberError) {
      setRegNumberError(validateRegistrationNumber(businessRegType, value))
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    if (!businessName.trim()) {
      setError('Business or store name is required.')
      return
    }

    const regValidationError = validateRegistrationNumber(businessRegType, businessRegNumber)
    if (regValidationError) {
      setRegNumberError(regValidationError)
      setError(regValidationError)
      return
    }
    setRegNumberError(null)

    const emailValidationError = validateMerchantEmail(contactEmail)
    if (emailValidationError) {
      setEmailError(emailValidationError)
      setError(emailValidationError)
      return
    }
    setEmailError(null)

    const phoneValidationError = validateSriLankaPhone(contactPhone)
    if (phoneValidationError) {
      setPhoneError(phoneValidationError)
      setError(phoneValidationError)
      return
    }
    setPhoneError(null)

    if (!user) {
      if (password.length < 5) {
        setError('Please enter your account password (or a new password of at least 8 characters).')
        return
      }
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
    const normalizedRegNumber = businessRegNumber.trim()

    try {
      // Ensure user is authenticated so the backend links UserId to the Merchant profile
      if (!user) {
        try {
          await login(contactEmail.trim(), password, true)
        } catch {
          await signup({
            fullName: contactName.trim() || businessName.trim(),
            email: contactEmail.trim(),
            password,
          })
        }
      }

      await merchantApi.registerMerchant({
        businessName: businessName.trim(),
        businessRegType,
        businessRegNumber: normalizedRegNumber,
        webhookUrl: webhookUrl.trim() || null,
      })

      // On successful registration, navigate to the live status review tracker
      navigate('/merchant/status', {
        state: {
          registered: true,
          businessName: businessName.trim(),
          businessRegNumber: normalizedRegNumber,
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
                    <label className="sd-form-label" htmlFor="businessRegType">
                      Registration Type <span className="sd-form-required">*</span>
                    </label>
                    <select
                      id="businessRegType"
                      className="sd-form-select"
                      value={businessRegType}
                      onChange={(e) => handleRegTypeChange(e.target.value as BusinessRegType)}
                      required
                    >
                      <option value="BRN">Business Registration Number (BRN)</option>
                      <option value="CRN">Company Registration Number</option>
                      <option value="TIN">Tax Identification Number (TIN)</option>
                    </select>
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
                    <label className="sd-form-label" htmlFor="businessRegNumber">
                      {REG_TYPE_META[businessRegType].label} <span className="sd-form-required">*</span>
                    </label>
                    <input
                      id="businessRegNumber"
                      className="sd-form-input"
                      type="text"
                      inputMode={businessRegType === 'TIN' ? 'numeric' : 'text'}
                      maxLength={businessRegType === 'TIN' ? 9 : 60}
                      placeholder={REG_TYPE_META[businessRegType].placeholder}
                      value={businessRegNumber}
                      onChange={(e) => handleRegNumberChange(e.target.value)}
                      onBlur={() => {
                        if (businessRegNumber.trim()) {
                          setRegNumberError(validateRegistrationNumber(businessRegType, businessRegNumber))
                        }
                      }}
                      aria-invalid={!!regNumberError}
                      required
                    />
                    {regNumberError ? (
                      <p className="sd-field__error" role="alert">
                        {regNumberError}
                      </p>
                    ) : (
                      <p className="sd-form-hint">{REG_TYPE_META[businessRegType].hint}</p>
                    )}
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
                      Email Address <span className="sd-form-required">*</span>
                    </label>
                    <input
                      id="contactEmail"
                      className="sd-form-input"
                      type="email"
                      placeholder="yourstore@gmail.com"
                      value={contactEmail}
                      onChange={(e) => {
                        setContactEmail(e.target.value)
                        if (emailError) setEmailError(validateMerchantEmail(e.target.value))
                      }}
                      onBlur={() => {
                        if (contactEmail.trim()) setEmailError(validateMerchantEmail(contactEmail))
                      }}
                      aria-invalid={!!emailError}
                      required
                    />
                    {emailError ? (
                      <p className="sd-field__error" role="alert">
                        {emailError}
                      </p>
                    ) : (
                      <p className="sd-form-hint">Allowed domains: @gmail.com, @yahoo.com, @hotmail.com</p>
                    )}
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="contactPhone">
                      Phone Number <span className="sd-form-required">*</span>
                    </label>
                    <input
                      id="contactPhone"
                      className="sd-form-input"
                      type="tel"
                      placeholder="0771234567 or +94771234567"
                      value={contactPhone}
                      onChange={(e) => {
                        setContactPhone(e.target.value)
                        if (phoneError) setPhoneError(validateSriLankaPhone(e.target.value))
                      }}
                      onBlur={() => {
                        if (contactPhone.trim()) setPhoneError(validateSriLankaPhone(contactPhone))
                      }}
                      aria-invalid={!!phoneError}
                      required
                    />
                    {phoneError ? (
                      <p className="sd-field__error" role="alert">
                        {phoneError}
                      </p>
                    ) : (
                      <p className="sd-form-hint">Format: 07XXXXXXXX (10 digits) or +947XXXXXXXX</p>
                    )}
                  </div>

                  {!user && (
                    <div className="sd-form-field sd-form-field--full">
                      <label className="sd-form-label" htmlFor="merchantPassword">
                        Account Password <span className="sd-form-required">*</span>
                      </label>
                      <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
                        <input
                          id="merchantPassword"
                          className="sd-form-input"
                          style={{ width: '100%', paddingRight: '2.75rem' }}
                          type={showPassword ? 'text' : 'password'}
                          autoComplete="new-password"
                          placeholder="Enter your existing SwiftDrop password (or create a new 8+ char password)"
                          value={password}
                          onChange={(e) => setPassword(e.target.value)}
                          required
                        />
                        <button
                          type="button"
                          onClick={() => setShowPassword((prev) => !prev)}
                          aria-label={showPassword ? 'Hide password' : 'Show password'}
                          title={showPassword ? 'Hide password' : 'Show password'}
                          style={{
                            position: 'absolute',
                            right: '0.75rem',
                            background: 'transparent',
                            border: 'none',
                            cursor: 'pointer',
                            color: 'var(--color-on-surface-variant, #434655)',
                            display: 'flex',
                            alignItems: 'center',
                            padding: '0.25rem',
                          }}
                        >
                          {showPassword ? (
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                              <path d="M3 3l18 18M10.6 10.6a3 3 0 0 0 4.2 4.2M9.9 5.2A10 10 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-3.2 4.2M6.1 6.1C3.4 7.9 2 12 2 12s3.5 7 10 7a9.8 9.8 0 0 0 4.4-1" />
                            </svg>
                          ) : (
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                              <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z" />
                              <circle cx="12" cy="12" r="3" />
                            </svg>
                          )}
                        </button>
                      </div>
                      <p className="sd-form-hint">
                        Links this merchant business to your SwiftDrop login so you can sign in once approved.
                      </p>
                    </div>
                  )}
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

