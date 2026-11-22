import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { SiteHeader } from '../../home/components/SiteHeader'
import { merchantApi } from '../api/merchant.api'
import type { MerchantDto } from '../types/merchant.types'
import './MerchantStatusPage.css'

interface LocationState {
  registered?: boolean
  businessName?: string
  businessRegNumber?: string
}

export function MerchantStatusPage() {
  const location = useLocation()
  const navState = (location.state as LocationState) ?? {}

  const [merchant, setMerchant] = useState<MerchantDto | null>(null)

  useEffect(() => {
    let active = true

    async function loadProfile() {
      try {
        const data = await merchantApi.getMyMerchantProfile()
        if (active && data) {
          setMerchant(data)
        }
      } catch {
        // If not logged in or in preview mode, fallback to navState or sample defaults
      }
    }

    loadProfile()
    return () => {
      active = false
    }
  }, [])

  // Use live data if loaded, otherwise fallback to navState or demo preview values
  const businessName = merchant?.businessName ?? navState.businessName ?? 'Ceylon Threads Apparel PVT LTD'
  const businessRegNumber = merchant?.businessRegNumber ?? navState.businessRegNumber ?? 'PV-00284918-LK'
  const status = merchant?.status ?? 'Pending'
  const referenceId = merchant?.id ? `APP-${merchant.id.substring(0, 8).toUpperCase()}` : 'APP-2025-88392'

  const isApproved = status.toLowerCase() === 'approved'
  const isRejected = status.toLowerCase() === 'rejected'

  return (
    <div className="sd-status-page">
      <SiteHeader />

      <main className="sd-status-container">
        {/* Glow */}
        <div className="sd-status-glow" aria-hidden="true" />

        {/* Primary Status Card */}
        <section className="sd-status-card">
          {/* Status Icon */}
          <div
            className={`sd-status-icon-wrapper ${
              isApproved
                ? 'sd-status-icon-wrapper--approved'
                : isRejected
                ? 'sd-status-icon-wrapper--rejected'
                : 'sd-status-icon-wrapper--pending'
            }`}
          >
            {isApproved ? (
              <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <path d="M20 6L9 17l-5-5" />
              </svg>
            ) : isRejected ? (
              <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            ) : (
              <>
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M5 22h14" />
                  <path d="M5 2h14" />
                  <path d="M17 22v-4.172a2 2 0 0 0-.586-1.414L12 12l-4.414 4.414A2 2 0 0 0 7 17.828V22" />
                  <path d="M7 2v4.172a2 2 0 0 0 .586 1.414L12 12l4.414-4.414A2 2 0 0 0 17 6.172V2" />
                </svg>
                <span className="sd-status-ping-dot">
                  <span className="sd-status-ping-circle" />
                  <span className="sd-status-ping-static" />
                </span>
              </>
            )}
          </div>

          {/* Status Chip */}
          <div
            className={`sd-status-chip ${
              isApproved
                ? 'sd-status-chip--approved'
                : isRejected
                ? 'sd-status-chip--rejected'
                : 'sd-status-chip--pending'
            }`}
          >
            <span className="sd-status-chip-dot" />
            <span>
              {isApproved
                ? 'Application Approved & Active'
                : isRejected
                ? 'Application Needs Attention'
                : 'Application Under Review'}
            </span>
          </div>

          {/* Headline */}
          <h1 className="sd-status-title">
            {isApproved
              ? 'Welcome to the SwiftDrop Network!'
              : isRejected
              ? 'Your application requires revision'
              : 'Your merchant account is under review'}
          </h1>

          <p className="sd-status-subtitle">
            {isApproved
              ? 'Your business credentials have been verified. You can now access your Merchant Developer Portal to generate API keys and configure client credentials.'
              : isRejected
              ? 'The verification team requested additional details regarding your business registration. Please check your email or contact support.'
              : 'Thanks for joining SwiftDrop! We are reviewing your business details and setting up your logistics rails. You will receive an email as soon as dispatch access is enabled.'}
          </p>

          {/* Reference ID Pill */}
          <div className="sd-status-ref">
            <span>Reference ID:</span>
            <code>{referenceId}</code>
          </div>

          {/* Milestones */}
          <div className="sd-milestones">
            <h2 className="sd-milestones__heading">Verification Milestones</h2>

            <div className="sd-milestone-list">
              {/* Milestone 1 */}
              <div className="sd-milestone-item">
                <div className="sd-milestone-icon sd-milestone-icon--completed">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                </div>
                <div className="sd-milestone-content">
                  <p className="sd-milestone-title">1. Registration Received</p>
                  <p className="sd-milestone-desc">Company profile and business details submitted</p>
                </div>
                <span className="sd-milestone-badge sd-milestone-badge--completed">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                  Completed
                </span>
              </div>

              {/* Milestone 2 */}
              <div
                className={`sd-milestone-item ${
                  isApproved
                    ? ''
                    : isRejected
                    ? ''
                    : 'sd-milestone-item--active'
                }`}
              >
                <div
                  className={`sd-milestone-icon ${
                    isApproved
                      ? 'sd-milestone-icon--completed'
                      : 'sd-milestone-icon--active'
                  }`}
                >
                  {isApproved ? (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                      <polyline points="20 6 9 17 4 12" />
                    </svg>
                  ) : (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                      <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67" />
                    </svg>
                  )}
                </div>
                <div className="sd-milestone-content">
                  <p className="sd-milestone-title">2. Business &amp; Tax Verification</p>
                  <p className="sd-milestone-desc">Confirming business registration credentials</p>
                </div>
                <span
                  className={`sd-milestone-badge ${
                    isApproved
                      ? 'sd-milestone-badge--completed'
                      : 'sd-milestone-badge--active'
                  }`}
                >
                  {isApproved ? 'Completed' : 'In Progress'}
                </span>
              </div>

              {/* Milestone 3 */}
              <div
                className={`sd-milestone-item ${
                  isApproved
                    ? 'sd-milestone-item--active'
                    : 'sd-milestone-item--upcoming'
                }`}
              >
                <div
                  className={`sd-milestone-icon ${
                    isApproved
                      ? 'sd-milestone-icon--completed'
                      : 'sd-milestone-icon--upcoming'
                  }`}
                >
                  {isApproved ? (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                      <polyline points="20 6 9 17 4 12" />
                    </svg>
                  ) : (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                      <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                    </svg>
                  )}
                </div>
                <div className="sd-milestone-content">
                  <p className="sd-milestone-title">3. Account Activation &amp; Developer Access</p>
                  <p className="sd-milestone-desc">Direct dispatch access and API key enablement</p>
                </div>
                <span
                  className={`sd-milestone-badge ${
                    isApproved
                      ? 'sd-milestone-badge--completed'
                      : 'sd-milestone-badge--upcoming'
                  }`}
                >
                  {isApproved ? 'Active' : 'Up Next'}
                </span>
              </div>
            </div>
          </div>

          {/* Business Summary Card */}
          <div className="sd-status-summary">
            <div className="sd-status-summary__header">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                <polyline points="9 22 9 12 15 12 15 22" />
              </svg>
              <span>Application Summary</span>
            </div>

            <div className="sd-status-summary__grid">
              <div>
                <span className="sd-summary-label">Registered Business</span>
                <span className="sd-summary-value">{businessName}</span>
              </div>
              <div>
                <span className="sd-summary-label">Tax / Reg Number</span>
                <span className="sd-summary-value sd-summary-value--code">{businessRegNumber}</span>
              </div>
              <div>
                <span className="sd-summary-label">Estimated Review Time</span>
                <span className="sd-summary-value sd-summary-value--highlight">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="12" cy="12" r="10" />
                    <polyline points="12 6 12 12 16 14" />
                  </svg>
                  2–4 business hours
                </span>
              </div>
              <div>
                <span className="sd-summary-label">Account Type</span>
                <span className="sd-summary-value">Merchant Partner (Express)</span>
              </div>
            </div>
          </div>

          {/* If Approved: Big Portal CTA */}
          {isApproved && (
            <Link to="/merchant/portal" className="sd-status-portal-btn">
              <span>Enter Merchant Developer Portal</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <path d="M5 12h14M13 6l6 6-6 6" />
              </svg>
            </Link>
          )}
        </section>

        {/* Support Card */}
        <footer className="sd-status-support">
          <div className="sd-status-support__info">
            <span className="sd-status-support__title">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z" />
              </svg>
              Need help or have questions about onboarding?
            </span>
            <div className="sd-status-support__links">
              <a href="mailto:onboarding@swiftdrop.io">onboarding@swiftdrop.io</a>
              <span>•</span>
              <span>+94 11 200 4800 (Ext 4)</span>
            </div>
          </div>

          <Link to="/" className="sd-status-home-link">
            Return to Home
          </Link>
        </footer>
      </main>
    </div>
  )
}
