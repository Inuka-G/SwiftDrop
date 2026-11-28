export type MerchantStatus = 'Pending' | 'Approved' | 'Rejected' | 'Suspended'

export type BusinessRegType = 'BRN' | 'CRN' | 'TIN'

export interface MerchantDto {
  id: string
  businessName: string
  businessRegType?: BusinessRegType | string
  businessRegNumber: string
  status: MerchantStatus | string
  webhookUrl: string | null
  userId: string | null
  approvedBy: string | null
  approvedAt: string | null
  createdAtUtc: string
}

export interface RegisterMerchantCommand {
  businessName: string
  businessRegType: BusinessRegType
  businessRegNumber: string
  webhookUrl?: string | null
}

export interface UpdateMerchantProfileCommand {
  businessName: string
  businessRegType: BusinessRegType
  businessRegNumber: string
  webhookUrl?: string | null
}

export interface MerchantApprovalQueueItemDto {
  id: string
  businessName: string
  businessRegType?: BusinessRegType | string
  businessRegNumber: string
  status: string
  userId: string | null
  createdAtUtc: string
  webhookUrl?: string | null
}

export interface ApiKeySummaryDto {
  id: string
  name: string
  keyPrefix: string
  isActive: boolean
  createdAtUtc: string
  revokedAtUtc: string | null
}

export interface CreatedApiKeyDto {
  id: string
  name: string
  keyPrefix: string
  plainTextKey: string
  createdAtUtc: string
}

export interface CreateApiKeyCommand {
  name: string
}

export interface ClientCredentialSummaryDto {
  id: string
  name: string
  clientId: string
  isActive: boolean
  createdAtUtc: string
  revokedAtUtc: string | null
}

export interface CreatedClientCredentialDto {
  id: string
  name: string
  clientId: string
  plainTextClientSecret: string
  createdAtUtc: string
}

export interface CreateClientCredentialCommand {
  name: string
}

