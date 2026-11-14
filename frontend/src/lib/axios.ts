import axios from 'axios'
import { storageService } from '../services/storage.service'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5017/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const token = storageService.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})
