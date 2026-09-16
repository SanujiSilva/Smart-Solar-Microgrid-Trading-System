import axios from 'axios'

export const ACCESS_TOKEN_KEY = 'smart-solar.access-token'

export function apiError(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'The request could not be completed.'
  const problem = error.response?.data
  if (problem?.errors) return Object.values(problem.errors).flat().join(' ')
  return problem?.detail || problem?.title || 'Cannot reach the API. Check your connection and retry.'
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api',
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,
})

apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem(ACCESS_TOKEN_KEY)
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !error.config?.url?.includes('/auth/login')) {
      sessionStorage.removeItem(ACCESS_TOKEN_KEY)
      window.dispatchEvent(new Event('smart-solar-session-expired'))
    }
    return Promise.reject(error)
  },
)
