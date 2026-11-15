export interface ApiError {
  title: string
  status: number
  detail?: string
}

export type AsyncStatus = 'idle' | 'loading' | 'success' | 'error'
