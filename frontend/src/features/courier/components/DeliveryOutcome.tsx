import { formatDateTime } from '../../../lib/utils'
import { useDeliveryProof, useFailureReport, usePhotoUrl } from '../hooks/useCourierDeliveries'
import type { CourierDelivery } from '../types/courier.types'
import { Icon } from './Icon'
import { FAILURE_REASON_LABELS } from './statusMeta'

/** What happened at the door: the proof of a delivered parcel, or the report for a failed one. */
export function DeliveryOutcome({ delivery }: { delivery: CourierDelivery }) {
  const delivered = delivery.currentStatus === 'Delivered'
  const failed = delivery.currentStatus === 'Failed'
  const proof = useDeliveryProof(delivery.id, delivered)
  const failure = useFailureReport(delivery.id, failed)
  const proofPhoto = usePhotoUrl('proof', delivery.id, delivered && !!proof.data?.hasPhoto)
  const failurePhoto = usePhotoUrl('failure', delivery.id, failed && !!failure.data?.hasPhoto)

  if (delivered && proof.data) {
    return (
      <section className="card outcome outcome-success">
        <h2>
          <Icon name="checkCircle" /> Proof of delivery
        </h2>
        <dl className="fact-grid">
          <div>
            <dt>Received by</dt>
            <dd>{proof.data.recipientName ?? 'Photo only'}</dd>
          </div>
          <div>
            <dt>Handed over</dt>
            <dd className="mono">{formatDateTime(proof.data.capturedAtUtc)}</dd>
          </div>
        </dl>
        {proof.data.note && <p className="note-box">{proof.data.note}</p>}
        {proofPhoto && <img className="outcome-photo" src={proofPhoto} alt="Handover proof" />}
      </section>
    )
  }

  if (failed && failure.data) {
    return (
      <section className="card outcome outcome-failed">
        <h2>
          <Icon name="alert" /> Delivery failed
        </h2>
        <dl className="fact-grid">
          <div>
            <dt>Reason</dt>
            <dd>{FAILURE_REASON_LABELS[failure.data.reason]}</dd>
          </div>
          <div>
            <dt>Reported</dt>
            <dd className="mono">{formatDateTime(failure.data.reportedAtUtc)}</dd>
          </div>
        </dl>
        {failure.data.note && <p className="note-box">{failure.data.note}</p>}
        {failurePhoto && <img className="outcome-photo" src={failurePhoto} alt="Delivery attempt" />}
      </section>
    )
  }

  return null
}
