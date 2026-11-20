import { Link } from 'react-router-dom'
import type { CourierDelivery } from '../types/courier.types'
import { ContactActions } from './ContactActions'
import { Icon } from './Icon'
import { StatusBadge } from './StatusBadge'
import { currentStop, formatCurrency } from './statusMeta'

/** One delivery in the Runs list. Shows the stop the courier must go to next. */
export function StopCard({ delivery, stopNumber }: { delivery: CourierDelivery; stopNumber: number }) {
  const stop = currentStop(delivery)

  return (
    <article className="card stop-card">
      <Link className="stop-card-link" to={`/courier/deliveries/${delivery.id}`}>
        <div className="stop-card-top">
          <span className="stop-chip">STOP #{stopNumber}</span>
          <span className="mono tracking">{delivery.trackingNumber}</span>
          <StatusBadge status={delivery.currentStatus} />
        </div>
        <p className="stop-kind">{stop.kind}</p>
        <h2 className="stop-name">{stop.name}</h2>
        <p className="stop-address">
          <Icon name="pin" size={16} />
          {stop.address}
        </p>
        <div className="stop-card-meta">
          <span>
            {delivery.weightKg} kg · {delivery.sizeCategory}
          </span>
          {delivery.amountToCollect > 0 && (
            <span className="cod-chip">COD {formatCurrency(delivery.amountToCollect)}</span>
          )}
        </div>
      </Link>
      {stop.phone && <ContactActions phone={stop.phone} address={stop.address} />}
    </article>
  )
}
