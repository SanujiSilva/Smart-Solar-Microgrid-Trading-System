import { createContext } from 'react'

export type UserRole = 'BACKOFFICE' | 'GRID_OPERATOR' | 'PROSUMER'
export type UserStatus = 'PENDING' | 'ACTIVE' | 'DEACTIVATION_REQUESTED' | 'DEACTIVATED'

export type AuthUser = {
  id: string
  nic: string | null
  fullName: string
  email: string
  phone: string
  role: UserRole
  status: UserStatus
}

type AuthContextValue = {
  user: AuthUser | null
  isLoading: boolean
  login: (identifier: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
