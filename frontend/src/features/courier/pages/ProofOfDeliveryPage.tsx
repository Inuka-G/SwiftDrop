import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { ContactActions } from '../components/ContactActions'
import { Icon } from '../components/Icon'
import { PhotoPicker } from '../components/PhotoPicker'
import { TopBar } from '../components/TopBar'
import { FAILURE_REASON_LABELS, formatCurrency, isClosed } from '../components/statusMeta'
import { useConfirmDelivery, useCourierDelivery, useReportFailure } from '../hooks/useCourierDeliveries'
import type { CourierDelivery, FailureReason } from '../types/courier.types'

const NOTE_LIMIT = 500

/** Closes a delivery: "Successful POD" (name and/or photo) or "Failed Delivery" (reason, optional photo). */
export function ProofOfDeliveryPage() {
  const { id = '' } = useParams()
  const [params, setParams] = useSearchParams()
  const { data: delivery } = useCourierDelivery(id)
  const detailPath = `/courier/deliveries/${id}`

  if (!delivery) return <p className="empty-state">Loading delivery…</p>
  if (isClosed(delivery.currentStatus)) return <Navigate to={detailPath} replace />

  const canDeliver = delivery.allowedNextStatuses.includes('Delivered')
  const mode = params.get('mode') === 'failed' || !canDeliver ? 'failed' : 'success'

  return (
    <>
      <TopBar title="Proof Of Delivery" backTo={detailPath} />

      <section className="card stop-header stop-header-tinted">
        <div className="stop-card-top">
          <span className="mono tracking">{delivery.trackingNumber}</span>
          <span className="pill pill-warning">● Pending action</span>
        </div>
        <div className="stop-header-main">
          <div>
            <h2 className="stop-name">{delivery.recipientName}</h2>
            <p className="stop-address">{delivery.dropoffAddress}</p>
          </div>
          <ContactActions phone={delivery.recipientPhone} address={delivery.dropoffAddress} />
        </div>
      </section>

      <div className="segmented" role="tablist" aria-label="Delivery outcome">
        <button type="button" role="tab" aria-selected={mode === 'success'} disabled={!canDeliver}
          onClick={() => setParams({}, { replace: true })}>
          <Icon name="checkCircle" size={18} /> Successful POD
        </button>
        <button type="button" role="tab" aria-selected={mode === 'failed'}
          className="segmented-danger" onClick={() => setParams({ mode: 'failed' }, { replace: true })}>
          <Icon name="alert" size={18} /> Failed Delivery
        </button>
      </div>
      {!canDeliver && (
        <p className="muted small center">Successful delivery opens once the parcel is out for delivery.</p>
      )}

      {mode === 'success' ? (
        <SuccessForm key="success" delivery={delivery} detailPath={detailPath} />
      ) : (
        <FailureForm key="failed" delivery={delivery} detailPath={detailPath} />
      )}
    </>
  )
}

function SuccessForm({ delivery, detailPath }: { delivery: CourierDelivery; detailPath: string }) {
  const navigate = useNavigate()
  const confirm = useConfirmDelivery()
  const [recipientName, setRecipientName] = useState(delivery.recipientName)
  const [note, setNote] = useState('')
  const [photo, setPhoto] = useState<File | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!recipientName.trim() && !photo) return setLocalError("Enter who received the parcel or attach a photo.")
    setLocalError(null)
    confirm.mutate(
      { id: delivery.id, input: { recipientName, note, photo } },
      { onSuccess: () => navigate(detailPath, { replace: true }) },
    )
  }

  const error = localError ?? (confirm.isError ? getErrorMessage(confirm.error) : null)

  return (
    <form className="pod-form" onSubmit={handleSubmit}>
      <section className="banner banner-success">
        <Icon name="checkCircle" size={28} />
        <div>
          <h2>Confirm successful handover</h2>
          <p>Closes the delivery and notifies the customer and merchant straight away.</p>
        </div>
      </section>

      {delivery.amountToCollect > 0 && (
        <section className="card cod-card">
          <Icon name="cash" size={24} />
          <div>
            <p className="label">Cash on delivery</p>
            <p className="cod-amount">{formatCurrency(delivery.amountToCollect)}</p>
          </div>
          <span className="pill pill-warning">Collect first</span>
        </section>
      )}

      <section className="card form-card">
        <h2>Authorized recipient</h2>
        <p className="muted">Who took the parcel? Leave empty only if you attach a photo.</p>
        <input
          className="input"
          value={recipientName}
          maxLength={150}
          onChange={(e) => setRecipientName(e.target.value)}
          placeholder="Recipient's full name"
          aria-label="Received by"
        />
      </section>

      <PhotoPicker title="Handover photo proof" hint="Parcel at the door or with the recipient." file={photo} onChange={setPhoto} />
      <NotesCard value={note} onChange={setNote} label="Delivery notes" required={false}
        placeholder="e.g. Handed to security at the front desk" />

      <Timestamp />
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="btn btn-success btn-block btn-lg" type="submit" disabled={confirm.isPending}>
        <Icon name="checkCircle" /> {confirm.isPending ? 'Saving…' : 'Confirm Delivery & Notify Customer'}
      </button>
      <Link className="btn btn-soft btn-block" to={detailPath}>
        <Icon name="back" /> Cancel & Return to Stop
      </Link>
    </form>
  )
}

function FailureForm({ delivery, detailPath }: { delivery: CourierDelivery; detailPath: string }) {
  const navigate = useNavigate()
  const report = useReportFailure()
  const [reason, setReason] = useState<FailureReason | ''>('')
  const [note, setNote] = useState('')
  const [photo, setPhoto] = useState<File | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!reason) return setLocalError('Choose the reason for non-delivery.')
    if (reason === 'Other' && !note.trim()) return setLocalError('Describe what happened in the notes.')
    setLocalError(null)
    report.mutate(
      { id: delivery.id, input: { reason, note, photo } },
      { onSuccess: () => navigate(detailPath, { replace: true }) },
    )
  }

  const error = localError ?? (report.isError ? getErrorMessage(report.error) : null)

  return (
    <form className="pod-form" onSubmit={handleSubmit}>
      <section className="banner banner-danger">
        <span className="banner-icon"><Icon name="alert" size={24} /></span>
        <div>
          <h2>Mark delivery as failed</h2>
          <p>Closes this delivery as Failed and notifies dispatch, the customer and the merchant.</p>
        </div>
      </section>

      <Timestamp />

      <section className="card form-card">
        <h2>Reason for non-delivery <span className="required">*</span></h2>
        <p className="muted">Select the main reason the handover could not happen.</p>
        <select className="input" value={reason} onChange={(e) => setReason(e.target.value as FailureReason)}
          aria-label="Reason for non-delivery">
          <option value="" disabled>Select a reason</option>
          {Object.entries(FAILURE_REASON_LABELS).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
      </section>

      <PhotoPicker title="Attempt proof photo" hint="Gate, doorway or locked premises (optional)." file={photo} onChange={setPhoto} />
      <NotesCard value={note} onChange={setNote} label="Additional notes / driver comments" required={reason === 'Other'}
        placeholder="e.g. Called twice, no answer. Main gate locked." />

      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="btn btn-danger btn-block btn-lg" type="submit" disabled={report.isPending}>
        <Icon name="alert" /> {report.isPending ? 'Saving…' : 'Confirm Failed Delivery & Notify Dispatch'}
      </button>
      <Link className="btn btn-soft btn-block" to={detailPath}>
        <Icon name="back" /> Cancel & Return to Stop
      </Link>
    </form>
  )
}

interface NotesProps {
  value: string
  onChange: (value: string) => void
  label: string
  required: boolean
  placeholder: string
}

function NotesCard({ value, onChange, label, required, placeholder }: NotesProps) {
  return (
    <section className="card form-card">
      <h2>
        {label} {required && <span className="required">*</span>}
      </h2>
      <textarea className="input" rows={4} maxLength={NOTE_LIMIT} value={value} required={required}
        onChange={(e) => onChange(e.target.value)} placeholder={placeholder} aria-label={label} />
      <p className="muted small mono align-end">
        {value.length} / {NOTE_LIMIT} chars
      </p>
    </section>
  )
}

/** The time this report is being made; the server records its own timestamp on save. */
function Timestamp() {
  const [now] = useState(() => new Date())
  return (
    <section className="card timestamp-card">
      <Icon name="clock" />
      <div>
        <p className="label">Timestamp</p>
        <p className="mono">{new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(now)}</p>
      </div>
    </section>
  )
}
