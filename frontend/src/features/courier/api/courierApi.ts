import { apiClient } from '../../../lib/axios'
import type {
  ConfirmDeliveryInput,
  CourierDelivery,
  DeliveryProof,
  FailureReport,
  ParcelStatus,
  PhotoKind,
  ReportFailureInput,
  StatusHistoryEntry,
} from '../types/courier.types'

const DELIVERIES = '/courier/deliveries'

/** Builds a multipart body, skipping empty text fields so the server sees them as absent. */
function toForm(fields: Record<string, string>, photo: File | null): FormData {
  const form = new FormData()
  for (const [name, value] of Object.entries(fields)) {
    if (value.trim()) form.append(name, value.trim())
  }
  if (photo) form.append('photo', photo)
  return form
}

export const courierApi = {
  async listDeliveries(closedSince: string): Promise<CourierDelivery[]> {
    const { data } = await apiClient.get<CourierDelivery[]>(DELIVERIES, { params: { closedSince } })
    return data
  },

  async getDelivery(id: string): Promise<CourierDelivery> {
    const { data } = await apiClient.get<CourierDelivery>(`${DELIVERIES}/${id}`)
    return data
  },

  async updateStatus(id: string, status: ParcelStatus, note?: string): Promise<CourierDelivery> {
    const { data } = await apiClient.patch<CourierDelivery>(`${DELIVERIES}/${id}/status`, {
      status,
      note: note || null,
    })
    return data
  },

  async confirmDelivery(id: string, input: ConfirmDeliveryInput): Promise<CourierDelivery> {
    const form = toForm({ recipientName: input.recipientName, note: input.note }, input.photo)
    const { data } = await apiClient.post<CourierDelivery>(`${DELIVERIES}/${id}/deliver`, form)
    return data
  },

  async reportFailure(id: string, input: ReportFailureInput): Promise<CourierDelivery> {
    const form = toForm({ reason: input.reason, note: input.note }, input.photo)
    const { data } = await apiClient.post<CourierDelivery>(`${DELIVERIES}/${id}/fail`, form)
    return data
  },

  async getHistory(id: string): Promise<StatusHistoryEntry[]> {
    const { data } = await apiClient.get<StatusHistoryEntry[]>(`/parcels/${id}/history`)
    return data
  },

  async getProof(id: string): Promise<DeliveryProof> {
    const { data } = await apiClient.get<DeliveryProof>(`/parcels/${id}/proof`)
    return data
  },

  async getFailureReport(id: string): Promise<FailureReport> {
    const { data } = await apiClient.get<FailureReport>(`/parcels/${id}/failure`)
    return data
  },

  /** Photos need the auth header, so they are fetched as blobs rather than used as <img> URLs. */
  async getPhoto(kind: PhotoKind, id: string): Promise<Blob> {
    const path = kind === 'proof' ? 'proof/photo' : 'failure/photo'
    const { data } = await apiClient.get<Blob>(`/parcels/${id}/${path}`, { responseType: 'blob' })
    return data
  },
}
