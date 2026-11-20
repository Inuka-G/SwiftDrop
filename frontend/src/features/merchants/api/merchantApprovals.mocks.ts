import type { MerchantApprovalQueueItemDto, MerchantDto } from '../types/merchantApprovals.types'

const pending: MerchantApprovalQueueItemDto[] = [
  { id: 'd1000000-0000-4000-8000-000000000001', businessName: 'Ceylon Artisan Spices Ltd', businessRegNumber: 'PV-98214', status: 'PENDING', userId: 'a1000000-0000-4000-8000-000000000006', createdAtUtc: new Date().toISOString() },
  { id: 'd1000000-0000-4000-8000-000000000002', businessName: 'Lanka Tech Essentials', businessRegNumber: 'PV-76208', status: 'PENDING', userId: null, createdAtUtc: '2026-10-28T06:30:00Z' },
  { id: 'd1000000-0000-4000-8000-000000000003', businessName: 'Island Organics', businessRegNumber: 'PV-33810', status: 'PENDING', userId: null, createdAtUtc: '2026-10-20T09:00:00Z' },
]

function mockStatus(action: string) {
  const mode = new URLSearchParams(window.location.search).get('usersMock')
  if (mode === `${action}-404` || mode === `${action}-409` || mode === `${action}-403`) {
    const status = Number(mode.slice(action.length + 1))
    throw Object.assign(new Error('Mock merchant review error'), { response: { status } })
  }
}

export async function getPendingMerchantsMock() {
  mockStatus('pending')
  return pending.map((merchant) => ({ ...merchant }))
}

export async function reviewMerchantMock(id: string, decision: 'approve' | 'reject'): Promise<MerchantDto> {
  mockStatus(decision)
  const index = pending.findIndex((merchant) => merchant.id === id)
  if (index < 0) throw Object.assign(new Error('Not found'), { response: { status: 404 } })
  const [merchant] = pending.splice(index, 1)
  return { ...merchant, status: decision === 'approve' ? 'APPROVED' : 'REJECTED', webhookUrl: null, approvedBy: null, approvedAt: null }
}
