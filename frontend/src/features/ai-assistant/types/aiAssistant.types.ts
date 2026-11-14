export interface ChatRequest {
  message: string
}

export interface ChatResponse {
  message: string
  toolsUsed: string[]
}

export interface ChatMessage {
  id: number
  role: 'user' | 'assistant'
  text: string
  toolsUsed?: string[]
}

