
import type { HubConnection } from '@microsoft/signalr'

export interface DeliveryStatusUpdatedEvent {
  parcelId: string
  trackingNumber: string
  status: string
  statusChangedAtUtc: string
}

export function onDeliveryStatusUpdated(
  connection: HubConnection,
  handler: (event: DeliveryStatusUpdatedEvent) => void,
): () => void {
  const eventName = 'DeliveryStatusUpdated'

  // SignalR does not deduplicate handlers, so make registration idempotent.
  connection.off(eventName, handler)
  connection.on(eventName, handler)

  let subscribed = true
  return () => {
    if (!subscribed) return
    subscribed = false
    connection.off(eventName, handler)
  }
}
