import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { type UserRole } from '../auth/authContext'
import { useAuth } from '../auth/useAuth'

export function ProtectedRoute({ roles }: { roles?: UserRole[] }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return <div className="route-loading">Restoring secure session...</div>
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (roles && !roles.includes(user.role)) return <Navigate to="/dashboard" replace />
  return <Outlet />
}
