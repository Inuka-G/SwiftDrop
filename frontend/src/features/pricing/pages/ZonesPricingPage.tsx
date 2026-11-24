
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
type RateFormState = {
  standardBaseRate: string
  standardAdditionalKgRate: string
  expressBaseRate: string
  expressAdditionalKgRate: string
}
type SaveFailure = { operation: 'zone' | 'activation' | 'standard' | 'express'; action?: 'create' | 'update'; message: string }
type SaveOutcome = { saved: string[]; failed: SaveFailure[]; created?: boolean; zoneBaseline?: ZoneDto }

function pricingUpdateErrorMessage(serviceType: 'Standard' | 'Express', status?: number): string {
  if (status === 400) return `${serviceType} rates were rejected. Check that both values are valid and non-negative.`
  if (status === 404) return `The ${serviceType} pricing rule was not found. Reload pricing rules before retrying.`
  if (status === 409) return `The ${serviceType} pricing rule conflicts with a newer change. Reload pricing rules and try again.`
  if (status === 500) return `The pricing service encountered a server error while updating the ${serviceType} rule. Try again.`
  if (status === 403) return `You don't have permission to update the ${serviceType} pricing rule.`
  if (status === 401) return `Your session expired while updating the ${serviceType} pricing rule.`
  if (status === undefined) return `Could not reach the pricing service while updating the ${serviceType} rule. Check your connection and try again.`
  return `The ${serviceType} pricing rule could not be updated (HTTP ${status}). Try again.`
}

function pricingCreateErrorMessage(serviceType: 'Standard' | 'Express', status?: number): string {
  if (status === 400) return `${serviceType} rates were rejected. Check the rate values and service type.`
  if (status === 404) return `The zone for the ${serviceType} rule was not found.`
  if (status === 409) return `A ${serviceType} pricing rule already exists. The page will refresh its rules; retry to update it if needed.`
  if (status === 500) return `The pricing service encountered a server error while creating the ${serviceType} rule. Refresh the rules before retrying.`
  if (status === 403) return `You don't have permission to create the ${serviceType} pricing rule.`
  if (status === 401) return `Your session expired while creating the ${serviceType} pricing rule.`
  if (status === undefined) return `Could not reach the pricing service while creating the ${serviceType} rule. Refresh the rules before retrying.`
  return `The ${serviceType} pricing rule could not be created (HTTP ${status}). Refresh the rules before retrying.`
}

function isValidRatePair(base: string, additional: string): boolean {
  const isValidRate = (text: string) => {
    if (!text.trim()) return false
    const value = Number(text)
    return Number.isFinite(value) && value >= 0 && value <= 99_999_999.99 &&
      Math.abs(value * 100 - Math.round(value * 100)) < 1e-8
  }
  return isValidRate(base) && isValidRate(additional)
}

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
        node instanceof HTMLElement &&
        node !== overlay &&
        !node.classList.contains('zones-overlay'),
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
  const [createdZone, setCreatedZone] = useState<ZoneDto | null>(null)
  const currentZone = zone ?? createdZone
  const dialogOpenerRef = useMemo<RefObject<HTMLElement | null>>(
    () => ({ current: opener }),
    [opener],
  )

  const [form, setForm] = useState<FormState>({
    zoneName: zone?.zoneName ?? '',
    description: zone?.description ?? '',
  })

  const [zoneBaseline, setZoneBaseline] = useState(zone)
  const [active, setActive] = useState(zone?.isActive ?? true)
  const [error, setError] = useState('')
  const [rateError, setRateError] = useState('')
  const [rates, setRates] = useState<RateFormState | null>(zone ? null : {
    standardBaseRate: '',
    standardAdditionalKgRate: '',
    expressBaseRate: '',
    expressAdditionalKgRate: '',
  })
  const [confirmDeactivate, setConfirmDeactivate] = useState(false)
  const activeToggleRef = useRef<HTMLButtonElement>(null)
  const ratesInitializedRef = useRef(false)

  const pricingRulesQuery = useQuery({
    queryKey: ['zone-pricing-rules', currentZone?.id],
    queryFn: () => zonesApi.getPricingRules(currentZone!.id),
    enabled: Boolean(currentZone),
    staleTime: 30_000,
  })

  const standardRule = pricingRulesQuery.data?.find((rule) => rule.serviceType === 'Standard')
  const expressRule = pricingRulesQuery.data?.find((rule) => rule.serviceType === 'Express')

  useEffect(() => {
    if (ratesInitializedRef.current || !currentZone || !pricingRulesQuery.isSuccess) return
    ratesInitializedRef.current = true
    setRates({
      standardBaseRate: standardRule ? String(standardRule.baseRate) : '',
      standardAdditionalKgRate: standardRule ? String(standardRule.additionalKgRate) : '',
      expressBaseRate: expressRule ? String(expressRule.baseRate) : '',
      expressAdditionalKgRate: expressRule ? String(expressRule.additionalKgRate) : '',
    })
  }, [currentZone, pricingRulesQuery.isSuccess, standardRule, expressRule])

  const mutation = useMutation({
    mutationFn: async (value: FormState): Promise<SaveOutcome> => {
      const saved: string[] = []
      const failed: SaveFailure[] = []
      let serverUpdateAttempted = false
      let targetZone = currentZone
      let createdDuringSave = false

      const runOperation = async (
        operation: SaveFailure['operation'],
        label: string,
        update: () => Promise<unknown>,
        createPricingRule = false,
      ): Promise<boolean> => {
        serverUpdateAttempted = true
        try {
          await update()
          saved.push(label)
          return true
        } catch (failure) {
          const status = getHttpStatus(failure)
          if (status === 401) {
            authService.clearAccessToken()
            window.location.assign('/admin')
          }
          const pricingService = operation === 'standard' ? 'Standard'
            : operation === 'express' ? 'Express'
              : null
          failed.push({
            operation,
            ...(pricingService ? { action: createPricingRule ? 'create' as const : 'update' as const } : {}),
            message: pricingService
              ? createPricingRule
                ? pricingCreateErrorMessage(pricingService, status)
                : pricingUpdateErrorMessage(pricingService, status)
              : zoneErrorMessage(status),
          })
          return false
        }
      }

      try {
        if (!targetZone) {
          if (!ratesValid) {
            const message = 'Enter non-negative values for all four rates before creating the zone.'
            failed.push({ operation: 'standard', action: 'create', message }, { operation: 'express', action: 'create', message })
            return { saved, failed }
          }

          serverUpdateAttempted = true
          targetZone = await zonesApi.create({
            zoneName: value.zoneName.trim(),
            description: value.description.trim() || null,
          })
          createdDuringSave = true
          saved.push('Zone')
          setCreatedZone(targetZone)
          setZoneBaseline(targetZone)
          setActive(targetZone.isActive)
          // The entered values are the baseline for rule creation; do not replace them
          // when the newly-created zone's rule query completes after a partial failure.
          ratesInitializedRef.current = true
        }

        const baselineAfter = { ...(zoneBaseline ?? targetZone) }
        const zoneDetailsChanged = value.zoneName.trim() !== baselineAfter.zoneName ||
          (value.description.trim() || '') !== (baselineAfter.description ?? '')
        if (zoneDetailsChanged) {
          const detailsSaved = await runOperation('zone', 'Zone details', () => zonesApi.update(targetZone!.id, {
            zoneName: value.zoneName.trim(),
            description: value.description.trim() || null,
          }))
          if (detailsSaved) {
            baselineAfter.zoneName = value.zoneName.trim()
            baselineAfter.description = value.description.trim() || null
          }
        }

        if (active !== baselineAfter.isActive) {
          const activationSaved = await runOperation('activation', 'Zone status', () => zonesApi.setActive(targetZone!.id, { isActive: active }))
          if (activationSaved) baselineAfter.isActive = active
        }

        const standardPairEntered = Boolean(rates && (rates.standardBaseRate.trim() || rates.standardAdditionalKgRate.trim()))
        const expressPairEntered = Boolean(rates && (rates.expressBaseRate.trim() || rates.expressAdditionalKgRate.trim()))
        const standardRateChanged = Boolean(rates && (standardRule
          ? Number(rates.standardBaseRate) !== standardRule.baseRate || Number(rates.standardAdditionalKgRate) !== standardRule.additionalKgRate
          : standardPairEntered))
        const expressRateChanged = Boolean(rates && (expressRule
          ? Number(rates.expressBaseRate) !== expressRule.baseRate || Number(rates.expressAdditionalKgRate) !== expressRule.additionalKgRate
          : expressPairEntered))
        const hasRateChanges = standardRateChanged || expressRateChanged
        if (!createdDuringSave && hasRateChanges && pricingRulesQuery.isError) {
          const message = 'Pricing rules could not be refreshed. No pricing update was sent; retry loading the rules before editing rates.'
          if (standardRateChanged) failed.push({ operation: 'standard', action: standardRule ? 'update' : 'create', message })
          if (expressRateChanged) failed.push({ operation: 'express', action: expressRule ? 'update' : 'create', message })
        } else if (rates) {
          if (standardRateChanged) {
            if (!isValidRatePair(rates.standardBaseRate, rates.standardAdditionalKgRate)) {
              failed.push({ operation: 'standard', action: standardRule ? 'update' : 'create', message: 'Enter valid non-negative Standard rates with at most two decimal places.' })
            } else if (standardRule) {
              await runOperation('standard', 'Standard pricing', () => zonesApi.updateRate(targetZone!.id, standardRule.id, {
                baseRate: Number(rates.standardBaseRate),
                additionalKgRate: Number(rates.standardAdditionalKgRate),
              }))
            } else {
              await runOperation('standard', 'Standard pricing', () => zonesApi.createRate(targetZone!.id, {
                serviceType: 1,
                baseRate: Number(rates.standardBaseRate),
                additionalKgRate: Number(rates.standardAdditionalKgRate),
                isActive: true,
              }), true)
            }
          }
          if (expressRateChanged) {
            if (!isValidRatePair(rates.expressBaseRate, rates.expressAdditionalKgRate)) {
              failed.push({ operation: 'express', action: expressRule ? 'update' : 'create', message: 'Enter valid non-negative Express rates with at most two decimal places.' })
            } else if (expressRule) {
              await runOperation('express', 'Express pricing', () => zonesApi.updateRate(targetZone!.id, expressRule.id, {
                baseRate: Number(rates.expressBaseRate),
                additionalKgRate: Number(rates.expressAdditionalKgRate),
              }))
            } else {
              await runOperation('express', 'Express pricing', () => zonesApi.createRate(targetZone!.id, {
                serviceType: 2,
                baseRate: Number(rates.expressBaseRate),
                additionalKgRate: Number(rates.expressAdditionalKgRate),
                isActive: true,
              }), true)
            }
          }
        }

        return { saved, failed, zoneBaseline: baselineAfter, created: createdDuringSave }
      } finally {
        if (serverUpdateAttempted) {
          const invalidations = [
            queryClient.invalidateQueries({ queryKey: ZONES_QUERY_KEY }),
          ]
          if (targetZone) invalidations.push(queryClient.invalidateQueries({ queryKey: ['zone-pricing-rules', targetZone.id] }))
          await Promise.all(invalidations)
        }
      }
    },

    onSuccess: (outcome) => {
      setError('')
      setRateError('')
      if (outcome.zoneBaseline) setZoneBaseline(outcome.zoneBaseline)
      if (outcome.failed.length > 0) {
        const messages = outcome.failed.map(({ operation, action, message }) => {
          if (operation === 'standard') return `Standard pricing ${action ?? 'update'} failed: ${message}`
          if (operation === 'express') return `Express pricing ${action ?? 'update'} failed: ${message}`
          if (operation === 'activation') return `Zone status update failed: ${message}`
          return `Zone details update failed: ${message}`
        })
        const summary = [
          outcome.saved.length ? `Saved: ${outcome.saved.join(', ')}.` : '',
          ...messages,
        ].filter(Boolean).join(' ')
        const pricingFailures = outcome.failed.filter(({ operation }) => operation === 'standard' || operation === 'express')
        if (pricingFailures.length) {
          setRateError(pricingFailures.map(({ operation, action, message }) => `${operation === 'standard' ? 'Standard' : 'Express'} pricing ${action ?? 'update'} failed: ${message}`).join(' '))
        }
        const zoneFailures = outcome.failed.filter(({ operation }) => operation === 'zone' || operation === 'activation')
        if (zoneFailures.length) setError(zoneFailures.map(({ message }) => message).join(' '))
        onError(summary)
        return
      }

      const pricingSaved = outcome.saved.some((item) => item === 'Standard pricing' || item === 'Express pricing')
      onSaved(outcome.created
        ? pricingSaved ? 'Zone and pricing rules created' : 'Zone created'
        : pricingSaved ? 'Zone and pricing updated' : 'Zone updated')
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

  const ratesValid =
    rates !== null &&
    isValidRatePair(rates.standardBaseRate, rates.standardAdditionalKgRate) &&
    isValidRatePair(rates.expressBaseRate, rates.expressAdditionalKgRate)
  const standardPairEntered = Boolean(rates && (rates.standardBaseRate.trim() || rates.standardAdditionalKgRate.trim()))
  const expressPairEntered = Boolean(rates && (rates.expressBaseRate.trim() || rates.expressAdditionalKgRate.trim()))
  const standardRateChanged = Boolean(rates && (standardRule
    ? Number(rates.standardBaseRate) !== standardRule.baseRate || Number(rates.standardAdditionalKgRate) !== standardRule.additionalKgRate
    : standardPairEntered))
  const expressRateChanged = Boolean(rates && (expressRule
    ? Number(rates.expressBaseRate) !== expressRule.baseRate || Number(rates.expressAdditionalKgRate) !== expressRule.additionalKgRate
    : expressPairEntered))
  const ratesChanged = standardRateChanged || expressRateChanged

  const changed =
    !currentZone ||
    form.zoneName.trim() !== (zoneBaseline?.zoneName ?? currentZone.zoneName) ||
    (form.description.trim() || '') !== (zoneBaseline?.description ?? '') ||
    active !== (zoneBaseline?.isActive ?? currentZone.isActive) ||
    ratesChanged

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')

    if (!nameValid) {
      setError('Enter a zone name up to 100 characters.')
      return
    }

    if (!currentZone && !ratesValid) {
      setRateError('Enter all four non-negative rates with at most two decimal places before creating the zone.')
      return
    }

    await mutation.mutateAsync(form)
  }

  const cancel = () => {
    if (!mutation.isPending) onClose()
  }

  return (
    <Modal
      title={currentZone ? 'Edit zone' : 'Add New Delivery Zone'}
      subtitle={
        currentZone
          ? "Update the zone's name, rates or status"
          : 'Set the zone name and its shipping rates'
      }
      onClose={cancel}
      closeRef={dialogOpenerRef}
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
            disabled={
              !changed || !nameValid || (!currentZone && !ratesValid) || mutation.isPending
            }
          >
            {mutation.isPending
              ? 'Saving…'
              : currentZone
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

        <section className="zones-rates-placeholder" aria-busy={pricingRulesQuery.isLoading}>
          <div className="zones-rate-title">
            <strong>Rates</strong>
            <span>Standard · Express</span>
          </div>

          {!currentZone ? (
            <p>Set the initial Standard and Express rates for this zone. The zone will be created first, followed by its pricing rules.</p>
          ) : pricingRulesQuery.isLoading ? (
            <p role="status">Loading pricing rules…</p>
          ) : pricingRulesQuery.isError ? (
            <div role="alert">
              <p>Pricing rules could not be loaded. Rate changes are unavailable until the rules refresh succeeds.</p>
              <button type="button" className="zones-button zones-button-muted" onClick={() => void pricingRulesQuery.refetch()}>Try again</button>
            </div>
          ) : !standardRule || !expressRule ? (
            <p role="status">{!standardRule && !expressRule
              ? 'This zone has no pricing rules yet. Enter rates to create Standard and Express rules.'
              : `This zone is missing its ${!standardRule ? 'Standard' : 'Express'} rule. Enter rates to create the missing rule.`}</p>
          ) : null}
          {rates && (!currentZone || pricingRulesQuery.isSuccess) ? (
            <div className="zones-form">
              {([
                ['standardBaseRate', 'Standard base rate'],
                ['standardAdditionalKgRate', 'Standard additional rate per kg'],
                ['expressBaseRate', 'Express base rate'],
                ['expressAdditionalKgRate', 'Express additional rate per kg'],
              ] as const).map(([field, label]) => (
                <label className="zones-field" key={field}>
                  <span>{label}</span>
                  <input
                    type="number"
                    min="0"
                    step="any"
                    inputMode="decimal"
                    required
                    aria-label={label}
                    aria-invalid={Boolean(rateError) || (rates[field].trim() !== '' && (!Number.isFinite(Number(rates[field])) || Number(rates[field]) < 0 || Number(rates[field]) > 99_999_999.99))}
                    value={rates[field]}
                    disabled={mutation.isPending}
                    onChange={(event) => {
                      setRates({ ...rates, [field]: event.target.value })
                      setRateError('')
                    }}
                  />
                </label>
              ))}
              {(rateError || (!ratesValid && Object.values(rates).some((value) => value !== ''))) && (
                <small className="zones-field-error" role="alert">
                  {rateError || 'Each rate must be a non-negative number.'}
                </small>
              )}
            </div>
          ) : currentZone && !pricingRulesQuery.isError && !pricingRulesQuery.isLoading ? (
            <p role="status">Preparing pricing rules…</p>
          ) : null}
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
              if (currentZone?.isActive && active) {
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

      {confirmDeactivate && currentZone && (
        <Modal
          title="Deactivate zone"
          subtitle={`${currentZone.zoneName} will no longer be used for new parcels.`}
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
  const dialogOpenerRef = useMemo<RefObject<HTMLElement | null>>(
    () => ({ current: opener }),
    [opener],
  )

  return (
    <Modal
      title="Deactivate zone"
      subtitle={`${zone.zoneName} will no longer be used for new parcels.`}
      onClose={close}
      closeRef={dialogOpenerRef}
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
