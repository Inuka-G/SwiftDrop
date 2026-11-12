import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getErrorMessage } from '../../../lib/axios'
import { authService } from '../../../services/auth.service'
import { Icon } from '../components/Icon'
import { StopCard } from '../components/StopCard'
import { TopBar } from '../components/TopBar'
import { isClosed } from '../components/statusMeta'
import { useCourierDeliveries } from '../hooks/useCourierDeliveries'
import { useTodayLedger } from '../hooks/useTodayLedger'

/**
 * Today's runs. "active" lists stops still to do; "completed" lists those delivered
 * or failed today. Stop numbers follow the order the dispatcher assigned them.
 */
export function RunsPage({ view }: { view: 'active' | 'completed' }) {
  const user = authService.getCurrentUser()
  const [today] = useState(() => new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date()))
  const { data: deliveries = [], isLoading, isError, error, refetch, isFetching, dataUpdatedAt } =
    useCourierDeliveries()
  const ledger = useTodayLedger(deliveries)

  const stops = deliveries
    .map((delivery, index) => ({ delivery, stopNumber: index + 1 }))
    .filter(({ delivery }) => (view === 'active') !== isClosed(delivery.currentStatus))

  return (
    <>
      <TopBar
        eyebrow={<>Active run · {today}</>}
        title={view === 'active' ? 'My Runs' : 'Completed Today'}
        action={
          <Link className="avatar" to="/courier/shift" aria-label="Shift and profile">
            {initials(user?.fullName)}
          </Link>
        }
      />

      <section className="card ledger-card" aria-label="Today's progress">
        <div className="ledger-head">
          <div>
            <p className="label">Deliveries</p>
            <p className="ledger-value">
              {ledger.closed} <span>/ {ledger.total} runs</span>
            </p>
          </div>
          <button className="icon-button" type="button" onClick={() => void refetch()} disabled={isFetching}
            aria-label="Refresh deliveries">
            <span className={isFetching ? 'spin' : undefined}><Icon name="refresh" /></span>
          </button>
        </div>
        <div className="progress" role="progressbar" aria-valuenow={ledger.percentDone} aria-valuemin={0} aria-valuemax={100}>
          <span style={{ width: `${ledger.percentDone}%` }} />
        </div>
        <div className="ledger-chips">
          <span className="pill">{ledger.toPickUp} to pick up</span>
          <span className="pill pill-primary">{ledger.onTheRoad} on the road</span>
          <span className="pill pill-success">{ledger.delivered} delivered</span>
          {ledger.failed > 0 && <span className="pill pill-danger">{ledger.failed} failed</span>}
        </div>
        {dataUpdatedAt > 0 && (
          <p className="muted small">
            Updated {new Intl.DateTimeFormat(undefined, { timeStyle: 'short' }).format(dataUpdatedAt)}
          </p>
        )}
      </section>

      {isError ? (
        <section className="card" role="alert">
          <p className="form-error">{getErrorMessage(error)}</p>
          <button className="btn btn-primary btn-block" type="button" onClick={() => void refetch()}>
            Try again
          </button>
        </section>
      ) : isLoading ? (
        <p className="empty-state">Loading your deliveries…</p>
      ) : stops.length === 0 ? (
        <div className="empty-state">
          <Icon name={view === 'active' ? 'truck' : 'checkCircle'} size={32} />
          <p>
            {view === 'active'
              ? 'No open deliveries. New assignments from dispatch will appear here.'
              : 'Nothing delivered or failed yet today.'}
          </p>
        </div>
      ) : (
        <div className="stop-list">
          {stops.map(({ delivery, stopNumber }) => (
            <StopCard key={delivery.id} delivery={delivery} stopNumber={stopNumber} />
          ))}
        </div>
      )}
    </>
  )
}

function initials(name?: string) {
  if (!name) return '?'
  return name.split(' ').filter(Boolean).slice(0, 2).map((part) => part[0].toUpperCase()).join('')
}
