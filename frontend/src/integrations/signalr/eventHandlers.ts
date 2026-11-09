import type { HubConnection } from '@microsoft/signalr'

export interface ParcelStatusChangedEvent {
  trackingNumber: string
  status: string
  changedAt: string
}

export function onParcelStatusChanged(
  connection: HubConnection,
  handler: (event: ParcelStatusChangedEvent) => void,
): () => void {
  const eventName = 'ParcelStatusChanged'
  connection.on(eventName, handler)
  return () => connection.off(eventName, handler)
}
