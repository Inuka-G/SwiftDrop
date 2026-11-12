import type { ParcelStatus } from '../types/courier.types'
import { STATUS_LABELS } from './statusMeta'

export function StatusBadge({ status }: { status: ParcelStatus }) {
  return <span className={`status-badge status-${status}`}>{STATUS_LABELS[status]}</span>
}
