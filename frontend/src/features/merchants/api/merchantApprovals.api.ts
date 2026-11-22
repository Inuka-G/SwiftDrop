import { apiClient } from '../../../lib/axios'
import { USE_MOCKS } from '../../../lib/constants'
import type { MerchantApprovalQueueItemDto, MerchantDto } from '../types/merchantApprovals.types'
import { getPendingMerchantsMock, reviewMerchantMock } from './merchantApprovals.mocks'

export async function getPendingMerchants(): Promise<MerchantApprovalQueueItemDto[]> {
  if (USE_MOCKS) return getPendingMerchantsMock()
  const { data } = await apiClient.get<MerchantApprovalQueueItemDto[]>('/merchants/pending')
  return data
}

export async function approveMerchant(id: string): Promise<MerchantDto> {
  if (USE_MOCKS) return reviewMerchantMock(id, 'approve')
  const { data } = await apiClient.post<MerchantDto>(`/merchants/${id}/approve`)
  return data
}

export async function rejectMerchant(id: string): Promise<MerchantDto> {
  if (USE_MOCKS) return reviewMerchantMock(id, 'reject')
  const { data } = await apiClient.post<MerchantDto>(`/merchants/${id}/reject`)
  return data
}
