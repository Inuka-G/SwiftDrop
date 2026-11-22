export interface MerchantApprovalQueueItemDto {
  id: string
  businessName: string
  businessRegNumber: string
  status: string
  userId: string | null
  createdAtUtc: string
}

export interface MerchantDto {
  id: string
  businessName: string
  businessRegNumber: string
  status: string
  webhookUrl: string | null
  userId: string | null
  approvedBy: string | null
  approvedAt: string | null
  createdAtUtc: string
}
