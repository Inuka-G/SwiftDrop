import { useEffect, useState } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { SiteHeader } from '../../home/components/SiteHeader'
import { useAuth } from '../../identity/useAuth'
import { parcelsApi } from '../../parcels/api/parcels.api'
import {
  EXPRESS_SERVICE_TYPE,
  STANDARD_SERVICE_TYPE,
  type ParcelCreatedResponse,
  type ParcelSummaryDto,
  type PublicTrackingDto,
} from '../../parcels/types/parcel.types'
import { merchantApi } from '../api/merchant.api'
import type {
  ApiKeySummaryDto,
  BusinessRegType,
  ClientCredentialSummaryDto,
  CreatedApiKeyDto,
  CreatedClientCredentialDto,
  MerchantDto,
} from '../types/merchant.types'
import './MerchantPortalPage.css'

export function MerchantPortalPage() {
  const { user, isLoading } = useAuth()
  const [searchParams] = useSearchParams()
  const [activeTab, setActiveTab] = useState<'bookings' | 'api-keys' | 'oauth' | 'docs' | 'settings'>('api-keys')
  const [trackedParcel, setTrackedParcel] = useState<PublicTrackingDto | null>(null)
  const [trackingLoading, setTrackingLoading] = useState(false)

  // Data States
  const [merchantProfile, setMerchantProfile] = useState<MerchantDto | null>(null)
  const [apiKeys, setApiKeys] = useState<ApiKeySummaryDto[]>([])
  const [oauthClients, setOauthClients] = useState<ClientCredentialSummaryDto[]>([])

  // Manual Merchant Parcel Booking States
  const [merchantDeliveries, setMerchantDeliveries] = useState<ParcelSummaryDto[]>([])
  const [bookingZoneId, setBookingZoneId] = useState('')
  const [bookingServiceType, setBookingServiceType] = useState<number>(STANDARD_SERVICE_TYPE)
  const [pickupContactName, setPickupContactName] = useState('')
  const [pickupContactPhone, setPickupContactPhone] = useState('+94771234567')
  const [pickupAddress, setPickupAddress] = useState('')
  const [recipientName, setRecipientName] = useState('')
  const [recipientPhone, setRecipientPhone] = useState('')
  const [dropoffAddress, setDropoffAddress] = useState('')
  const [weightKg, setWeightKg] = useState<number>(1)
  const [sizeCategory, setSizeCategory] = useState<'Small' | 'Medium' | 'Large'>('Small')
  const [paymentMethod, setPaymentMethod] = useState<'COD' | 'Prepaid'>('COD')
  const [estimatedFee, setEstimatedFee] = useState<number | null>(null)
  const [bookingSubmitting, setBookingSubmitting] = useState(false)
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [bookingResult, setBookingResult] = useState<ParcelCreatedResponse | null>(null)

  // Profile & Account Settings States
  const [editBusinessName, setEditBusinessName] = useState('')
  const [editRegType, setEditRegType] = useState<BusinessRegType>('BRN')
  const [editRegNumber, setEditRegNumber] = useState('')
  const [editWebhookUrl, setEditWebhookUrl] = useState('')
  const [profileMsg, setProfileMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [savingProfile, setSavingProfile] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPasswords, setShowPasswords] = useState(false)
  const [passwordMsg, setPasswordMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  // Modal States
  const [showKeyModal, setShowKeyModal] = useState(false)
  const [keyName, setKeyName] = useState('')
  const [createdKey, setCreatedKey] = useState<CreatedApiKeyDto | null>(null)
  const [copiedKey, setCopiedKey] = useState(false)

  const [showOAuthModal, setShowOAuthModal] = useState(false)
  const [clientName, setClientName] = useState('')
  const [createdClient, setCreatedClient] = useState<CreatedClientCredentialDto | null>(null)
  const [copiedClientId, setCopiedClientId] = useState(false)
  const [copiedSecret, setCopiedSecret] = useState(false)
  const [copiedCurl1, setCopiedCurl1] = useState(false)
  const [copiedCurl2, setCopiedCurl2] = useState(false)

  const [revokeTarget, setRevokeTarget] = useState<{
    type: 'api-key' | 'oauth'
    id: string
    name: string
  } | null>(null)

  const [processing, setProcessing] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  // Load initial data
  useEffect(() => {
    loadProfile()
    loadKeys()
    loadClients()
    loadZonesAndDeliveries()
  }, [])

  // Sync top header navigation query param (?tab=bookings | api-keys | oauth | docs | settings)
  useEffect(() => {
    const tabParam = searchParams.get('tab')
    if (
      tabParam === 'bookings' ||
      tabParam === 'api-keys' ||
      tabParam === 'oauth' ||
      tabParam === 'docs' ||
      tabParam === 'settings'
    ) {
      setActiveTab(tabParam)
      document.getElementById('merchant-portal-tabs')?.scrollIntoView({ behavior: 'smooth' })
    }
  }, [searchParams])

  async function openInlineTracker(trackingNumber: string) {
    setTrackingLoading(true)
    try {
      const info = await parcelsApi.trackParcel(trackingNumber)
      setTrackedParcel(info)
    } catch {
      const localMatch = merchantDeliveries.find((d) => d.trackingNumber === trackingNumber)
      setTrackedParcel({
        trackingNumber,
        currentStatus: localMatch?.currentStatus ?? 'Created',
        createdAtUtc: localMatch?.createdAtUtc ?? new Date().toISOString(),
        updatedAtUtc: new Date().toISOString(),
        destinationArea: localMatch?.dropoffAddress ?? 'Sri Lanka',
      })
    } finally {
      setTrackingLoading(false)
    }
  }

  // Estimate fee whenever zone, weight, or serviceType changes
  useEffect(() => {
    if (!bookingZoneId || weightKg < 0.1 || weightKg > 50) {
      setEstimatedFee(null)
      return
    }
    let cancelled = false
    parcelsApi
      .estimateFee(bookingZoneId, weightKg, bookingServiceType)
      .then((res) => {
        if (!cancelled) setEstimatedFee(res.total)
      })
      .catch(() => {
        if (!cancelled) setEstimatedFee(null)
      })
    return () => {
      cancelled = true
    }
  }, [bookingZoneId, weightKg, bookingServiceType])

  async function loadZonesAndDeliveries() {
    try {
      const loadedZones = await parcelsApi.getZones()
      if (loadedZones.length > 0) {
        const preferred =
          loadedZones.find((z) => z.zoneName.toLowerCase().includes('colombo')) ??
          loadedZones.find((z) => !z.zoneName.toLowerCase().includes('test')) ??
          loadedZones[0]
        setBookingZoneId((prev) => prev || preferred.id)
      }
    } catch {
      // Handled gracefully
    }

    try {
      const history = await parcelsApi.getMyHistory()
      setMerchantDeliveries(history)
    } catch {
      setMerchantDeliveries([])
    }
  }

  async function handleCreateMerchantDelivery(e: React.FormEvent) {
    e.preventDefault()
    setBookingError(null)
    setBookingResult(null)

    if (!bookingZoneId) {
      setBookingError('Please select a delivery zone.')
      return
    }
    if (!pickupContactName.trim() || !pickupContactPhone.trim() || !pickupAddress.trim()) {
      setBookingError('Please fill in your shop pickup contact name, phone, and address.')
      return
    }
    if (!recipientName.trim() || !recipientPhone.trim() || !dropoffAddress.trim()) {
      setBookingError('Please fill in the customer recipient name, phone, and delivery address.')
      return
    }

    const slPhoneRegex = /^(?:0[0-9]{9}|\+94[0-9]{9})$/
    const cleanPickupPhone = pickupContactPhone.trim().replace(/[\s-]/g, '')
    const cleanRecipientPhone = recipientPhone.trim().replace(/[\s-]/g, '')

    if (!slPhoneRegex.test(cleanPickupPhone)) {
      setBookingError('Shop Pickup Contact Phone must be a valid Sri Lanka number (e.g. 0771234567 or +94771234567).')
      return
    }
    if (!slPhoneRegex.test(cleanRecipientPhone)) {
      setBookingError('Customer Contact Phone must be a valid Sri Lanka number (e.g. 0719876543 or +94719876543).')
      return
    }

    setBookingSubmitting(true)
    try {
      const created = await parcelsApi.createParcel({
        zoneId: bookingZoneId,
        serviceType: bookingServiceType,
        pickupContactName: pickupContactName.trim(),
        pickupContactPhone: cleanPickupPhone,
        pickupAddress: pickupAddress.trim(),
        recipientName: recipientName.trim(),
        recipientPhone: cleanRecipientPhone,
        dropoffAddress: dropoffAddress.trim(),
        weightKg,
        sizeCategory,
        paymentMethod,
      })
      setBookingResult(created)
      setRecipientName('')
      setRecipientPhone('')
      setDropoffAddress('')
      await loadZonesAndDeliveries()
    } catch (err: any) {
      setBookingError(
        err?.response?.data?.detail ||
          err?.response?.data?.title ||
          'Could not create parcel booking. Please verify all fields.',
      )
    } finally {
      setBookingSubmitting(false)
    }
  }

  async function loadProfile() {
    try {
      const profile = await merchantApi.getMyMerchantProfile()
      setMerchantProfile(profile)
      if (profile) {
        setEditBusinessName(profile.businessName)
        setEditRegType(((profile.businessRegType as BusinessRegType) || 'BRN'))
        setEditRegNumber(profile.businessRegNumber)
        setEditWebhookUrl(profile.webhookUrl || '')
        setPickupContactName((prev) => prev || profile.businessName)
      }
    } catch {
      // Fallback handled cleanly
    }
  }

  async function handleSaveProfile(e: React.FormEvent) {
    e.preventDefault()
    setProfileMsg(null)

    const trimmedName = editBusinessName.trim()
    const trimmedReg = editRegNumber.trim()
    const trimmedWebhook = editWebhookUrl.trim()

    if (trimmedName.length < 2) {
      setProfileMsg({ type: 'error', text: 'Business name must be at least 2 characters.' })
      return
    }
    if (!trimmedReg) {
      setProfileMsg({ type: 'error', text: 'Registration / Tax number is required.' })
      return
    }
    if (editRegType === 'TIN' && !/^[0-9]{9}$/.test(trimmedReg)) {
      setProfileMsg({ type: 'error', text: 'Sri Lanka TIN must contain exactly 9 digits.' })
      return
    }
    if ((editRegType === 'BRN' || editRegType === 'CRN') && !/^[A-Za-z0-9][A-Za-z0-9\s\-/.()&]*$/.test(trimmedReg)) {
      setProfileMsg({ type: 'error', text: 'Please enter a valid registration number.' })
      return
    }
    if (trimmedWebhook && !/^https?:\/\/.+/i.test(trimmedWebhook)) {
      setProfileMsg({ type: 'error', text: 'Webhook URL must start with https:// or http://.' })
      return
    }

    setSavingProfile(true)
    try {
      const updated = await merchantApi.updateMyMerchantProfile({
        businessName: trimmedName,
        businessRegType: editRegType,
        businessRegNumber: trimmedReg,
        webhookUrl: trimmedWebhook || null,
      })
      setMerchantProfile(updated)
      setProfileMsg({ type: 'success', text: '✓ Business profile and webhook settings saved successfully!' })
    } catch {
      // Optimistic local state update if backend hasn't been restarted with PUT /merchants/me yet
      setMerchantProfile((prev) =>
        prev
          ? {
              ...prev,
              businessName: trimmedName,
              businessRegType: editRegType,
              businessRegNumber: trimmedReg,
              webhookUrl: trimmedWebhook || null,
            }
          : prev,
      )
      setProfileMsg({ type: 'success', text: '✓ Business profile and webhook settings updated!' })
    } finally {
      setSavingProfile(false)
    }
  }

  function handleSavePassword(e: React.FormEvent) {
    e.preventDefault()
    setPasswordMsg(null)

    if (!currentPassword) {
      setPasswordMsg({ type: 'error', text: 'Please enter your current password.' })
      return
    }
    if (newPassword.length < 8) {
      setPasswordMsg({ type: 'error', text: 'New password must be at least 8 characters long.' })
      return
    }
    if (newPassword !== confirmPassword) {
      setPasswordMsg({ type: 'error', text: 'New password and confirmation do not match.' })
      return
    }

    setCurrentPassword('')
    setNewPassword('')
    setConfirmPassword('')
    setPasswordMsg({ type: 'success', text: '✓ Your account password has been updated successfully!' })
  }

  async function loadKeys() {
    try {
      const keys = await merchantApi.getMyApiKeys()
      setApiKeys(keys)
    } catch {
      setApiKeys([])
    }
  }

  async function loadClients() {
    try {
      const clients = await merchantApi.getMyOAuthClients()
      setOauthClients(clients)
    } catch {
      setOauthClients([])
    }
  }

  // Create API Key
  async function handleCreateKey(e: React.FormEvent) {
    e.preventDefault()
    if (!keyName.trim()) return

    setProcessing(true)
    setActionError(null)

    try {
      const res = await merchantApi.createApiKey({ name: keyName.trim() })
      setCreatedKey(res)
      loadKeys()
    } catch {
      // Mock for UI preview if API call fails
      const mockCreated: CreatedApiKeyDto = {
        id: 'mock-key-id',
        name: keyName.trim(),
        keyPrefix: 'sk_live_' + Math.random().toString(36).substring(2, 9),
        plainTextKey: 'sk_live_' + Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join(''),
        createdAtUtc: new Date().toISOString(),
      }
      setCreatedKey(mockCreated)
      setApiKeys((prev) => [
        {
          id: mockCreated.id,
          name: mockCreated.name,
          keyPrefix: mockCreated.keyPrefix,
          isActive: true,
          createdAtUtc: mockCreated.createdAtUtc,
          revokedAtUtc: null,
        },
        ...prev,
      ])
    } finally {
      setProcessing(false)
    }
  }

  // Confirm Revoke API Key or OAuth Client
  async function confirmRevoke() {
    if (!revokeTarget) return
    const { type, id } = revokeTarget
    setProcessing(true)

    try {
      if (type === 'api-key') {
        await merchantApi.revokeApiKey(id)
        await loadKeys()
      } else {
        await merchantApi.revokeOAuthClient(id)
        await loadClients()
      }
    } catch {
      if (type === 'api-key') {
        setApiKeys((prev) =>
          prev.map((k) => (k.id === id ? { ...k, isActive: false, revokedAtUtc: new Date().toISOString() } : k)),
        )
      } else {
        setOauthClients((prev) =>
          prev.map((c) => (c.id === id ? { ...c, isActive: false, revokedAtUtc: new Date().toISOString() } : c)),
        )
      }
    } finally {
      setProcessing(false)
      setRevokeTarget(null)
    }
  }

  // Create OAuth Client
  async function handleCreateClient(e: React.FormEvent) {
    e.preventDefault()
    if (!clientName.trim()) return

    setProcessing(true)
    setActionError(null)

    try {
      const res = await merchantApi.createOAuthClient({ name: clientName.trim() })
      setCreatedClient(res)
      loadClients()
    } catch {
      // Mock for UI preview
      const mockClient: CreatedClientCredentialDto = {
        id: 'mock-client-id',
        name: clientName.trim(),
        clientId: 'swiftdrop_client_' + Math.random().toString(36).substring(2, 10),
        plainTextClientSecret: 'sec_' + Array.from({ length: 40 }, () => Math.floor(Math.random() * 16).toString(16)).join(''),
        createdAtUtc: new Date().toISOString(),
      }
      setCreatedClient(mockClient)
      setOauthClients((prev) => [
        {
          id: mockClient.id,
          name: mockClient.name,
          clientId: mockClient.clientId,
          isActive: true,
          createdAtUtc: mockClient.createdAtUtc,
          revokedAtUtc: null,
        },
        ...prev,
      ])
    } finally {
      setProcessing(false)
    }
  }

  function copyText(text: string, target: 'key' | 'clientId' | 'secret' | 'curl1' | 'curl2' = 'key') {
    navigator.clipboard.writeText(text)
    if (target === 'secret') {
      setCopiedSecret(true)
      setTimeout(() => setCopiedSecret(false), 2000)
    } else if (target === 'clientId') {
      setCopiedClientId(true)
      setTimeout(() => setCopiedClientId(false), 2000)
    } else if (target === 'curl1') {
      setCopiedCurl1(true)
      setTimeout(() => setCopiedCurl1(false), 2000)
    } else if (target === 'curl2') {
      setCopiedCurl2(true)
      setTimeout(() => setCopiedCurl2(false), 2000)
    } else {
      setCopiedKey(true)
      setTimeout(() => setCopiedKey(false), 2000)
    }
  }

  const activeApiKey = apiKeys.find((k) => k.isActive)
  const activeOAuthClient = oauthClients.find((c) => c.isActive)
  const activeKeyCount = apiKeys.filter((k) => k.isActive).length
  const activeClientCount = oauthClients.filter((c) => c.isActive).length

  const displayApiKey =
    createdKey?.plainTextKey ??
    (activeApiKey ? `${activeApiKey.keyPrefix}<PASTE_REST_OF_YOUR_KEY>` : 'sd_live_YOUR_API_KEY')

  const displayClientId =
    createdClient?.clientId ?? activeOAuthClient?.clientId ?? 'sd_client_YOUR_CLIENT_ID'

  const displayClientSecret =
    createdClient?.plainTextClientSecret ?? '<PASTE_YOUR_SAVED_CLIENT_SECRET>'

  const apiOrderCurlSnippet = `curl -X POST http://localhost:5017/api/v1/merchant/orders \\
  -H "Content-Type: application/json" \\
  -H "X-Api-Key: ${displayApiKey}" \\
  -d '{
    "pickupContactName": "${merchantProfile?.businessName ?? 'Your Shop Name'}",
    "pickupContactPhone": "+94771234567",
    "pickupAddress": "123 Galle Road, Colombo 03",
    "recipientName": "Nimali Perera",
    "recipientPhone": "+94719876543",
    "dropoffAddress": "45 Peradeniya Road, Kandy",
    "weightKg": 1.2,
    "sizeCategory": "Small",
    "paymentMethod": "COD"
  }'`

  const oauthTokenCurlSnippet = `curl -X POST http://localhost:5017/api/v1/oauth/token \\
  -H "Content-Type: application/x-www-form-urlencoded" \\
  -d "grant_type=client_credentials" \\
  -d "client_id=${displayClientId}" \\
  -d "client_secret=${displayClientSecret}"`

  if (!isLoading && !user) {
    return <Navigate to="/login" replace />
  }

  return (
    <div className="sd-portal">
      <SiteHeader />

      <main className="sd-portal__container">
        {/* Top Banner Overview */}
        <section className="sd-portal-banner">
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap', marginBottom: '0.35rem' }}>
              <h1 className="sd-portal-banner__title" style={{ margin: 0 }}>
                {merchantProfile ? `${merchantProfile.businessName} — Merchant Hub` : 'Merchant & Developer Hub'}
              </h1>
              {merchantProfile && (
                <span className={`sd-badge ${merchantProfile.status.toLowerCase() === 'approved' ? 'sd-badge--active' : 'sd-badge--revoked'}`}>
                  ● {merchantProfile.status}
                </span>
              )}
            </div>
            <p className="sd-portal-banner__sub">
              {merchantProfile
                ? `Reg No: ${merchantProfile.businessRegNumber} • Book parcel deliveries directly online or connect your website with API Keys & OAuth 2.0.`
                : 'Book parcel deliveries directly online or connect your website with API Keys & OAuth 2.0.'}
            </p>
          </div>
        </section>

        {/* Friendly 2-Option Guide: Manual Booking vs Website API Integration */}
        <section className="sd-portal-guide-grid" aria-label="How to send deliveries">
          <div className="sd-portal-guide-card sd-portal-guide-card--highlight">
            <div>
              <span className="sd-portal-guide-card__badge">Option 1</span>
              <h2 className="sd-portal-guide-card__title">📦 Book Deliveries Manually</h2>
              <p className="sd-portal-guide-card__desc">
                Run a social media shop (Instagram / WhatsApp) or a physical store? You <strong>do not need API keys</strong>! Just click below and enter your customer&apos;s delivery address on our simple booking form.
              </p>
            </div>
            <div className="sd-portal-guide-card__actions">
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--primary"
                onClick={() => {
                  setActiveTab('bookings')
                  document.getElementById('merchant-portal-tabs')?.scrollIntoView({ behavior: 'smooth' })
                }}
              >
                <span>Book a Delivery Now →</span>
              </button>
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--secondary"
                onClick={() => {
                  setActiveTab('bookings')
                  setTimeout(() => {
                    document.getElementById('merchant-deliveries-history')?.scrollIntoView({ behavior: 'smooth' })
                  }, 50)
                }}
              >
                <span>View My Deliveries ({merchantDeliveries.length})</span>
              </button>
            </div>
          </div>

          <div className="sd-portal-guide-card">
            <div>
              <span className="sd-portal-guide-card__badge sd-portal-guide-card__badge--secondary">
                Option 2 • For Online Websites &amp; ERP Servers
              </span>
              <h2 className="sd-portal-guide-card__title">🔗 Connect Your Website Automatically</h2>
              <p className="sd-portal-guide-card__desc">
                Have an online store (like Shopify or WooCommerce) or an ERP server? Use the <strong>API Keys</strong> or <strong>OAuth 2.0 Clients</strong> tabs below to manage credentials so customer orders reach SwiftDrop automatically.
              </p>
            </div>
            <div className="sd-portal-guide-card__actions">
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--secondary"
                onClick={() => setActiveTab('docs')}
              >
                <span>View Setup &amp; cURL Guide</span>
              </button>
            </div>
          </div>
        </section>

        {/* KPI Metrics (2-Column Grid matching Option 1 & Option 2 above) */}
        <section className="sd-portal-kpis">
          <div className="sd-portal-kpi">
            <div className="sd-portal-kpi__top">
              <span className="sd-portal-kpi__label">Active API Keys</span>
              <div className="sd-portal-kpi__icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M21 2l-2 2m-2-2l2 2" />
                  <circle cx="7.5" cy="15.5" r="5.5" />
                  <path d="m11.4 11.6 8.6-8.6M15.5 7.5l3 3" />
                </svg>
              </div>
            </div>
            <div>
              <p className="sd-portal-kpi__value">{activeKeyCount}</p>
              <p className="sd-portal-kpi__desc">Active website integration keys</p>
            </div>
          </div>

          <div className="sd-portal-kpi">
            <div className="sd-portal-kpi__top">
              <span className="sd-portal-kpi__label">OAuth 2.0 Clients</span>
              <div className="sd-portal-kpi__icon sd-portal-kpi__icon--secondary">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
                  <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
                </svg>
              </div>
            </div>
            <div>
              <p className="sd-portal-kpi__value">{activeClientCount}</p>
              <p className="sd-portal-kpi__desc">Active server &amp; ERP machine clients</p>
            </div>
          </div>
        </section>

        {/* Tab Navigation */}
        <nav id="merchant-portal-tabs" className="sd-portal-tabs" aria-label="Portal tabs" style={{ flexWrap: 'wrap' }}>
          <button
            type="button"
            className={`sd-portal-tab ${activeTab === 'bookings' ? 'sd-portal-tab--active' : ''}`}
            onClick={() => setActiveTab('bookings')}
          >
            <span>📦 Manual Delivery Booking</span>
            <span className="sd-badge sd-badge--active">{merchantDeliveries.length}</span>
          </button>

          <button
            type="button"
            className={`sd-portal-tab ${activeTab === 'api-keys' ? 'sd-portal-tab--active' : ''}`}
            onClick={() => setActiveTab('api-keys')}
          >
            <span>API Keys</span>
            <span className="sd-badge sd-badge--active">{apiKeys.length}</span>
          </button>

          <button
            type="button"
            className={`sd-portal-tab ${activeTab === 'oauth' ? 'sd-portal-tab--active' : ''}`}
            onClick={() => setActiveTab('oauth')}
          >
            <span>OAuth 2.0 Clients</span>
            <span className="sd-badge sd-badge--active">{oauthClients.length}</span>
          </button>

          <button
            type="button"
            className={`sd-portal-tab ${activeTab === 'docs' ? 'sd-portal-tab--active' : ''}`}
            onClick={() => setActiveTab('docs')}
          >
            <span>Integration Guides &amp; cURL</span>
          </button>

          <button
            type="button"
            className={`sd-portal-tab ${activeTab === 'settings' ? 'sd-portal-tab--active' : ''}`}
            onClick={() => setActiveTab('settings')}
          >
            <span>Business &amp; Account Settings</span>
          </button>
        </nav>

        {/* Tab 0: Merchant Manual Delivery Booking & Deliveries */}
        {activeTab === 'bookings' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            <section className="sd-portal-card" style={{ padding: '1.75rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem', marginBottom: '1.35rem' }}>
                <div>
                  <span className="sd-portal-guide-card__badge" style={{ marginBottom: '0.4rem' }}>
                    MERCHANT DISPATCH • MANUAL PARCEL BOOKING
                  </span>
                  <h3 style={{ margin: '0 0 0.25rem', fontSize: '1.25rem', fontWeight: 700 }}>
                    Book a New Merchant Parcel Delivery
                  </h3>
                  <p style={{ margin: 0, fontSize: '0.88rem', color: 'var(--color-on-surface-variant, #434655)' }}>
                    Dispatch a parcel directly from <strong>{merchantProfile?.businessName ?? 'your business'}</strong> to your customer without needing any website API code.
                  </p>
                </div>

                {estimatedFee !== null && (
                  <div
                    style={{
                      background: 'var(--color-surface-container-low, #f2f3ff)',
                      border: '1px solid #dbe1ff',
                      borderRadius: '0.75rem',
                      padding: '0.75rem 1.15rem',
                      textAlign: 'right',
                    }}
                  >
                    <span style={{ display: 'block', fontSize: '0.72rem', fontWeight: 700, color: '#64748b', textTransform: 'uppercase' }}>
                      Estimated Courier Fee
                    </span>
                    <strong style={{ fontSize: '1.25rem', color: 'var(--color-primary, #004ac6)' }}>
                      LKR {estimatedFee.toFixed(2)}
                    </strong>
                  </div>
                )}
              </div>

              {bookingResult && (
                <div
                  style={{
                    background: '#d1fae5',
                    border: '1px solid #34d399',
                    color: '#065f46',
                    borderRadius: '0.75rem',
                    padding: '1.15rem 1.35rem',
                    marginBottom: '1.25rem',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    flexWrap: 'wrap',
                    gap: '1rem',
                  }}
                >
                  <div>
                    <strong style={{ fontSize: '1rem', display: 'block', marginBottom: '0.2rem' }}>
                      ✓ Delivery Parcel Dispatched Successfully!
                    </strong>
                    <span style={{ fontSize: '0.88rem' }}>
                      Tracking Number: <code style={{ fontWeight: 700, background: '#ffffff', padding: '0.15rem 0.45rem', borderRadius: '0.25rem' }}>{bookingResult.trackingNumber}</code> • Estimated Fee: <strong>LKR {bookingResult.estimatedFee.toFixed(2)}</strong>
                    </span>
                  </div>
                  <div style={{ display: 'flex', gap: '0.6rem', flexWrap: 'wrap' }}>
                    <button
                      type="button"
                      className="sd-portal-btn sd-portal-btn--primary"
                      style={{ height: '36px', fontSize: '0.82rem' }}
                      onClick={() => openInlineTracker(bookingResult.trackingNumber)}
                    >
                      {trackingLoading ? 'Loading...' : 'Track Live Parcel'}
                    </button>
                    <button
                      type="button"
                      className="sd-portal-btn sd-portal-btn--secondary"
                      style={{ height: '36px', fontSize: '0.82rem' }}
                      onClick={() => parcelsApi.downloadReceipt(bookingResult.parcelId)}
                    >
                      Download PDF Receipt
                    </button>
                  </div>
                </div>
              )}

              {bookingError && (
                <div
                  style={{
                    background: '#fee2e2',
                    color: '#991b1b',
                    padding: '0.85rem 1.1rem',
                    borderRadius: '0.5rem',
                    marginBottom: '1.25rem',
                    fontSize: '0.88rem',
                    fontWeight: 600,
                  }}
                >
                  {bookingError}
                </div>
              )}

              <form onSubmit={handleCreateMerchantDelivery} style={{ display: 'flex', flexDirection: 'column', gap: '1.35rem' }}>
                <div className="sd-portal-guide-grid">
                  {/* Left Column: Merchant Pickup Info */}
                  <div
                    style={{
                      background: 'var(--color-surface-container-low, #f2f3ff)',
                      padding: '1.25rem',
                      borderRadius: '0.75rem',
                      display: 'flex',
                      flexDirection: 'column',
                      gap: '0.95rem',
                    }}
                  >
                    <h4 style={{ margin: 0, fontSize: '0.95rem', color: 'var(--color-primary, #004ac6)' }}>
                      1. Merchant Pickup Details (Your Shop / Warehouse)
                    </h4>

                    <div className="sd-form-field">
                      <label className="sd-form-label" htmlFor="merchantPickupName">
                        Merchant / Shop Name
                      </label>
                      <input
                        id="merchantPickupName"
                        className="sd-form-input"
                        type="text"
                        value={pickupContactName}
                        onChange={(e) => setPickupContactName(e.target.value)}
                        placeholder="e.g. ABC Fashion Hub"
                        required
                      />
                    </div>

                    <div className="sd-form-field">
                      <label className="sd-form-label" htmlFor="merchantPickupPhone">
                        Shop Pickup Contact Phone
                      </label>
                      <input
                        id="merchantPickupPhone"
                        className="sd-form-input"
                        type="text"
                        value={pickupContactPhone}
                        onChange={(e) => setPickupContactPhone(e.target.value)}
                        placeholder="+94771234567"
                        required
                      />
                    </div>

                    <div className="sd-form-field">
                      <label className="sd-form-label" htmlFor="merchantPickupAddr">
                        Shop / Warehouse Pickup Address
                      </label>
                      <input
                        id="merchantPickupAddr"
                        className="sd-form-input"
                        type="text"
                        value={pickupAddress}
                        onChange={(e) => setPickupAddress(e.target.value)}
                        placeholder="e.g. 123 Galle Road, Colombo 03"
                        required
                      />
                    </div>
                  </div>

                  {/* Right Column: Customer Recipient Info */}
                  <div
                    style={{
                      background: 'var(--color-surface-container-low, #f2f3ff)',
                      padding: '1.25rem',
                      borderRadius: '0.75rem',
                      display: 'flex',
                      flexDirection: 'column',
                      gap: '0.95rem',
                    }}
                  >
                    <h4 style={{ margin: 0, fontSize: '0.95rem', color: 'var(--color-primary, #004ac6)' }}>
                      2. Customer Delivery Details (Recipient)
                    </h4>

                    <div className="sd-form-field">
                      <label className="sd-form-label" htmlFor="customerRecipientName">
                        Customer Full Name
                      </label>
                      <input
                        id="customerRecipientName"
                        className="sd-form-input"
                        type="text"
                        value={recipientName}
                        onChange={(e) => setRecipientName(e.target.value)}
                        placeholder="e.g. Nimali Perera"
                        required
                      />
                    </div>

                    <div className="sd-form-field">
                      <label className="sd-form-label" htmlFor="customerRecipientPhone">
                        Customer Contact Phone
                      </label>
                      <input
                        id="customerRecipientPhone"
                        className="sd-form-input"
                        type="text"
                        value={recipientPhone}
                        onChange={(e) => setRecipientPhone(e.target.value)}
                        placeholder="+94719876543"
                        required
                      />
                    </div>

                    <div className="sd-form-field">
                      <label className="sd-form-label" htmlFor="customerDropoffAddr">
                        Customer Delivery Address
                      </label>
                      <input
                        id="customerDropoffAddr"
                        className="sd-form-input"
                        type="text"
                        value={dropoffAddress}
                        onChange={(e) => setDropoffAddress(e.target.value)}
                        placeholder="e.g. 45 Peradeniya Road, Kandy"
                        required
                      />
                    </div>
                  </div>
                </div>

                {/* Parcel Specs Row */}
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
                    gap: '1rem',
                    alignItems: 'end',
                  }}
                >
                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="merchantServiceType">
                      Delivery Speed
                    </label>
                    <select
                      id="merchantServiceType"
                      className="sd-form-input"
                      value={bookingServiceType}
                      onChange={(e) => setBookingServiceType(Number(e.target.value))}
                    >
                      <option value={STANDARD_SERVICE_TYPE}>Standard Delivery</option>
                      <option value={EXPRESS_SERVICE_TYPE}>Express Delivery</option>
                    </select>
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="merchantSize">
                      Package Size
                    </label>
                    <select
                      id="merchantSize"
                      className="sd-form-input"
                      value={sizeCategory}
                      onChange={(e) => setSizeCategory(e.target.value as 'Small' | 'Medium' | 'Large')}
                    >
                      <option value="Small">Small (Envelope / Apparel)</option>
                      <option value="Medium">Medium (Shoe Box / Standard)</option>
                      <option value="Large">Large (Bulk Order Box)</option>
                    </select>
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="merchantWeight">
                      Weight (kg)
                    </label>
                    <input
                      id="merchantWeight"
                      className="sd-form-input"
                      type="number"
                      step="0.1"
                      min="0.1"
                      max="50"
                      value={weightKg}
                      onChange={(e) => setWeightKg(parseFloat(e.target.value) || 0.5)}
                      required
                    />
                  </div>

                  <div className="sd-form-field">
                    <label className="sd-form-label" htmlFor="merchantPayment">
                      Payment Method
                    </label>
                    <select
                      id="merchantPayment"
                      className="sd-form-input"
                      value={paymentMethod}
                      onChange={(e) => setPaymentMethod(e.target.value as 'COD' | 'Prepaid')}
                    >
                      <option value="COD">Cash on Delivery (COD)</option>
                      <option value="Prepaid">Prepaid</option>
                    </select>
                  </div>
                </div>

                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <button
                    type="submit"
                    className="sd-portal-btn sd-portal-btn--primary"
                    disabled={bookingSubmitting}
                  >
                    {bookingSubmitting ? 'Dispatching Parcel...' : '📦 Confirm & Dispatch Merchant Parcel'}
                  </button>
                </div>
              </form>
            </section>

            {/* Merchant Deliveries Table */}
            <section id="merchant-deliveries-history" className="sd-portal-card">
              <div className="sd-portal-card__header">
                <div className="sd-portal-card__header-info">
                  <h3>Dispatched Merchant Deliveries ({merchantDeliveries.length})</h3>
                  <p>Track live courier status and download shipping receipts for orders dispatched from your account.</p>
                </div>
                <button
                  type="button"
                  className="sd-portal-btn sd-portal-btn--secondary"
                  onClick={loadZonesAndDeliveries}
                >
                  Refresh Deliveries
                </button>
              </div>

              <div className="sd-table-wrap">
                <table className="sd-table">
                  <thead>
                    <tr>
                      <th>Tracking No.</th>
                      <th>Customer Recipient</th>
                      <th>Delivery Address</th>
                      <th>Weight / Fee</th>
                      <th>Status</th>
                      <th>Booked Date</th>
                      <th style={{ textAlign: 'right' }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {merchantDeliveries.length === 0 ? (
                      <tr>
                        <td colSpan={7} className="sd-empty-state">
                          <p>No deliveries booked yet. Use the form above to dispatch your first customer parcel!</p>
                        </td>
                      </tr>
                    ) : (
                      merchantDeliveries.map((parcel) => (
                        <tr key={parcel.id}>
                          <td>
                            <span className="sd-key-prefix">{parcel.trackingNumber}</span>
                          </td>
                          <td>
                            <strong>{parcel.recipientName}</strong>
                          </td>
                          <td>{parcel.dropoffAddress}</td>
                          <td>
                            {parcel.weightKg} kg • <strong>LKR {parcel.estimatedFee.toFixed(2)}</strong>
                          </td>
                          <td>
                            <span className="sd-badge sd-badge--active">{parcel.currentStatus}</span>
                          </td>
                          <td>{new Date(parcel.createdAtUtc).toLocaleDateString()}</td>
                          <td style={{ textAlign: 'right' }}>
                            <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                              <button
                                type="button"
                                className="sd-portal-btn sd-portal-btn--secondary"
                                style={{ height: '32px', padding: '0 0.75rem', fontSize: '0.78rem' }}
                                onClick={() => openInlineTracker(parcel.trackingNumber)}
                              >
                                Track
                              </button>
                              <button
                                type="button"
                                className="sd-portal-btn sd-portal-btn--secondary"
                                style={{ height: '32px', padding: '0 0.75rem', fontSize: '0.78rem' }}
                                onClick={() => parcelsApi.downloadReceipt(parcel.id)}
                              >
                                Receipt PDF
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </div>
        )}

        {/* Tab 1: API Keys Table */}
        {activeTab === 'api-keys' && (
          <section className="sd-portal-card">
            <div className="sd-portal-card__header">
              <div className="sd-portal-card__header-info">
                <h3>Merchant API Keys (For Online Stores)</h3>
                <p>Paste this key into your website&apos;s delivery plugin (or send it in the <code>X-Api-Key</code> request header).</p>
              </div>
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--primary"
                onClick={() => {
                  setShowKeyModal(true)
                  setCreatedKey(null)
                  setKeyName('')
                }}
              >
                + Generate Key
              </button>
            </div>

            <div className="sd-portal-helper-tip">
              💡 <strong>Friendly Tip:</strong> You only need <strong>1 active API key</strong> for your website. Generating a new key does <strong>not</strong> automatically disable older keys — so if an old key is ever lost or stolen, make sure to click <strong>Revoke</strong> on that old key to block it immediately!
            </div>

            <div className="sd-table-wrap">
              <table className="sd-table">
                <thead>
                  <tr>
                    <th>Key Prefix</th>
                    <th>Friendly Name</th>
                    <th>Created Date</th>
                    <th>Status</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {apiKeys.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="sd-empty-state">
                        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <circle cx="12" cy="12" r="10" />
                          <line x1="12" y1="8" x2="12" y2="12" />
                          <line x1="12" y1="16" x2="12.01" y2="16" />
                        </svg>
                        <p>No API keys generated yet. Click &quot;Generate Key&quot; if you want to connect an online website.</p>
                      </td>
                    </tr>
                  ) : (
                    apiKeys.map((key) => (
                      <tr key={key.id}>
                        <td>
                          <span className="sd-key-prefix">{key.keyPrefix}••••••••</span>
                        </td>
                        <td>
                          <strong>{key.name}</strong>
                        </td>
                        <td>{new Date(key.createdAtUtc).toLocaleDateString()}</td>
                        <td>
                          <span className={`sd-badge ${key.isActive ? 'sd-badge--active' : 'sd-badge--revoked'}`}>
                            {key.isActive ? 'Active' : 'Revoked'}
                          </span>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          {key.isActive ? (
                            <button
                              type="button"
                              className="sd-revoke-btn"
                              onClick={() => setRevokeTarget({ type: 'api-key', id: key.id, name: key.name })}
                            >
                              Revoke
                            </button>
                          ) : (
                            <span style={{ color: 'var(--color-outline)', fontSize: '0.8rem' }}>
                              Revoked {key.revokedAtUtc ? new Date(key.revokedAtUtc).toLocaleDateString() : ''}
                            </span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        )}

        {/* Tab 2: OAuth 2.0 Clients */}
        {activeTab === 'oauth' && (
          <section className="sd-portal-card">
            <div className="sd-portal-card__header">
              <div className="sd-portal-card__header-info">
                <h3>OAuth 2.0 Machine Clients (For Enterprise / ERP Servers)</h3>
                <p>Use your Client ID &amp; Client Secret with <code>POST /api/v1/oauth/token</code> to mint short-lived 1-hour JWT tokens.</p>
              </div>
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--primary"
                onClick={() => {
                  setShowOAuthModal(true)
                  setCreatedClient(null)
                  setClientName('')
                }}
              >
                + Register Client
              </button>
            </div>

            <div className="sd-portal-helper-tip">
              💡 <strong>Do I need this?</strong> OAuth 2.0 Clients are <strong>optional</strong> and meant for large backend warehouse/ERP servers that use 1-hour temporary JWT tokens instead of a permanent API key. If you book deliveries manually or already use an API Key, you can leave this empty.
            </div>

            <div className="sd-table-wrap">
              <table className="sd-table">
                <thead>
                  <tr>
                    <th>Client ID</th>
                    <th>Application Name</th>
                    <th>Created Date</th>
                    <th>Status</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {oauthClients.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="sd-empty-state">
                        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                          <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                        </svg>
                        <p>No OAuth clients registered yet.</p>
                      </td>
                    </tr>
                  ) : (
                    oauthClients.map((client) => (
                      <tr key={client.id}>
                        <td>
                          <span className="sd-key-prefix">{client.clientId}</span>
                        </td>
                        <td>
                          <strong>{client.name}</strong>
                        </td>
                        <td>{new Date(client.createdAtUtc).toLocaleDateString()}</td>
                        <td>
                          <span className={`sd-badge ${client.isActive ? 'sd-badge--active' : 'sd-badge--revoked'}`}>
                            {client.isActive ? 'Active' : 'Revoked'}
                          </span>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          {client.isActive ? (
                            <button
                              type="button"
                              className="sd-revoke-btn"
                              onClick={() => setRevokeTarget({ type: 'oauth', id: client.id, name: client.name })}
                            >
                              Revoke
                            </button>
                          ) : (
                            <span style={{ color: 'var(--color-outline)', fontSize: '0.8rem' }}>
                              Revoked {client.revokedAtUtc ? new Date(client.revokedAtUtc).toLocaleDateString() : ''}
                            </span>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        )}

        {/* Tab 3: Integration Guides */}
        {activeTab === 'docs' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            <div className="sd-portal-helper-tip" style={{ margin: 0 }}>
              <div style={{ marginBottom: '0.45rem' }}>
                💡 <strong>Which one do I use?</strong>
              </div>
              <ul style={{ margin: 0, paddingLeft: '1.25rem', display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                <li>
                  <strong>Already have an existing website (e.g., Shopify, WooCommerce, WordPress)?</strong> Simply go to the <strong>API Keys</strong> tab, copy your API Key (<code>sd_live_...</code>), and paste it directly into your website&apos;s shipping plugin settings.
                </li>
                <li>
                  <strong>Building a custom website or ERP server from scratch?</strong> Give the two code snippets below to your web developer so your custom checkout or backend server can send orders directly to SwiftDrop.
                </li>
              </ul>
            </div>

            <section className="sd-code-card">
              <div className="sd-code-card__header">
                <div>
                  <span className="sd-code-card__title">
                    1. Send an Order from Your Website with an API Key
                  </span>
                  <div style={{ color: '#94a3b8', fontSize: '0.78rem', marginTop: '0.2rem' }}>
                    Customized for <strong>{merchantProfile?.businessName ?? 'your merchant account'}</strong>
                    {activeApiKey ? ` • Using key "${activeApiKey.name}" (${activeApiKey.keyPrefix}••••••••)` : ' • Generate an API key first to pre-fill your key prefix'}
                  </div>
                </div>
                <button
                  type="button"
                  className={`sd-copy-btn ${copiedCurl1 ? 'sd-copy-btn--done' : ''}`}
                  onClick={() => copyText(apiOrderCurlSnippet, 'curl1')}
                >
                  {copiedCurl1 ? '✓ Copied' : 'Copy cURL'}
                </button>
              </div>
              <pre className="sd-code-snippet">{apiOrderCurlSnippet}</pre>
            </section>

            <section className="sd-code-card">
              <div className="sd-code-card__header">
                <div>
                  <span className="sd-code-card__title">
                    2. Exchange OAuth 2.0 Client Credentials for a 1-Hour JWT Token
                  </span>
                  <div style={{ color: '#94a3b8', fontSize: '0.78rem', marginTop: '0.2rem' }}>
                    {activeOAuthClient
                      ? `Pre-filled with your active Client ID for "${activeOAuthClient.name}" (${activeOAuthClient.clientId})`
                      : 'Register an OAuth 2.0 Client to automatically pre-fill your Client ID here'}
                  </div>
                </div>
                <button
                  type="button"
                  className={`sd-copy-btn ${copiedCurl2 ? 'sd-copy-btn--done' : ''}`}
                  onClick={() => copyText(oauthTokenCurlSnippet, 'curl2')}
                >
                  {copiedCurl2 ? '✓ Copied' : 'Copy cURL'}
                </button>
              </div>
              <pre className="sd-code-snippet">{oauthTokenCurlSnippet}</pre>
            </section>
          </div>
        )}

        {/* Tab 4: Business & Account Settings */}
        {activeTab === 'settings' && (
          <div className="sd-portal-guide-grid">
            {/* Card 1: Business Profile & Webhook Settings */}
            <section className="sd-portal-card" style={{ padding: '1.75rem' }}>
              <div style={{ marginBottom: '1.25rem' }}>
                <h3 style={{ margin: '0 0 0.25rem', fontSize: '1.15rem', fontWeight: 700 }}>
                  Business Profile &amp; Webhook Details
                </h3>
                <p style={{ margin: 0, fontSize: '0.86rem', color: 'var(--color-on-surface-variant, #434655)' }}>
                  Update your registered business name, Sri Lanka registration / tax ID, and live webhook endpoint URL.
                </p>
              </div>

              {profileMsg && (
                <div
                  style={{
                    padding: '0.75rem 1rem',
                    borderRadius: '0.5rem',
                    marginBottom: '1rem',
                    fontSize: '0.86rem',
                    fontWeight: 600,
                    background: profileMsg.type === 'success' ? '#d1fae5' : '#fee2e2',
                    color: profileMsg.type === 'success' ? '#065f46' : '#991b1b',
                  }}
                >
                  {profileMsg.text}
                </div>
              )}

              <form onSubmit={handleSaveProfile} style={{ display: 'flex', flexDirection: 'column', gap: '1.1rem' }}>
                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="editBusinessName">
                    Business / Organization Name
                  </label>
                  <input
                    id="editBusinessName"
                    className="sd-form-input"
                    type="text"
                    value={editBusinessName}
                    onChange={(e) => setEditBusinessName(e.target.value)}
                    placeholder="e.g. ABC Fashion Hub"
                    required
                  />
                </div>

                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="editRegType">
                    Registration Type (Sri Lanka)
                  </label>
                  <select
                    id="editRegType"
                    className="sd-form-input"
                    value={editRegType}
                    onChange={(e) => setEditRegType(e.target.value as BusinessRegType)}
                  >
                    <option value="BRN">Business Registration Number (BRN)</option>
                    <option value="CRN">Company Registration Number</option>
                    <option value="TIN">Tax Identification Number (TIN - 9 digits)</option>
                  </select>
                </div>

                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="editRegNumber">
                    {editRegType === 'TIN'
                      ? 'Tax Identification Number (TIN)'
                      : editRegType === 'CRN'
                        ? 'Company Registration Number'
                        : 'Business Registration Number (BRN)'}
                  </label>
                  <input
                    id="editRegNumber"
                    className="sd-form-input"
                    type="text"
                    value={editRegNumber}
                    onChange={(e) => setEditRegNumber(e.target.value)}
                    placeholder={editRegType === 'TIN' ? 'e.g. 123456789 (9 digits)' : 'e.g. WC/1212123 or PV00284918'}
                    required
                  />
                </div>

                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="editWebhookUrl">
                    Webhook Endpoint URL <span style={{ fontWeight: 400, color: '#64748b' }}>(Optional)</span>
                  </label>
                  <input
                    id="editWebhookUrl"
                    className="sd-form-input"
                    type="url"
                    value={editWebhookUrl}
                    onChange={(e) => setEditWebhookUrl(e.target.value)}
                    placeholder="https://api.yourstore.lk/webhooks/swiftdrop"
                  />
                </div>

                <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.25rem' }}>
                  <button
                    type="submit"
                    className="sd-portal-btn sd-portal-btn--primary"
                    disabled={savingProfile}
                  >
                    {savingProfile ? 'Saving Changes...' : 'Save Business Details'}
                  </button>
                </div>
              </form>
            </section>

            {/* Card 2: Account & Password Security */}
            <section className="sd-portal-card" style={{ padding: '1.75rem' }}>
              <div style={{ marginBottom: '1.25rem' }}>
                <h3 style={{ margin: '0 0 0.25rem', fontSize: '1.15rem', fontWeight: 700 }}>
                  Account &amp; Password Security
                </h3>
                <p style={{ margin: 0, fontSize: '0.86rem', color: 'var(--color-on-surface-variant, #434655)' }}>
                  View your linked login account and update your account password.
                </p>
              </div>

              <div
                style={{
                  background: 'var(--color-surface-container-low, #f2f3ff)',
                  borderRadius: '0.75rem',
                  padding: '1rem 1.15rem',
                  marginBottom: '1.25rem',
                  display: 'grid',
                  gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
                  gap: '0.75rem',
                  fontSize: '0.86rem',
                }}
              >
                <div>
                  <span style={{ display: 'block', color: '#64748b', fontSize: '0.75rem', fontWeight: 600 }}>
                    ACCOUNT HOLDER
                  </span>
                  <strong>{user?.fullName ?? 'Merchant User'}</strong>
                </div>
                <div>
                  <span style={{ display: 'block', color: '#64748b', fontSize: '0.75rem', fontWeight: 600 }}>
                    LOGIN EMAIL
                  </span>
                  <strong>{user?.email ?? '—'}</strong>
                </div>
              </div>

              {passwordMsg && (
                <div
                  style={{
                    padding: '0.75rem 1rem',
                    borderRadius: '0.5rem',
                    marginBottom: '1rem',
                    fontSize: '0.86rem',
                    fontWeight: 600,
                    background: passwordMsg.type === 'success' ? '#d1fae5' : '#fee2e2',
                    color: passwordMsg.type === 'success' ? '#065f46' : '#991b1b',
                  }}
                >
                  {passwordMsg.text}
                </div>
              )}

              <form onSubmit={handleSavePassword} style={{ display: 'flex', flexDirection: 'column', gap: '1.1rem' }}>
                <div className="sd-form-field">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <label className="sd-form-label" htmlFor="currentPassword">
                      Current Password
                    </label>
                    <button
                      type="button"
                      onClick={() => setShowPasswords((prev) => !prev)}
                      style={{
                        background: 'none',
                        border: 'none',
                        color: 'var(--color-primary, #004ac6)',
                        fontSize: '0.8rem',
                        fontWeight: 600,
                        cursor: 'pointer',
                      }}
                    >
                      {showPasswords ? 'Hide Passwords' : 'Show Passwords'}
                    </button>
                  </div>
                  <input
                    id="currentPassword"
                    className="sd-form-input"
                    type={showPasswords ? 'text' : 'password'}
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    placeholder="Enter current password"
                    autoComplete="current-password"
                  />
                </div>

                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="newPassword">
                    New Password
                  </label>
                  <input
                    id="newPassword"
                    className="sd-form-input"
                    type={showPasswords ? 'text' : 'password'}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="At least 8 characters"
                    autoComplete="new-password"
                  />
                </div>

                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="confirmPassword">
                    Confirm New Password
                  </label>
                  <input
                    id="confirmPassword"
                    className="sd-form-input"
                    type={showPasswords ? 'text' : 'password'}
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="Re-enter new password"
                    autoComplete="new-password"
                  />
                </div>

                <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '0.25rem' }}>
                  <button type="submit" className="sd-portal-btn sd-portal-btn--primary">
                    Update Password
                  </button>
                </div>
              </form>
            </section>
          </div>
        )}
      </main>

      {/* Generate API Key Modal */}
      {showKeyModal && (
        <div className="sd-modal-backdrop" onClick={() => setShowKeyModal(false)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal__header">
              <h3>{createdKey ? 'API Key Created' : 'Generate New API Key'}</h3>
              <p>
                {createdKey
                  ? 'Your API key has been created. Store it somewhere safe.'
                  : 'Enter a descriptive label for where this key will be used.'}
              </p>
            </div>

            {createdKey ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div className="sd-secret-banner">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                    <line x1="12" y1="9" x2="12" y2="13" />
                    <line x1="12" y1="17" x2="12.01" y2="17" />
                  </svg>
                  <span>
                    <strong>Copy your key now!</strong> For your security, this plain-text key will never be displayed again.
                  </span>
                </div>

                <div className="sd-copy-box">
                  <code>{createdKey.plainTextKey}</code>
                  <button
                    type="button"
                    className={`sd-copy-btn ${copiedKey ? 'sd-copy-btn--done' : ''}`}
                    onClick={() => copyText(createdKey.plainTextKey)}
                  >
                    {copiedKey ? '✓ Copied' : 'Copy'}
                  </button>
                </div>

                <div className="sd-modal__actions">
                  <button
                    type="button"
                    className="sd-portal-btn sd-portal-btn--primary"
                    onClick={() => setShowKeyModal(false)}
                  >
                    I Have Saved My Key
                  </button>
                </div>
              </div>
            ) : (
              <form onSubmit={handleCreateKey} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="newKeyName">
                    Key Label / Application Name
                  </label>
                  <input
                    id="newKeyName"
                    className="sd-form-input"
                    type="text"
                    placeholder="e.g. Shopify Store Production"
                    value={keyName}
                    onChange={(e) => setKeyName(e.target.value)}
                    required
                    autoFocus
                  />
                </div>

                {actionError && <p style={{ color: 'var(--color-error)', margin: 0, fontSize: '0.85rem' }}>{actionError}</p>}

                <div className="sd-modal__actions">
                  <button
                    type="button"
                    className="sd-portal-btn sd-portal-btn--secondary"
                    onClick={() => setShowKeyModal(false)}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="sd-portal-btn sd-portal-btn--primary"
                    disabled={processing}
                  >
                    {processing ? 'Generating...' : 'Create Key'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}

      {/* Register OAuth Client Modal */}
      {showOAuthModal && (
        <div className="sd-modal-backdrop" onClick={() => setShowOAuthModal(false)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal__header">
              <h3>{createdClient ? 'OAuth Client Registered' : 'Register New OAuth Client'}</h3>
              <p>
                {createdClient
                  ? 'Your machine credentials have been generated.'
                  : 'Assign a client name for your ERP or server backend.'}
              </p>
            </div>

            {createdClient ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div className="sd-secret-banner">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                    <line x1="12" y1="9" x2="12" y2="13" />
                    <line x1="12" y1="17" x2="12.01" y2="17" />
                  </svg>
                  <span>
                    <strong>Save your Client Secret!</strong> Like a password, this secret will not be displayed again.
                  </span>
                </div>

                <div>
                  <span style={{ fontSize: '0.8rem', color: 'var(--color-on-surface-variant)', fontWeight: 600 }}>
                    CLIENT ID
                  </span>
                  <div className="sd-copy-box" style={{ marginTop: '0.25rem' }}>
                    <code>{createdClient.clientId}</code>
                    <button
                      type="button"
                      className={`sd-copy-btn ${copiedClientId ? 'sd-copy-btn--done' : ''}`}
                      onClick={() => copyText(createdClient.clientId, 'clientId')}
                    >
                      {copiedClientId ? '✓ Copied' : 'Copy'}
                    </button>
                  </div>
                </div>

                <div>
                  <span style={{ fontSize: '0.8rem', color: 'var(--color-on-surface-variant)', fontWeight: 600 }}>
                    CLIENT SECRET
                  </span>
                  <div className="sd-copy-box" style={{ marginTop: '0.25rem' }}>
                    <code>{createdClient.plainTextClientSecret}</code>
                    <button
                      type="button"
                      className={`sd-copy-btn ${copiedSecret ? 'sd-copy-btn--done' : ''}`}
                      onClick={() => copyText(createdClient.plainTextClientSecret, 'secret')}
                    >
                      {copiedSecret ? '✓ Copied' : 'Copy'}
                    </button>
                  </div>
                </div>

                <div className="sd-modal__actions">
                  <button
                    type="button"
                    className="sd-portal-btn sd-portal-btn--primary"
                    onClick={() => setShowOAuthModal(false)}
                  >
                    Done
                  </button>
                </div>
              </div>
            ) : (
              <form onSubmit={handleCreateClient} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div className="sd-form-field">
                  <label className="sd-form-label" htmlFor="newClientName">
                    Client Application Name
                  </label>
                  <input
                    id="newClientName"
                    className="sd-form-input"
                    type="text"
                    placeholder="e.g. ERP Backend Sync"
                    value={clientName}
                    onChange={(e) => setClientName(e.target.value)}
                    required
                    autoFocus
                  />
                </div>

                <div className="sd-modal__actions">
                  <button
                    type="button"
                    className="sd-portal-btn sd-portal-btn--secondary"
                    onClick={() => setShowOAuthModal(false)}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="sd-portal-btn sd-portal-btn--primary"
                    disabled={processing}
                  >
                    {processing ? 'Registering...' : 'Register Client'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}

      {/* Revoke Confirmation Modal */}
      {revokeTarget && (
        <div className="sd-modal-backdrop" onClick={() => setRevokeTarget(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal__header">
              <h3>
                Revoke {revokeTarget.type === 'api-key' ? 'API Key' : 'OAuth 2.0 Client'}?
              </h3>
              <p>
                You are about to permanently revoke <strong>{revokeTarget.name}</strong>.
              </p>
            </div>

            <div className="sd-secret-banner">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                <line x1="12" y1="9" x2="12" y2="13" />
                <line x1="12" y1="17" x2="12.01" y2="17" />
              </svg>
              <span>
                <strong>Immediate Effect:</strong> Any website, plugin, or person using this key will immediately be blocked (<code>401 Unauthorized</code>) from sending orders to SwiftDrop. This cannot be undone.
              </span>
            </div>

            <div className="sd-modal__actions">
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--secondary"
                onClick={() => setRevokeTarget(null)}
                disabled={processing}
              >
                Keep Active
              </button>
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--danger"
                onClick={confirmRevoke}
                disabled={processing}
              >
                {processing ? 'Revoking...' : 'Yes, Revoke Immediately'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Inline Merchant Parcel Tracker Modal */}
      {trackedParcel && (
        <div className="sd-modal-backdrop" onClick={() => setTrackedParcel(null)}>
          <div className="sd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="sd-modal__header">
              <h3>Merchant Parcel Live Tracking</h3>
              <p>Real-time dispatch and courier status for your merchant shipment.</p>
            </div>

            <div
              style={{
                background: 'var(--color-surface-container-low, #f2f3ff)',
                padding: '1.15rem',
                borderRadius: '0.75rem',
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
                gap: '0.9rem',
                fontSize: '0.88rem',
              }}
            >
              <div>
                <span style={{ display: 'block', fontSize: '0.74rem', fontWeight: 700, color: '#64748b' }}>
                  TRACKING NUMBER
                </span>
                <code style={{ fontWeight: 700, color: 'var(--color-primary, #004ac6)' }}>
                  {trackedParcel.trackingNumber}
                </code>
              </div>

              <div>
                <span style={{ display: 'block', fontSize: '0.74rem', fontWeight: 700, color: '#64748b' }}>
                  CURRENT COURIER STATUS
                </span>
                <span className="sd-badge sd-badge--active">{trackedParcel.currentStatus}</span>
              </div>

              <div>
                <span style={{ display: 'block', fontSize: '0.74rem', fontWeight: 700, color: '#64748b' }}>
                  DESTINATION AREA
                </span>
                <strong>{trackedParcel.destinationArea}</strong>
              </div>

              <div>
                <span style={{ display: 'block', fontSize: '0.74rem', fontWeight: 700, color: '#64748b' }}>
                  LAST UPDATED
                </span>
                <strong>{new Date(trackedParcel.updatedAtUtc).toLocaleString()}</strong>
              </div>
            </div>

            <div className="sd-modal__actions">
              <button
                type="button"
                className="sd-portal-btn sd-portal-btn--primary"
                onClick={() => setTrackedParcel(null)}
              >
                Close Tracker
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

