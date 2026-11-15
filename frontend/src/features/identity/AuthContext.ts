import { createContext } from 'react'
import type { AuthUser, SignupData } from '../../services/auth.service'

export interface AuthContextValue {
  user: AuthUser | null
  /** True while the saved token is being checked on page load. */
  isLoading: boolean
  login(username: string, password: string, remember: boolean): Promise<AuthUser>
  signup(data: SignupData): Promise<AuthUser>
  completeGoogleLogin(code: string): Promise<AuthUser>
  logout(): void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
