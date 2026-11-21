
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

  connection.on(eventName, handler)

  return () => connection.off(eventName, handler)
}
