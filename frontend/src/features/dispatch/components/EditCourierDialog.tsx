import axios from 'axios'
import { useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { COURIER_LICENSE_PLATE_MAX_LENGTH, USE_MOCKS } from '../../../lib/constants'
import { authService } from '../../../services/auth.service'
import { useUpdateCourier } from '../hooks/useCourierMutations'
import { useAccessibleDialog } from '../hooks/useAccessibleDialog'
import type { CourierDto } from '../types/dispatch.types'
import { buildCourierPatch, courierInitials, hasCourierChanges, isLicensePlateValid, mapCourierMutationError, normalizeLicensePlate, vehicleOptions, type CourierFormValues } from '../utils/courierWorkload.utils'
import { vehicleGlyph } from '../utils/dispatchBoard.utils'

function formValues(courier: CourierDto): CourierFormValues {
  return { zoneId: courier.zoneId ?? '', isAvailable: courier.isAvailable, vehicleType: courier.vehicleType ?? '', licensePlate: courier.licensePlate ?? '' }
}

export function EditCourierDialog({ courier: initialCourier, opener, onClose, onUpdated, onRefresh }: {
  courier: CourierDto
  opener: HTMLElement | null
  onClose: () => void
  onUpdated: (courier: CourierDto) => void
  onRefresh: () => Promise<CourierDto | null>
}) {
  const [courier, setCourier] = useState(initialCourier)
  const [values, setValues] = useState(() => formValues(initialCourier))
  const [error, setError] = useState<'conflict' | 'not-found' | 'generic' | null>(null)
  const [plateTouched, setPlateTouched] = useState(false)
  const dialogRef = useRef<HTMLElement>(null)
  const firstInputRef = useRef<HTMLSelectElement>(null)
  const mutation = useUpdateCourier()
  const plateValid = isLicensePlateValid(values.licensePlate)
  const changed = useMemo(() => hasCourierChanges(courier, values), [courier, values])
  const handleKeyDown = useAccessibleDialog(dialogRef, firstInputRef, opener, mutation.isPending, onClose)

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!changed || !plateValid || mutation.isPending) return
    setError(null)
    try {
      const updated = await mutation.mutateAsync({ courierId: courier.courierId, request: buildCourierPatch(courier, values) })
      onUpdated(updated)
      onClose()
    } catch (cause) {
      const status = axios.isAxiosError(cause) ? cause.response?.status : undefined
      const kind = mapCourierMutationError(status, 'update')
      if (kind === 'unauthorized') {
        authService.clearAccessToken()
        window.location.assign('/admin')
      } else setError(kind === 'conflict' || kind === 'not-found' ? kind : 'generic')
    }
  }

  async function refreshAfterError() {
    const latest = await onRefresh()
    if (!latest) {
      onClose()
      return
    }
    setCourier(latest)
    setValues(formValues(latest))
    setError(null)
  }

  function setField<K extends keyof CourierFormValues>(name: K, value: CourierFormValues[K]) {
    setValues((current) => ({ ...current, [name]: value }))
  }

  const content = <div className="courier-dialog-overlay">
    <section className="courier-dialog" role="dialog" aria-modal="true" aria-labelledby="edit-courier-title" ref={dialogRef} onKeyDown={handleKeyDown}>
      <header className="courier-dialog-header">
        <span className="courier-dialog-icon is-orange" aria-hidden="true">▤</span>
        <span><strong id="edit-courier-title">Edit courier</strong><small>Update the zone, vehicle or availability</small></span>
        <span className={`courier-availability-badge${values.isAvailable ? ' is-available' : ''}`}>{values.isAvailable ? 'Available' : 'Unavailable'}</span>
        <button type="button" className="dispatch-icon-button" aria-label="Close edit courier dialog" disabled={mutation.isPending} onClick={onClose}>×</button>
      </header>
      {USE_MOCKS && import.meta.env.DEV && <p className="dispatch-sample-tag courier-dialog-sample">Sample data</p>}
      <form onSubmit={(event) => void save(event)}>
        <div className="courier-dialog-body">
          <section className="courier-identity-section" aria-label="Courier identity">
            <span className="courier-section-label">Courier identity</span>
            <div className="courier-identity-card"><span className="courier-avatar">{courierInitials(courier.name)}</span><span className="courier-identity-copy"><strong title={courier.name}>{courier.name}</strong><small>ID #{courier.courierId.slice(0, 8)}</small></span><span className="courier-readonly">♙ Read-only</span></div>
          </section>
          <label className="courier-form-field"><span>Vehicle type</span><span className="courier-select-icon"><select ref={firstInputRef} value={values.vehicleType} disabled={mutation.isPending} onChange={(event) => setField('vehicleType', event.target.value)}><option value="">Not set</option>{vehicleOptions(courier.vehicleType ?? '').map((option) => <option key={option}>{option}</option>)}</select><b aria-hidden="true">{vehicleGlyph(values.vehicleType)}</b></span></label>
          <label className="courier-form-field"><span>License plate</span><span className="courier-license-control"><input value={values.licensePlate} maxLength={COURIER_LICENSE_PLATE_MAX_LENGTH} placeholder="e.g. WP CAD-1234" disabled={mutation.isPending} aria-invalid={plateTouched && !plateValid || undefined} aria-describedby={plateTouched && !plateValid ? 'edit-plate-error' : undefined} onChange={(event) => { setField('licensePlate', event.target.value); setPlateTouched(true) }} onBlur={() => setField('licensePlate', normalizeLicensePlate(values.licensePlate))} /><b aria-hidden="true">LK</b></span>{plateTouched && !plateValid && <small id="edit-plate-error" className="courier-field-error">Use up to 20 letters, numbers, spaces or hyphens.</small>}</label>
          <section className="courier-availability-card">
            <span className={`courier-status-icon${values.isAvailable ? ' is-available' : ''}`} aria-hidden="true">✓</span>
            <span><strong>Available for dispatch <em className={`courier-availability-badge${values.isAvailable ? ' is-available' : ''}`}>{values.isAvailable ? 'Available' : 'Unavailable'}</em></strong><small>Can be assigned new parcels</small></span>
            <button type="button" role="switch" aria-checked={values.isAvailable} aria-label={`Available for dispatch, ${courier.name}`} title="Unavailable couriers cannot receive new parcels" disabled={mutation.isPending} className={`courier-toggle${values.isAvailable ? ' is-on' : ''}`} onClick={() => setField('isAvailable', !values.isAvailable)}><i /></button>
          </section>
          <div className="courier-active-load-tile"><span>▤</span><strong>Active deliveries: {courier.activeDeliveryCount}</strong></div>
          {error && <div className="courier-form-alert" role="alert">{error === 'conflict' ? 'This courier was just updated by someone else. Refresh to see the latest details.' : error === 'not-found' ? 'This courier no longer exists.' : 'The courier could not be updated. Try again.'}<button type="button" onClick={() => error === 'generic' ? setError(null) : void refreshAfterError()}>{error === 'generic' ? 'Try again' : 'Refresh'}</button></div>}
        </div>
        <footer className="courier-dialog-footer"><button type="button" className="dispatch-secondary-button" disabled={mutation.isPending} onClick={onClose}>Cancel</button><button type="submit" className="dispatch-primary-button courier-dialog-submit" disabled={!changed || !plateValid || mutation.isPending}>{mutation.isPending ? <><span className="dispatch-spinner" aria-hidden="true" />Saving…</> : '▣ Save changes'}</button></footer>
      </form>
    </section>
  </div>
  return createPortal(content, document.body)
}
