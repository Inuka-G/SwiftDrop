import { useEffect } from 'react'
import { DISPATCH_TOAST_DURATION_MS } from '../../../lib/constants'

export interface CourierToastMessage {
  title: string
  message: string
  kind: 'success' | 'error' | 'info'
}

export function CourierToast({ data, onClose }: { data: CourierToastMessage; onClose: () => void }) {
  useEffect(() => {
    const timer = window.setTimeout(onClose, DISPATCH_TOAST_DURATION_MS)
    return () => window.clearTimeout(timer)
  }, [onClose])

  return <aside className={`dispatch-toast courier-toast is-${data.kind}`} role={data.kind === 'error' ? 'alert' : 'status'} aria-live={data.kind === 'error' ? 'assertive' : 'polite'}>
    <span className="dispatch-toast-check" aria-hidden="true">{data.kind === 'error' ? '!' : '✓'}</span>
    <span className="dispatch-toast-copy"><strong>{data.title}</strong><span>{data.message}</span></span>
    <button type="button" className="dispatch-icon-button" aria-label="Dismiss notification" onClick={onClose}>×</button>
  </aside>
}
