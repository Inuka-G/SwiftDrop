import { apiClient } from '../../../lib/axios'
import type { ChatRequest, ChatResponse } from '../types/aiAssistant.types'

export const aiAssistantApi = {
  async chat(request: ChatRequest): Promise<ChatResponse> {
    const response = await apiClient.post<ChatResponse>(
      '/ai-assistant/chat',
      request,
    )
    return response.data
  },
}

