import axios from 'axios'
import { useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { COURIER_LICENSE_PLATE_MAX_LENGTH, COURIER_VEHICLE_TYPES, USE_MOCKS } from '../../../lib/constants'
import { authService } from '../../../services/auth.service'
import type { UserDto } from '../../identity'
import { useRegisterCourier } from '../hooks/useCourierMutations'
import { useAccessibleDialog } from '../hooks/useAccessibleDialog'
import type { CourierDto } from '../types/dispatch.types'
import { isLicensePlateValid, isDuplicateCourier, mapCourierMutationError, normalizeLicensePlate, registerCourierPayload } from '../utils/courierWorkload.utils'
import { CourierUserCombobox } from './CourierUserCombobox'

type RegisterError = 'ineligible' | 'generic'

export function RegisterCourierDialog({ couriers, opener, onClose, onRegistered, onEditExisting }: {
  couriers: CourierDto[]
  opener: HTMLElement | null
  onClose: () => void
  onRegistered: (courier: CourierDto) => void
  onEditExisting: (courier: CourierDto, opener: HTMLElement | null) => void
}) {
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null)
  const [vehicleType, setVehicleType] = useState('')
  const [licensePlate, setLicensePlate] = useState('')
  const [error, setError] = useState<RegisterError | null>(null)
  const [duplicateVisible, setDuplicateVisible] = useState(false)
  const [serverDuplicateUserId, setServerDuplicateUserId] = useState<string | null>(null)
  const [plateTouched, setPlateTouched] = useState(false)
  const dialogRef = useRef<HTMLElement>(null)
  const userRef = useRef<HTMLInputElement>(null)
  const mutation = useRegisterCourier()
  const duplicateCourier = selectedUser ? couriers.find((courier) => courier.courierId === selectedUser.id) : undefined
  const selectedIsDuplicate = Boolean(selectedUser && (isDuplicateCourier(selectedUser.id, couriers) || serverDuplicateUserId === selectedUser.id))
  const plateValid = isLicensePlateValid(licensePlate)
  const handleKeyDown = useAccessibleDialog(dialogRef, userRef, opener, mutation.isPending, onClose)

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedUser || selectedIsDuplicate || !plateValid || mutation.isPending) return
    setError(null)
    setDuplicateVisible(false)
    try {
      const courier = await mutation.mutateAsync(registerCourierPayload(selectedUser.id, {
        zoneId: '', isAvailable: true, vehicleType, licensePlate,
      }))
      onRegistered(courier)
      onClose()
    } catch (cause) {
      const status = axios.isAxiosError(cause) ? cause.response?.status : undefined
      const kind = mapCourierMutationError(status, 'register')
      if (kind === 'duplicate') {
        setServerDuplicateUserId(selectedUser.id)
        setDuplicateVisible(true)
      }
      else if (kind === 'ineligible') setError('ineligible')
      else if (kind === 'unauthorized') {
        authService.clearAccessToken()
        window.location.assign('/admin')
      } else setError('generic')
    }
  }

  const content = <div className="admin-shell courier-dialog-overlay">
    <section className="courier-dialog" role="dialog" aria-modal="true" aria-labelledby="register-courier-title" ref={dialogRef} onKeyDown={handleKeyDown}>
      <header className="courier-dialog-header">
        <span className="courier-dialog-icon is-blue" aria-hidden="true">♙+</span>
        <span><strong id="register-courier-title">Register courier</strong><small>Link an existing courier account to a dispatch zone and vehicle</small></span>
        <button type="button" className="dispatch-icon-button" aria-label="Close register courier dialog" disabled={mutation.isPending} onClick={onClose}>×</button>
      </header>
      {USE_MOCKS && import.meta.env.DEV && <p className="dispatch-sample-tag courier-dialog-sample">Sample data</p>}
      <form onSubmit={(event) => void submit(event)}>
        <div className="courier-dialog-body">
          <CourierUserCombobox
            couriers={couriers}
            selected={selectedUser}
            onSelect={(user) => { setSelectedUser(user); setServerDuplicateUserId(null); setDuplicateVisible(Boolean(user && isDuplicateCourier(user.id, couriers))); setError(null) }}
            disabled={mutation.isPending}
            duplicate={selectedIsDuplicate || duplicateVisible}
            describedBy={duplicateVisible ? 'courier-duplicate-banner' : undefined}
            inputRef={userRef}
          />
          {duplicateVisible ? <div className="courier-duplicate-banner" id="courier-duplicate-banner" role="alert"><span className="courier-error-icon" aria-hidden="true">!</span>
            <div className="courier-error-heading"><strong>Duplicate Courier Conflict</strong><span>Already registered</span><button type="button" aria-label="Dismiss duplicate message" onClick={() => setDuplicateVisible(false)}>×</button></div>
            <p>This user already has a courier profile. Please select another user account or {duplicateCourier ? <button type="button" className="courier-inline-link" onClick={() => onEditExisting(duplicateCourier, opener)}>edit the existing courier</button> : 'edit the existing courier'}.</p>
          </div> : null}
          {!duplicateVisible && error === 'ineligible' && <div className="courier-form-alert" role="alert">This account is not an active courier. Choose another account.</div>}
          {!duplicateVisible && error === 'generic' && <div className="courier-form-alert" role="alert">The courier could not be registered. Try again.<button type="button" onClick={() => setError(null)}>Try again</button></div>}
          <label className="courier-form-field"><span>Vehicle type</span><select value={vehicleType} disabled={mutation.isPending} onChange={(event) => setVehicleType(event.target.value)}><option value="">Select vehicle type</option>{COURIER_VEHICLE_TYPES.map((option) => <option key={option}>{option}</option>)}</select></label>
          <label className="courier-form-field"><span>License plate</span><span className="courier-license-control"><input value={licensePlate} maxLength={COURIER_LICENSE_PLATE_MAX_LENGTH} placeholder="e.g. WP CAD-1234" disabled={mutation.isPending} aria-invalid={plateTouched && !plateValid || undefined} aria-describedby={plateTouched && !plateValid ? 'register-plate-error' : undefined} onChange={(event) => { setLicensePlate(event.target.value); setPlateTouched(true) }} onBlur={() => setLicensePlate(normalizeLicensePlate(licensePlate))} /><b aria-hidden="true">LK</b></span>{plateTouched && !plateValid && <small id="register-plate-error" className="courier-field-error">Use up to 20 letters, numbers, spaces or hyphens.</small>}</label>
          <p className="courier-info-note"><span aria-hidden="true">ⓘ</span>Registered couriers will immediately appear on the active workload board with zero assigned orders.</p>
        </div>
        <footer className="courier-dialog-footer">
          <button type="button" className="dispatch-secondary-button" disabled={mutation.isPending} onClick={onClose}>Cancel</button>
          <button type="submit" className="dispatch-primary-button courier-dialog-submit" disabled={!selectedUser || selectedIsDuplicate || !plateValid || mutation.isPending}>{mutation.isPending ? <><span className="dispatch-spinner" aria-hidden="true" />Registering…</> : '♙ Register courier'}</button>
        </footer>
      </form>
    </section>
  </div>
  return createPortal(content, document.body)
}
