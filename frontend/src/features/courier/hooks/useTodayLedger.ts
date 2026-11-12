import { useMemo } from 'react'
import { isClosed } from '../components/statusMeta'
import type { CourierDelivery } from '../types/courier.types'

/** Today's counts, shared by the Runs and Shift screens. */
export function useTodayLedger(deliveries: CourierDelivery[]) {
  return useMemo(() => {
    const count = (match: (d: CourierDelivery) => boolean) => deliveries.filter(match).length
    const sum = (match: (d: CourierDelivery) => boolean) =>
      deliveries.filter(match).reduce((total, d) => total + d.amountToCollect, 0)

    const delivered = count((d) => d.currentStatus === 'Delivered')
    const failed = count((d) => d.currentStatus === 'Failed')
    const closed = delivered + failed
    return {
      total: deliveries.length,
      closed,
      delivered,
      failed,
      toPickUp: count((d) => d.currentStatus === 'Created'),
      onTheRoad: count((d) => !isClosed(d.currentStatus) && d.currentStatus !== 'Created'),
      percentDone: deliveries.length ? Math.round((closed / deliveries.length) * 100) : 0,
      successRate: closed ? Math.round((delivered / closed) * 100) : null,
      codCollected: sum((d) => d.currentStatus === 'Delivered'),
      codPending: sum((d) => !isClosed(d.currentStatus)),
    }
  }, [deliveries])
}
