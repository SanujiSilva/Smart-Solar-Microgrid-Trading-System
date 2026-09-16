import { BrowserRouter, Navigate, NavLink, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthProvider'
import { type UserRole } from './auth/authContext'
import { useAuth } from './auth/useAuth'
import { LoginPage } from './pages/LoginPage'
import { RoleHomePage } from './pages/RoleHomePage'
import { ProtectedRoute } from './routes/ProtectedRoute'
import './App.css'

const roleLinks: Record<UserRole, { label: string; path: string }[]> = {
  BACKOFFICE: [{ label: 'Dashboard', path: '/dashboard' }, { label: 'Users', path: '/users' }, { label: 'Stations', path: '/stations' }],
  GRID_OPERATOR: [{ label: 'Dashboard', path: '/dashboard' }, { label: 'Stations', path: '/stations' }, { label: 'Reservations', path: '/reservations' }],
  PROSUMER: [{ label: 'Dashboard', path: '/dashboard' }, { label: 'My reservations', path: '/reservations' }, { label: 'Nearby stations', path: '/stations' }],
}

function WorkspaceFrame() {
  const { user, logout } = useAuth()
  if (!user) return null
  return <div className="app-frame workspace-frame">
    <header className="topbar">
      <NavLink className="brand" to="/dashboard" aria-label="Smart Solar Microgrid dashboard"><span className="brand-symbol" aria-hidden="true">S</span><span>SMART SOLAR <b>MICROGRID</b></span></NavLink>
      <div className="workspace-actions"><span className="user-chip">{user.fullName} <b>{user.role}</b></span><button className="logout-button" type="button" onClick={logout}>Sign out</button></div>
    </header>
    <div className="workspace-body">
      <aside className="side-nav" aria-label="Role navigation">
        <div className="side-caption">Workspace</div>
        {roleLinks[user.role].map((link) => <NavLink key={link.path} className={({ isActive }) => isActive ? 'side-link is-active' : 'side-link'} to={link.path}>{link.label}</NavLink>)}
      </aside>
      <main className="workspace-content"><Routes><Route path="dashboard" element={<RoleHomePage />} /><Route path="*" element={<RoleHomePage />} /></Routes></main>
    </div>
    <footer className="footer-line"><span>Authenticated session</span><span>API authority</span><span>{user.status}</span></footer>
  </div>
}

function App() {
  return (
    <AuthProvider><BrowserRouter><Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}><Route element={<WorkspaceFrame />}><Route path="/*" element={null} /></Route></Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes></BrowserRouter></AuthProvider>
  )
}

export default App
