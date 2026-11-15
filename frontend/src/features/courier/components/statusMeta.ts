import type { CourierDelivery, FailureReason, ParcelStatus } from '../types/courier.types'

export const STATUS_LABELS: Record<ParcelStatus, string> = {
  Created: 'Awaiting pickup',
  PickedUp: 'Picked up',
  InTransit: 'In transit',
  OutForDelivery: 'Out for delivery',
  Delivered: 'Delivered',
  Failed: 'Failed',
}

/** The button text for moving a parcel into each in-progress status. */
export const ADVANCE_ACTIONS: Partial<Record<ParcelStatus, string>> = {
  PickedUp: 'Confirm pickup',
  InTransit: 'Start transit',
  OutForDelivery: 'Out for delivery',
}

export const MILESTONES: ParcelStatus[] = ['Created', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered']

export const MILESTONE_LABELS: Record<ParcelStatus, string> = {
  Created: 'Order created',
  PickedUp: 'Picked up',
  InTransit: 'In transit',
  OutForDelivery: 'Out for delivery',
  Delivered: 'Delivered',
  Failed: 'Delivery failed',
}

export const FAILURE_REASON_LABELS: Record<FailureReason, string> = {
  RecipientUnavailable: 'Recipient unavailable / phone unreachable',
  WrongAddress: 'Wrong or incomplete address',
  RefusedByRecipient: 'Refused by recipient',
  DamagedParcel: 'Parcel damaged',
  AccessRestricted: 'Could not access location',
  Other: 'Other (describe in notes)',
}

export function isClosed(status: ParcelStatus): boolean {
  return status === 'Delivered' || status === 'Failed'
}

/** Before pickup the courier heads to the sender; afterwards, to the recipient. */
export function currentStop(d: CourierDelivery) {
  return d.currentStatus === 'Created'
    ? { kind: 'Pickup from', name: d.pickupContactName, address: d.pickupAddress, phone: d.pickupContactPhone }
    : { kind: 'Deliver to', name: d.recipientName, address: d.dropoffAddress, phone: isClosed(d.currentStatus) ? null : d.recipientPhone }
}

export function formatCurrency(amount: number): string {
  return `LKR ${new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)}`
}
