import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { authService } from '../../../services/auth.service'
import { Icon } from '../components/Icon'
import { TopBar } from '../components/TopBar'
import { formatCurrency } from '../components/statusMeta'
import { useCourierDeliveries } from '../hooks/useCourierDeliveries'
import { useTodayLedger } from '../hooks/useTodayLedger'

/** The courier's profile and today's ledger, built only from real data. */
export function ShiftPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const user = authService.getCurrentUser()
  const { data: deliveries = [] } = useCourierDeliveries()
  const ledger = useTodayLedger(deliveries)

  function endShift() {
    authService.logout()
    queryClient.clear()   // drop this courier's cached deliveries
    navigate('/login', { replace: true })
  }

  return (
    <>
      <TopBar eyebrow="On duty" title="Driver Profile" />

      <section className="card profile-card">
        <span className="avatar avatar-lg" aria-hidden="true">
          <Icon name="user" size={28} />
        </span>
        <div>
          <h2 className="profile-name">{user?.fullName}</h2>
          <p className="mono primary-text">{user?.username ?? 'courier'}</p>
          <p className="muted small">{user?.email}</p>
        </div>
      </section>

      <h2 className="section-heading">Today's operational ledger</h2>
      <div className="ledger-grid">
        <section className="card">
          <div className="ledger-card-head">
            <p className="label">Deliveries</p>
            <Icon name="box" />
          </div>
          <p className="ledger-value">
            {ledger.closed} <span>/ {ledger.total} runs</span>
          </p>
          <div className="progress">
            <span style={{ width: `${ledger.percentDone}%` }} />
          </div>
          <p className="primary-text small">{ledger.percentDone}% complete</p>
        </section>
        <section className="card">
          <div className="ledger-card-head">
            <p className="label">Success rate</p>
            <Icon name="checkCircle" />
          </div>
          <p className="ledger-value">{ledger.successRate === null ? '—' : `${ledger.successRate}%`}</p>
          <p className="muted small">
            {ledger.delivered} delivered · {ledger.failed} failed
          </p>
        </section>
      </div>

      <section className="card cod-card">
        <Icon name="cash" size={24} />
        <div>
          <p className="label">Cash collected today (COD)</p>
          <p className="cod-amount">{formatCurrency(ledger.codCollected)}</p>
          <p className="muted small">Still to collect: {formatCurrency(ledger.codPending)}</p>
        </div>
      </section>

      <button className="btn btn-warning btn-block btn-lg" type="button" onClick={endShift}>
        <Icon name="logout" /> End Shift & Sign Out
      </button>
      <p className="muted small center">Hand over any cash collected at the depot before leaving.</p>
    </>
  )
}
