import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { ContactActions } from '../components/ContactActions'
import { DeliveryOutcome } from '../components/DeliveryOutcome'
import { Icon } from '../components/Icon'
import { JourneyMilestones } from '../components/JourneyMilestones'
import { StatusBadge } from '../components/StatusBadge'
import { TopBar } from '../components/TopBar'
import { ADVANCE_ACTIONS, currentStop, formatCurrency, isClosed } from '../components/statusMeta'
import { useCourierDelivery, useParcelHistory, useUpdateStatus } from '../hooks/useCourierDeliveries'
import type { CourierDelivery } from '../types/courier.types'

export function DeliveryDetailPage() {
  const { id = '' } = useParams()
  const { data: delivery, isError, error } = useCourierDelivery(id)
  const history = useParcelHistory(id)

  if (isError) {
    return (
      <>
        <TopBar title="Delivery" backTo="/courier" />
        <section className="card" role="alert">
          <p className="form-error">{getErrorMessage(error)}</p>
        </section>
      </>
    )
  }
  if (!delivery) return <p className="empty-state">Loading delivery…</p>

  const stop = currentStop(delivery)
  const closed = isClosed(delivery.currentStatus)

  return (
    <>
      <TopBar title="Delivery" backTo={closed ? '/courier/completed' : '/courier'} />

      <section className="card stop-header">
        <div className="stop-card-top">
          <span className="mono tracking">{delivery.trackingNumber}</span>
          <StatusBadge status={delivery.currentStatus} />
        </div>
        <p className="stop-kind">{stop.kind}</p>
        <div className="stop-header-main">
          <div>
            <h2 className="stop-name">{stop.name}</h2>
            <p className="stop-address">{stop.address}</p>
          </div>
          {stop.phone && <ContactActions phone={stop.phone} address={stop.address} />}
        </div>
      </section>

      {!closed && <NextAction delivery={delivery} />}
      <DeliveryOutcome delivery={delivery} />

      <section className="card">
        <h2 className="section-title">Journey milestones</h2>
        {history.isLoading ? (
          <p className="muted">Loading history…</p>
        ) : (
          <JourneyMilestones status={delivery.currentStatus} history={history.data ?? []} />
        )}
      </section>

      <section className="card">
        <h2 className="section-title">Parcel & route</h2>
        <dl className="fact-grid">
          <div>
            <dt>Pickup</dt>
            <dd>{delivery.pickupAddress}</dd>
            <dd className="muted">
              {delivery.pickupContactName} · {delivery.pickupContactPhone}
            </dd>
          </div>
          <div>
            <dt>Drop-off</dt>
            <dd>{delivery.dropoffAddress}</dd>
            <dd className="muted">
              {delivery.recipientName} · {delivery.recipientPhone}
            </dd>
          </div>
          <div>
            <dt>Weight / size</dt>
            <dd>
              {delivery.weightKg} kg · {delivery.sizeCategory}
            </dd>
          </div>
          <div>
            <dt>Payment</dt>
            <dd>{delivery.amountToCollect > 0 ? `Collect ${formatCurrency(delivery.amountToCollect)}` : 'Prepaid'}</dd>
          </div>
        </dl>
      </section>
    </>
  )
}

/** The moves the server allows next: one forward step, or closing the delivery. */
function NextAction({ delivery }: { delivery: CourierDelivery }) {
  const [note, setNote] = useState('')
  const update = useUpdateStatus()
  const next = delivery.allowedNextStatuses
  const advanceTo = next.find((s) => s in ADVANCE_ACTIONS)
  const proofPath = `/courier/deliveries/${delivery.id}/proof`

  function advance() {
    if (!advanceTo) return
    update.mutate({ id: delivery.id, status: advanceTo, note: note.trim() }, { onSuccess: () => setNote('') })
  }

  return (
    <section className="card next-action">
      <h2 className="section-title">Next step</h2>
      {advanceTo && (
        <>
          <label className="field">
            <span>Note (optional)</span>
            <input
              value={note}
              maxLength={500}
              onChange={(e) => setNote(e.target.value)}
              placeholder="e.g. Collected from front desk"
            />
          </label>
          <button className="btn btn-primary btn-block btn-lg" type="button" onClick={advance} disabled={update.isPending}>
            <Icon name="check" /> {update.isPending ? 'Updating…' : ADVANCE_ACTIONS[advanceTo]}
          </button>
        </>
      )}
      {next.includes('Delivered') && (
        <Link className="btn btn-success btn-block btn-lg" to={proofPath}>
          <Icon name="checkCircle" /> Complete delivery
        </Link>
      )}
      {update.isError && (
        <p className="form-error" role="alert">
          {getErrorMessage(update.error)}
        </p>
      )}
      {next.includes('Failed') && (
        <Link className="btn btn-soft-danger btn-block" to={`${proofPath}?mode=failed`}>
          <Icon name="alert" /> Report failed delivery
        </Link>
      )}
    </section>
  )
}
