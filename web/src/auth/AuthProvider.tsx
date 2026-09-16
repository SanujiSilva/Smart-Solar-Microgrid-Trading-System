import { useEffect, useState, type ReactNode } from 'react'
import { apiClient, ACCESS_TOKEN_KEY } from '../lib/api'
import { AuthContext, type AuthUser } from './authContext'

type LoginResponse = {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  user: AuthUser
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(() => Boolean(sessionStorage.getItem(ACCESS_TOKEN_KEY)))

  useEffect(() => {
    const token = sessionStorage.getItem(ACCESS_TOKEN_KEY)
    if (!token) return

    apiClient.get<AuthUser>('/auth/me')
      .then(({ data }) => setUser(data))
      .catch(() => sessionStorage.removeItem(ACCESS_TOKEN_KEY))
      .finally(() => setIsLoading(false))
  }, [])

  async function login(identifier: string, password: string) {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', { identifier, password })
    sessionStorage.setItem(ACCESS_TOKEN_KEY, data.accessToken)
    setUser(data.user)
  }

  function logout() {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY)
    setUser(null)
  }

  return <AuthContext.Provider value={{ user, isLoading, login, logout }}>{children}</AuthContext.Provider>
}
