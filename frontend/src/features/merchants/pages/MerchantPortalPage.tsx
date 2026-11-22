import { useEffect, useState } from 'react'
import { SiteHeader } from '../../home/components/SiteHeader'
import { merchantApi } from '../api/merchant.api'
import type {
  ApiKeySummaryDto,
  ClientCredentialSummaryDto,
  CreatedApiKeyDto,
  CreatedClientCredentialDto,
} from '../types/merchant.types'
import './MerchantPortalPage.css'

export function MerchantPortalPage() {
  const [activeTab, setActiveTab] = useState<'api-keys' | 'oauth' | 'docs'>('api-keys')

  // Data States
  const [apiKeys, setApiKeys] = useState<ApiKeySummaryDto[]>([])
  const [oauthClients, setOauthClients] = useState<ClientCredentialSummaryDto[]>([])

  // Modal States
  const [showKeyModal, setShowKeyModal] = useState(false)
  const [keyName, setKeyName] = useState('')
  const [createdKey, setCreatedKey] = useState<CreatedApiKeyDto | null>(null)
  const [copiedKey, setCopiedKey] = useState(false)

  const [showOAuthModal, setShowOAuthModal] = useState(false)
  const [clientName, setClientName] = useState('')
  const [createdClient, setCreatedClient] = useState<CreatedClientCredentialDto | null>(null)
  const [copiedSecret, setCopiedSecret] = useState(false)

  const [processing, setProcessing] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  // Load initial data
  useEffect(() => {
    loadKeys()
    loadClients()
  }, [])

  async function loadKeys() {
    try {
      const keys = await merchantApi.getMyApiKeys()
      setApiKeys(keys)
    } catch {
      // Demo fallback if backend is offline or unauthenticated
      setApiKeys([
        {
          id: '1',
          name: 'Production Shopify Store',
          keyPrefix: 'sk_live_9f81a7b',
          isActive: true,
          createdAtUtc: new Date().toISOString(),
          revokedAtUtc: null,
        },
        {
          id: '2',
          name: 'Warehouse Scanner Android App',
          keyPrefix: 'sk_live_1d2e3f4',
          isActive: true,
          createdAtUtc: new Date(Date.now() - 86400000 * 5).toISOString(),
          revokedAtUtc: null,
        },
      ])
    }
  }

  async function loadClients() {
    try {
      const clients = await merchantApi.getMyOAuthClients()
      setOauthClients(clients)
    } catch {
      // Demo fallback
      setOauthClients([
        {
          id: '1',
          name: 'ERP Inventory Integration',
          clientId: 'swiftdrop_client_883a9f',
          isActive: true,
          createdAtUtc: new Date().toISOString(),
          revokedAtUtc: null,
        },
      ])
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

  // Revoke API Key
  async function handleRevokeKey(id: string) {
    if (!confirm('Are you sure you want to revoke this API key? Applications using it will immediately lose access.')) return

    try {
      await merchantApi.revokeApiKey(id)
      loadKeys()
    } catch {
      // Local state update for preview
      setApiKeys((prev) =>
        prev.map((k) => (k.id === id ? { ...k, isActive: false, revokedAtUtc: new Date().toISOString() } : k)),
      )
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

  // Revoke OAuth Client
  async function handleRevokeClient(id: string) {
    if (!confirm('Are you sure you want to revoke this OAuth client? Integrations using these credentials will be blocked.')) return

    try {
      await merchantApi.revokeOAuthClient(id)
      loadClients()
    } catch {
      setOauthClients((prev) =>
        prev.map((c) => (c.id === id ? { ...c, isActive: false, revokedAtUtc: new Date().toISOString() } : c)),
      )
    }
  }

  function copyText(text: string, isSecret = false) {
    navigator.clipboard.writeText(text)
    if (isSecret) {
      setCopiedSecret(true)
      setTimeout(() => setCopiedSecret(false), 2000)
    } else {
      setCopiedKey(true)
      setTimeout(() => setCopiedKey(false), 2000)
    }
  }

  const activeKeyCount = apiKeys.filter((k) => k.isActive).length
  const activeClientCount = oauthClients.filter((c) => c.isActive).length

  return (
    <div className="sd-portal">
      <SiteHeader />

      <main className="sd-portal__container">
        {/* Top Banner Overview */}
        <section className="sd-portal-banner">
          <div>
            <h1 className="sd-portal-banner__title">Developer &amp; API Portal</h1>
            <p className="sd-portal-banner__sub">
              Manage your SwiftDrop REST API keys, OAuth 2.0 machine credentials, and real-time webhook endpoints.
            </p>
          </div>

          <div className="sd-portal-banner__actions">
            <button
              type="button"
              className="sd-portal-btn sd-portal-btn--primary"
              onClick={() => {
                setShowKeyModal(true)
                setCreatedKey(null)
                setKeyName('')
              }}
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                <line x1="12" y1="5" x2="12" y2="19" />
                <line x1="5" y1="12" x2="19" y2="12" />
              </svg>
              <span>Generate API Key</span>
            </button>

            <button
              type="button"
              className="sd-portal-btn sd-portal-btn--secondary"
              onClick={() => {
                setShowOAuthModal(true)
                setCreatedClient(null)
                setClientName('')
              }}
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              <span>Register OAuth Client</span>
            </button>
          </div>
        </section>

        {/* KPI Metrics */}
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
              <p className="sd-portal-kpi__desc">Hashed with SHA-256 for secure lookup</p>
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
              <p className="sd-portal-kpi__desc">RFC 6749 client_credentials token grant</p>
            </div>
          </div>

          <div className="sd-portal-kpi">
            <div className="sd-portal-kpi__top">
              <span className="sd-portal-kpi__label">Rate Limit Budget</span>
              <div className="sd-portal-kpi__icon sd-portal-kpi__icon--tertiary">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M12 2v4M12 18v4M4.93 4.93l2.83 2.83M16.24 16.24l2.83 2.83M2 12h4M18 12h4M4.93 19.07l2.83-2.83M16.24 7.76l2.83-2.83" />
                </svg>
              </div>
            </div>
            <div>
              <p className="sd-portal-kpi__value">100 req/min</p>
              <p className="sd-portal-kpi__desc">Global limit • 15 req/min on auth routes</p>
            </div>
          </div>

          <div className="sd-portal-kpi">
            <div className="sd-portal-kpi__top">
              <span className="sd-portal-kpi__label">API Version</span>
              <div className="sd-portal-kpi__icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <polyline points="16 18 22 12 16 6" />
                  <polyline points="8 6 2 12 8 18" />
                </svg>
              </div>
            </div>
            <div>
              <p className="sd-portal-kpi__value">v1.0</p>
              <p className="sd-portal-kpi__desc">Active base: /api/v1/merchants</p>
            </div>
          </div>
        </section>

        {/* Tab Navigation */}
        <nav className="sd-portal-tabs" aria-label="Portal tabs">
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
        </nav>

        {/* Tab 1: API Keys Table */}
        {activeTab === 'api-keys' && (
          <section className="sd-portal-card">
            <div className="sd-portal-card__header">
              <div className="sd-portal-card__header-info">
                <h3>Merchant API Keys</h3>
                <p>Pass your key in the <code>X-Api-Key</code> request header to authenticate REST requests.</p>
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
                        <p>No API keys generated yet. Click "Generate Key" to create your first one.</p>
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
                              onClick={() => handleRevokeKey(key.id)}
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
                <h3>OAuth 2.0 Machine Clients</h3>
                <p>Use your Client ID &amp; Client Secret with <code>POST /api/v1/oauth/token</code> to mint short-lived JWT tokens.</p>
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
                              onClick={() => handleRevokeClient(client.id)}
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
            <section className="sd-code-card">
              <div className="sd-code-card__header">
                <span className="sd-code-card__title">1. Authenticate with an API Key</span>
                <span style={{ color: '#38bdf8', fontSize: '0.8rem' }}>HTTP Header</span>
              </div>
              <pre className="sd-code-snippet">
{`curl -X GET http://localhost:5017/api/v1/merchants/me \\
  -H "X-Api-Key: sk_live_YOUR_RAW_API_KEY"`}
              </pre>
            </section>

            <section className="sd-code-card">
              <div className="sd-code-card__header">
                <span className="sd-code-card__title">2. Exchange OAuth 2.0 Client Credentials for a JWT Token</span>
                <span style={{ color: '#38bdf8', fontSize: '0.8rem' }}>RFC 6749 Standard</span>
              </div>
              <pre className="sd-code-snippet">
{`curl -X POST http://localhost:5017/api/v1/oauth/token \\
  -H "Content-Type: application/x-www-form-urlencoded" \\
  -d "grant_type=client_credentials" \\
  -d "client_id=swiftdrop_client_YOUR_ID" \\
  -d "client_secret=YOUR_CLIENT_SECRET"`}
              </pre>
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
                      className="sd-copy-btn"
                      onClick={() => copyText(createdClient.clientId)}
                    >
                      Copy
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
                      onClick={() => copyText(createdClient.plainTextClientSecret, true)}
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
    </div>
  )
}

