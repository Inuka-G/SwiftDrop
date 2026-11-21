
import axios from 'axios'
import { useEffect, useMemo, useRef, useState } from 'react'
import type { FormEvent, ReactNode, RefObject } from 'react'
import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query'
import { createPortal } from 'react-dom'
import { authService } from '../../../services/auth.service'
import {
  ZONE_DESCRIPTION_MAX_LENGTH,
  ZONE_NAME_MAX_LENGTH,
  ZONE_TOAST_DURATION_MS,
} from '../../../lib/constants'
import { zonesApi } from '../api/zones.api'
import type { ZoneDto } from '../types/zones.types'
import {
  filterZones,
  getHttpStatus,
  zoneErrorMessage,
  zoneInitials,
  zoneStats,
  type ZoneStatusFilter,
} from '../utils/zones.utils'
import { ZONES_QUERY_KEY } from '../hooks/useZones'
import './zones.css'

type Toast = { message: string; error?: boolean }
type FormState = { zoneName: string; description: string }

const EMPTY_ZONES: ZoneDto[] = []

function Modal({
  title,
  subtitle,
  onClose,
  children,
  footer,
  closeRef,
}: {
  title: string
  subtitle: string
  onClose: () => void
  children: ReactNode
  footer: ReactNode
  closeRef: RefObject<HTMLElement | null>
}) {
  const dialogRef = useRef<HTMLElement>(null)
  const onCloseRef = useRef(onClose)

  useEffect(() => {
    onCloseRef.current = onClose
  }, [onClose])

  useEffect(() => {
    const previousOverflow = document.body.style.overflow
    const returnFocus = closeRef.current
    const overlay = dialogRef.current?.parentElement

    const inertSiblings = Array.from(
      overlay?.parentElement?.children ?? [],
    ).filter(
      (node): node is HTMLElement =>
        node instanceof HTMLElement && node !== overlay,
    )

    const priorInert = inertSiblings.map((node) => node.inert)

    inertSiblings.forEach((node) => {
      node.inert = true
    })

    document.body.style.overflow = 'hidden'

    const timer = window.setTimeout(
      () => dialogRef.current?.querySelector<HTMLElement>('input,button')?.focus(),
      0,
    )

    const keydown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        onCloseRef.current()
        return
      }

      if (event.key !== 'Tab' || !dialogRef.current) return

      const items = Array.from(
        dialogRef.current.querySelectorAll<HTMLElement>(
          'button:not(:disabled),input:not(:disabled),textarea:not(:disabled),[tabindex="0"]',
        ),
      )

      if (!items.length) return

      if (event.shiftKey && document.activeElement === items[0]) {
        event.preventDefault()
        items.at(-1)?.focus()
      } else if (
        !event.shiftKey &&
        document.activeElement === items.at(-1)
      ) {
        event.preventDefault()
        items[0].focus()
      }
    }

    document.addEventListener('keydown', keydown)

    return () => {
      window.clearTimeout(timer)
      document.removeEventListener('keydown', keydown)
      document.body.style.overflow = previousOverflow

      inertSiblings.forEach((node, index) => {
        node.inert = priorInert[index]
      })

      returnFocus?.focus()
    }
  }, [closeRef])

  return createPortal(
    <div
      className="zones-overlay"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose()
      }}
    >
      <section
        ref={dialogRef}
        className="zones-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="zones-dialog-title"
      >
        <header className="zones-dialog-head">
          <span className="zones-dialog-icon" aria-hidden="true">⌖</span>

          <div>
            <h2 id="zones-dialog-title">{title}</h2>
            <p>{subtitle}</p>
          </div>

          <button
            type="button"
            className="zones-close"
            aria-label="Close dialog"
            onClick={onClose}
          >
            ×
          </button>
        </header>

        {children}

        <footer className="zones-dialog-foot">{footer}</footer>
      </section>
    </div>,
    document.querySelector('.admin-shell') ?? document.body,
  )
}

function ZoneFormDialog({
  zone,
  opener,
  onClose,
  onSaved,
  onError,
}: {
  zone: ZoneDto | null
  opener: HTMLElement | null
  onClose: () => void
  onSaved: (message: string) => void
  onError: (message: string) => void
}) {
  const queryClient = useQueryClient()

  const [form, setForm] = useState<FormState>({
    zoneName: zone?.zoneName ?? '',
    description: zone?.description ?? '',
  })

  const [active, setActive] = useState(zone?.isActive ?? true)
  const [error, setError] = useState('')
  const [confirmDeactivate, setConfirmDeactivate] = useState(false)
  const activeToggleRef = useRef<HTMLButtonElement>(null)

  const mutation = useMutation({
    mutationFn: async (value: FormState) => {
      if (!zone) {
        return zonesApi.create({
          zoneName: value.zoneName.trim(),
          description: value.description.trim() || null,
        })
      }

      const updated = await zonesApi.update(zone.id, {
        zoneName: value.zoneName.trim(),
        description: value.description.trim() || null,
      })

      if (active !== zone.isActive) {
        await zonesApi.setActive(zone.id, { isActive: active })
      }

      return updated
    },

    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ZONES_QUERY_KEY })
      await queryClient.invalidateQueries({
        queryKey: ['zone-pricing-rules'],
      })

      onSaved(zone ? 'Zone updated' : 'Zone created')
    },

    onError: (failure: unknown) => {
      const status = getHttpStatus(failure)

      if (status === 401) {
        authService.clearAccessToken()
        window.location.assign('/admin')
        return
      }

      setError(zoneErrorMessage(status))
      onError(zoneErrorMessage(status))
    },
  })

  const nameValid =
    Boolean(form.zoneName.trim()) &&
    form.zoneName.trim().length <= ZONE_NAME_MAX_LENGTH

  const changed =
    !zone ||
    form.zoneName.trim() !== zone.zoneName ||
    (form.description.trim() || '') !== (zone.description ?? '') ||
    active !== zone.isActive

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')

    if (!nameValid) {
      setError('Enter a zone name up to 100 characters.')
      return
    }

    await mutation.mutateAsync(form)
  }

  const cancel = () => {
    if (!mutation.isPending) onClose()
  }

  return (
    <Modal
      title={zone ? 'Edit zone' : 'Add New Delivery Zone'}
      subtitle={
        zone
          ? "Update the zone's name, rates or status"
          : 'Set the zone name and its shipping rates'
      }
      onClose={cancel}
      closeRef={{ current: opener }}
      footer={
        <>
          <button
            type="button"
            className="zones-button zones-button-muted"
            disabled={mutation.isPending}
            onClick={cancel}
          >
            Cancel
          </button>

          <button
            type="submit"
            form="zone-form"
            className="zones-button zones-button-primary"
            disabled={!changed || !nameValid || mutation.isPending}
          >
            {mutation.isPending
              ? 'Saving…'
              : zone
                ? 'Save changes'
                : '＋ Create zone'}
          </button>
        </>
      }
    >
      <form
        id="zone-form"
        className="zones-form"
        onSubmit={(event) => void submit(event)}
      >
        <label className="zones-field">
          <span>
            Zone name <b>*</b>
          </span>

          <input
            autoComplete="off"
            maxLength={ZONE_NAME_MAX_LENGTH}
            required
            aria-required="true"
            aria-invalid={Boolean(error)}
            aria-describedby={error ? 'zone-form-error' : undefined}
            value={form.zoneName}
            disabled={mutation.isPending}
            onChange={(event) =>
              setForm({ ...form, zoneName: event.target.value })
            }
            placeholder="e.g. Colombo Central"
          />

          {error && (
            <small
              id="zone-form-error"
              className="zones-field-error"
              role="alert"
            >
              {error}
            </small>
          )}
        </label>

        <label className="zones-field">
          <span>
            Description <small>Optional</small>
          </span>

          <textarea
            maxLength={ZONE_DESCRIPTION_MAX_LENGTH}
            rows={3}
            value={form.description}
            disabled={mutation.isPending}
            onChange={(event) =>
              setForm({ ...form, description: event.target.value })
            }
            placeholder="Short description of this delivery area"
          />
        </label>

        <section className="zones-rates-placeholder">
          <div className="zones-rate-title">
            <strong>Rates</strong>
            <span>Standard · Express</span>
          </div>

          <p>
            Pricing rules are displayed in the delivery zones table.
            Rate editing is handled separately.
          </p>
        </section>

        <div className="zones-active-card">
          <span className="zones-active-mark" aria-hidden="true">✓</span>

          <span>
            <strong>Zone active</strong>
            <small>Active zones can be used for new parcels.</small>
          </span>

          <button
            ref={activeToggleRef}
            className="zones-switch"
            type="button"
            role="switch"
            aria-checked={active}
            aria-label="Zone active"
            disabled={mutation.isPending}
            onClick={() => {
              if (zone?.isActive && active) {
                setConfirmDeactivate(true)
              } else {
                setActive(!active)
              }
            }}
          >
            <i />
          </button>
        </div>
      </form>

      {confirmDeactivate && zone && (
        <Modal
          title="Deactivate zone"
          subtitle={`${zone.zoneName} will no longer be used for new parcels.`}
          onClose={() => setConfirmDeactivate(false)}
          closeRef={activeToggleRef}
          footer={
            <>
              <button
                type="button"
                className="zones-button zones-button-muted"
                onClick={() => setConfirmDeactivate(false)}
              >
                Cancel
              </button>

              <button
                type="button"
                className="zones-button zones-button-danger"
                onClick={() => {
                  setActive(false)
                  setConfirmDeactivate(false)
                }}
              >
                Deactivate zone
              </button>
            </>
          }
        >
          <div className="zones-confirm-copy">
            This change affects new parcel bookings. Existing deliveries are
            not changed.
          </div>
        </Modal>
      )}
    </Modal>
  )
}

function DeactivateDialog({
  zone,
  close,
  confirm,
  busy,
  opener,
}: {
  zone: ZoneDto
  close: () => void
  confirm: () => void
  busy: boolean
  opener: HTMLElement | null
}) {
  return (
    <Modal
      title="Deactivate zone"
      subtitle={`${zone.zoneName} will no longer be used for new parcels.`}
      onClose={close}
      closeRef={{ current: opener }}
      footer={
        <>
          <button
            type="button"
            className="zones-button zones-button-muted"
            onClick={close}
            disabled={busy}
          >
            Cancel
          </button>

          <button
            type="button"
            className="zones-button zones-button-danger"
            onClick={confirm}
            disabled={busy}
          >
            {busy ? 'Updating…' : 'Deactivate zone'}
          </button>
        </>
      }
    >
      <div className="zones-confirm-copy">
        This change affects new parcel bookings. Existing deliveries are not
        changed.
      </div>
    </Modal>
  )
}

export function ZonesPricingPage() {
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: ZONES_QUERY_KEY,
    queryFn: zonesApi.list,
    staleTime: 30_000,
  })

  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<ZoneStatusFilter>('all')
  const [dialogZone, setDialogZone] = useState<ZoneDto | null | undefined>(
    undefined,
  )
  const [pendingDeactivate, setPendingDeactivate] =
    useState<ZoneDto | null>(null)
  const [opener, setOpener] = useState<HTMLElement | null>(null)
  const [toast, setToast] = useState<Toast | null>(null)

  const timerRef = useRef<number | undefined>(undefined)

  const toggle = useMutation({
    mutationFn: ({
      zone,
      isActive,
    }: {
      zone: ZoneDto
      isActive: boolean
    }) => zonesApi.setActive(zone.id, { isActive }),

    onSuccess: async (_data, variables) => {
      await queryClient.invalidateQueries({ queryKey: ZONES_QUERY_KEY })

      showToast(
        variables.isActive ? 'Zone activated' : 'Zone deactivated',
      )
    },

    onError: (failure: unknown) => {
      const code = getHttpStatus(failure)

      if (code === 401) {
        authService.clearAccessToken()
        window.location.assign('/admin')
        return
      }

      showToast(zoneErrorMessage(code), true)
    },
  })

  const allZones = query.data ?? EMPTY_ZONES

  // Fetch pricing rules separately for every delivery zone.
  const pricingQueries = useQueries({
    queries: allZones.map((zone) => ({
      queryKey: ['zone-pricing-rules', zone.id],
      queryFn: () => zonesApi.getPricingRules(zone.id),
      staleTime: 30_000,
    })),
  })

  const pricingByZone = new Map(
    allZones.map((zone, index) => [
      zone.id,
      pricingQueries[index]?.data ?? [],
    ]),
  )

  const visibleZones = useMemo(
    () => filterZones(allZones, search, status),
    [allZones, search, status],
  )

  const stats = zoneStats(allZones)

  const unauthorized =
    axios.isAxiosError(query.error) &&
    [401, 403].includes(query.error.response?.status ?? 0)

  function showToast(message: string, error = false) {
    setToast({ message, error })

    window.clearTimeout(timerRef.current)

    timerRef.current = window.setTimeout(
      () => setToast(null),
      ZONE_TOAST_DURATION_MS,
    )
  }

  function openForm(zone: ZoneDto | null, element: HTMLElement) {
    setOpener(element)
    setDialogZone(zone)
  }

  async function refresh() {
    await query.refetch()
  }

  return (
    <main className="zones-page">
      <header className="zones-page-head">
        <div>
          <span className="zones-eyebrow">PRICING &amp; ZONES</span>
          <h1>Delivery Zones &amp; Tariff Pricing</h1>
          <p>Manage delivery zones and their shipping rates.</p>
        </div>

        <button
          type="button"
          className="zones-button zones-button-primary"
          onClick={(event) => openForm(null, event.currentTarget)}
        >
          ＋ Add New Zone
        </button>
      </header>

      <section className="zones-stat-grid" aria-label="Zone summary">
        <Stat
          label="Active zones"
          value={`${stats.active} ${stats.active === 1 ? 'Zone' : 'Zones'}`}
          note={`of ${stats.total} zones`}
          icon="◉"
        />

        <Stat
          label="Standard rates"
          value="Managed by pricing rules"
          note="Base + additional per kg"
          icon="¤"
        />

        <Stat
          label="Express rates"
          value="Managed by pricing rules"
          note="Base + additional per kg"
          icon="↗"
        />

        <Stat
          label="Inactive zones"
          value={String(stats.inactive)}
          note="Not used for new parcels"
          icon="⊘"
        />
      </section>

      <section className="zones-toolbar" aria-label="Filter zones">
        <label className="zones-search">
          <span aria-hidden="true">⌕</span>
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search zone name…"
            aria-label="Search zone name"
          />
        </label>

        <label className="zones-filter">
          <span>Status</span>
          <select
            value={status}
            onChange={(event) =>
              setStatus(event.target.value as ZoneStatusFilter)
            }
          >
            <option value="all">All statuses</option>
            <option value="active">Active only</option>
            <option value="inactive">Inactive only</option>
          </select>
        </label>
      </section>

      {query.isLoading ? (
        <section className="zones-table-card" aria-label="Loading zones">
          <div className="zones-table-scroll">
            <table className="zones-table">
              <thead>
                <tr>
                  {[
                    'Zone',
                    'Standard base',
                    'Standard / kg',
                    'Express base',
                    'Express / kg',
                    'Status',
                    'Actions',
                  ].map((label) => (
                    <th scope="col" key={label}>
                      {label}
                    </th>
                  ))}
                </tr>
              </thead>

              <tbody>
                {Array.from({ length: 4 }, (_, index) => (
                  <tr className="zones-skeleton-row" key={index}>
                    <td colSpan={7}>
                      <i />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : query.isError ? (
        <section
          className="zones-table-card zones-state"
          role="alert"
        >
          <strong>
            {unauthorized
              ? 'You need a dispatcher or admin account to manage zones.'
              : 'We could not load delivery zones.'}
          </strong>

          <button
            className="zones-button zones-button-muted"
            type="button"
            onClick={() => {
              if (unauthorized) {
                authService.clearAccessToken()
                window.location.assign('/admin')
              } else {
                void refresh()
              }
            }}
          >
            {unauthorized ? 'Sign in again' : 'Try again'}
          </button>
        </section>
      ) : allZones.length === 0 ? (
        <section className="zones-table-card zones-empty">
          <span aria-hidden="true">⌖</span>
          <h2>No delivery zones yet</h2>
          <p>Create the first delivery zone to get started.</p>

          <button
            type="button"
            className="zones-button zones-button-primary"
            onClick={(event) => openForm(null, event.currentTarget)}
          >
            ＋ Add New Zone
          </button>
        </section>
      ) : visibleZones.length === 0 ? (
        <section className="zones-table-card zones-empty">
          <h2>No zones match your filters</h2>

          <button
            type="button"
            className="zones-button zones-button-muted"
            onClick={() => {
              setSearch('')
              setStatus('all')
            }}
          >
            Clear filters
          </button>
        </section>
      ) : (
        <section className="zones-table-card">
          <div className="zones-table-scroll">
            <table className="zones-table">
              <thead>
                <tr>
                  <th scope="col">Zone</th>
                  <th scope="col">Standard base</th>
                  <th scope="col">Standard / kg</th>
                  <th scope="col">Express base</th>
                  <th scope="col">Express / kg</th>
                  <th scope="col">Status</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>

              <tbody>
                {visibleZones.map((zone) => {
                  const rules = pricingByZone.get(zone.id) ?? []

                  const standardRule = rules.find(
                    (rule) => rule.serviceType === 'Standard',
                  )

                  const expressRule = rules.find(
                    (rule) => rule.serviceType === 'Express',
                  )

                  const zoneIndex = allZones.findIndex(
                    (item) => item.id === zone.id,
                  )

                  const pricingQuery = pricingQueries[zoneIndex]
                  const loadingPricing = pricingQuery?.isLoading ?? false
                  const pricingError = pricingQuery?.isError ?? false

                  return (
                    <tr key={zone.id}>
                      <td>
                        <span className="zones-identity">
                          <i>{zoneInitials(zone.zoneName)}</i>

                          <span>
                            <strong>{zone.zoneName}</strong>
                            <small>
                              {zone.description || 'Delivery zone'}
                            </small>
                          </span>
                        </span>
                      </td>

                      <td>
                        {loadingPricing
                          ? 'Loading...'
                          : standardRule
                            ? `Rs. ${standardRule.baseRate.toLocaleString()}`
                            : pricingError
                              ? 'Unavailable'
                              : '—'}
                      </td>

                      <td>
                        {loadingPricing
                          ? 'Loading...'
                          : standardRule
                            ? `Rs. ${standardRule.additionalKgRate.toLocaleString()}`
                            : pricingError
                              ? 'Unavailable'
                              : '—'}
                      </td>

                      <td>
                        {loadingPricing
                          ? 'Loading...'
                          : expressRule
                            ? `Rs. ${expressRule.baseRate.toLocaleString()}`
                            : pricingError
                              ? 'Unavailable'
                              : '—'}
                      </td>

                      <td>
                        {loadingPricing
                          ? 'Loading...'
                          : expressRule
                            ? `Rs. ${expressRule.additionalKgRate.toLocaleString()}`
                            : pricingError
                              ? 'Unavailable'
                              : '—'}
                      </td>

                      <td>
                        <span
                          className={`zones-status ${zone.isActive ? 'is-active' : 'is-inactive'
                            }`}
                        >
                          {zone.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>

                      <td className="zones-actions">
                        <button
                          type="button"
                          className="zones-link-button"
                          onClick={(event) =>
                            openForm(zone, event.currentTarget)
                          }
                        >
                          Edit
                        </button>

                        <button
                          type="button"
                          className="zones-switch"
                          role="switch"
                          aria-checked={zone.isActive}
                          aria-label={`Zone active, ${zone.zoneName}`}
                          aria-busy={
                            toggle.isPending &&
                            toggle.variables?.zone.id === zone.id
                          }
                          disabled={toggle.isPending}
                          onClick={(event) => {
                            setOpener(event.currentTarget)

                            if (zone.isActive) {
                              setPendingDeactivate(zone)
                            } else {
                              toggle.mutate({
                                zone,
                                isActive: true,
                              })
                            }
                          }}
                        >
                          <i />
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <footer className="zones-table-foot">
            Showing {visibleZones.length} of {allZones.length} zones
          </footer>
        </section>
      )}

      {dialogZone !== undefined && (
        <ZoneFormDialog
          zone={dialogZone}
          opener={opener}
          onClose={() => setDialogZone(undefined)}
          onSaved={(message) => {
            setDialogZone(undefined)
            showToast(message)
          }}
          onError={(message) => showToast(message, true)}
        />
      )}

      {pendingDeactivate && (
        <DeactivateDialog
          zone={pendingDeactivate}
          opener={opener}
          busy={toggle.isPending}
          close={() => setPendingDeactivate(null)}
          confirm={() =>
            toggle.mutate(
              {
                zone: pendingDeactivate,
                isActive: false,
              },
              {
                onSettled: () => setPendingDeactivate(null),
              },
            )
          }
        />
      )}

      {toast && (
        <div
          className={`zones-toast${toast.error ? ' is-error' : ''}`}
          role={toast.error ? 'alert' : 'status'}
        >
          <span>{toast.message}</span>

          <button
            type="button"
            aria-label="Dismiss notification"
            onClick={() => setToast(null)}
          >
            ×
          </button>
        </div>
      )}
    </main>
  )
}

function Stat({
  label,
  value,
  note,
  icon,
}: {
  label: string
  value: string
  note: string
  icon: string
}) {
  return (
    <article className="zones-stat">
      <div className="zones-stat-top">
        <span>{label}</span>
        <i aria-hidden="true">{icon}</i>
      </div>

      <strong>{value}</strong>
      <small>{note}</small>
    </article>
  )
}
