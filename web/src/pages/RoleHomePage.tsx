import { type UserRole } from '../auth/authContext'
import { useAuth } from '../auth/useAuth'

const roleLabels: Record<UserRole, string> = {
  BACKOFFICE: 'Backoffice workspace',
  GRID_OPERATOR: 'Grid Operator workspace',
  PROSUMER: 'Prosumer workspace',
}

export function RoleHomePage() {
  const { user } = useAuth()
  if (!user) return null

  return (
    <section className="workspace-page">
      <div className="eyebrow">Authenticated / {user.role}</div>
      <h1>{roleLabels[user.role]}</h1>
      <p className="lead-copy">You are signed in as <strong>{user.fullName}</strong>. Use the native Android app for prosumer registration, profile management, nearby stations and your dashboard.</p>
      <div className="workspace-strip">
        <div><span className="panel-label">Account status</span><strong>{user.status}</strong></div>
        <div><span className="panel-label">Server identity</span><strong>{user.email}</strong></div>
        <div><span className="panel-label">API authority</span><strong>Connected</strong></div>
      </div>
    </section>
  )
}
