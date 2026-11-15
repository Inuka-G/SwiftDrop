/** Mirrors SwiftDrop.Parcels.Contracts.ParcelStatuses. */
export type ParcelStatus =
  | 'Created'
  | 'PickedUp'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Failed'

/** Mirrors SwiftDrop.Parcels.Contracts.FailureReasons. */
export type FailureReason =
  | 'RecipientUnavailable'
  | 'WrongAddress'
  | 'RefusedByRecipient'
  | 'DamagedParcel'
  | 'AccessRestricted'
  | 'Other'

export interface CourierDelivery {
  id: string
  trackingNumber: string
  currentStatus: ParcelStatus
  pickupAddress: string
  pickupContactName: string
  pickupContactPhone: string
  dropoffAddress: string
  recipientName: string
  recipientPhone: string
  weightKg: number
  sizeCategory: string
  paymentMethod: string
  amountToCollect: number
  assignedAtUtc: string | null
  updatedAtUtc: string
  allowedNextStatuses: ParcelStatus[]
}

export interface StatusHistoryEntry {
  fromStatus: ParcelStatus | null
  toStatus: ParcelStatus
  note: string | null
  changedByRole: string
  occurredAtUtc: string
}

export interface DeliveryProof {
  recipientName: string | null
  note: string | null
  hasPhoto: boolean
  capturedAtUtc: string
}

export interface FailureReport {
  reason: FailureReason
  note: string | null
  hasPhoto: boolean
  reportedAtUtc: string
}

export type PhotoKind = 'proof' | 'failure'

export interface ConfirmDeliveryInput {
  recipientName: string
  note: string
  photo: File | null
}

export interface ReportFailureInput {
  reason: FailureReason
  note: string
  photo: File | null
}
