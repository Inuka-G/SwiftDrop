import { apiClient } from '../../../lib/axios'
import type {
  ApiKeySummaryDto,
  ClientCredentialSummaryDto,
  CreateApiKeyCommand,
  CreateClientCredentialCommand,
  CreatedApiKeyDto,
  CreatedClientCredentialDto,
  MerchantApprovalQueueItemDto,
  MerchantDto,
  RegisterMerchantCommand,
  UpdateMerchantProfileCommand,
} from '../types/merchant.types'

export const merchantApi = {
  /** Register a new merchant profile */
  async registerMerchant(command: RegisterMerchantCommand): Promise<MerchantDto> {
    const response = await apiClient.post<MerchantDto>('/merchants/register', command)
    return response.data
  },

  /** Get the currently logged-in user's merchant profile */
  async getMyMerchantProfile(): Promise<MerchantDto | null> {
    try {
      const response = await apiClient.get<MerchantDto>('/merchants/me')
      return response.data
    } catch (error: any) {
      if (error?.response?.status === 404) {
        return null
      }
      throw error
    }
  },

  /** Update the currently logged-in user's merchant profile */
  async updateMyMerchantProfile(command: UpdateMerchantProfileCommand): Promise<MerchantDto> {
    const response = await apiClient.put<MerchantDto>('/merchants/me', command)
    return response.data
  },

  /** Get a specific merchant application by ID (Admin only) */
  async getMerchantById(id: string): Promise<MerchantDto> {
    const response = await apiClient.get<MerchantDto>(`/merchants/${id}`)
    return response.data
  },

  /** Get list of pending merchants (Admin only) */
  async getPendingMerchants(): Promise<MerchantApprovalQueueItemDto[]> {
    const response = await apiClient.get<MerchantApprovalQueueItemDto[]>('/merchants/pending')
    return response.data
  },

  /** Approve a pending merchant (Admin only) */
  async approveMerchant(id: string): Promise<MerchantDto> {
    const response = await apiClient.post<MerchantDto>(`/merchants/${id}/approve`)
    if (response.data.userId) {
      try {
        await apiClient.patch(`/users/${response.data.userId}/role`, { role: 'MERCHANT' })
      } catch {
        // Role update is best-effort if user already has MERCHANT or ADMIN role
      }
    }
    return response.data
  },

  /** Reject a pending merchant (Admin only) */
  async rejectMerchant(id: string): Promise<MerchantDto> {
    const response = await apiClient.post<MerchantDto>(`/merchants/${id}/reject`)
    return response.data
  },

  /** Get all API keys for the current merchant */
  async getMyApiKeys(): Promise<ApiKeySummaryDto[]> {
    const response = await apiClient.get<ApiKeySummaryDto[]>('/merchants/api-keys')
    return response.data
  },

  /** Generate a new API key */
  async createApiKey(command: CreateApiKeyCommand): Promise<CreatedApiKeyDto> {
    const response = await apiClient.post<CreatedApiKeyDto>('/merchants/api-keys', command)
    return response.data
  },

  /** Revoke an existing API key */
  async revokeApiKey(keyId: string): Promise<void> {
    await apiClient.delete(`/merchants/api-keys/${keyId}`)
  },

  /** Get all OAuth client credentials for the current merchant */
  async getMyOAuthClients(): Promise<ClientCredentialSummaryDto[]> {
    const response = await apiClient.get<ClientCredentialSummaryDto[]>('/merchants/oauth-clients')
    return response.data
  },

  /** Create a new OAuth client credential */
  async createOAuthClient(command: CreateClientCredentialCommand): Promise<CreatedClientCredentialDto> {
    const response = await apiClient.post<CreatedClientCredentialDto>('/merchants/oauth-clients', command)
    return response.data
  },

  /** Revoke an existing OAuth client credential */
  async revokeOAuthClient(clientId: string): Promise<void> {
    await apiClient.delete(`/merchants/oauth-clients/${clientId}`)
  },
}

