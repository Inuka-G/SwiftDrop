import { useEffect } from 'react'
import { DISPATCH_TOAST_DURATION_MS } from '../../../lib/constants'
import type { AssignmentToastData } from '../types/dispatch.types'
import { courierName, shortId, vehicleGlyph } from '../utils/dispatchBoard.utils'

export function AssignmentToast({ data, onClose }: { data: AssignmentToastData; onClose: () => void }) {
  useEffect(() => {
    const timer = window.setTimeout(onClose, DISPATCH_TOAST_DURATION_MS)
    return () => window.clearTimeout(timer)
  }, [onClose])

  return <aside className="dispatch-toast" role="status" aria-live="polite">
    <span className="dispatch-toast-check" aria-hidden="true">✓</span>
    <span className="dispatch-toast-copy"><strong>Assignment confirmed</strong><span>Parcel {data.parcel.trackingNumber} assigned to {courierName(data.courier)} ({vehicleGlyph(data.courier.vehicleType)} {data.courier.vehicleType || 'Vehicle'} • {shortId(data.courier.zoneId)})</span></span>
    <button type="button" className="dispatch-icon-button" aria-label="Dismiss notification" onClick={onClose}>×</button>
  </aside>
}
