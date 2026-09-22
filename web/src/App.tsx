import { BrowserRouter, Navigate, NavLink, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { AuthProvider } from './auth/AuthProvider'
import { type UserRole } from './auth/authContext'
import { useAuth } from './auth/useAuth'
import { LoginPage } from './pages/LoginPage'
import { BackofficePage } from './pages/BackofficePage'
import { OperatorPage } from './pages/OperatorPage'
import { RoleHomePage } from './pages/RoleHomePage'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { SolarIcon, SolarLandscape } from './components/SolarVisuals'

const roleLinks: Record<UserRole, { label: string; path: string }[]> = {
  BACKOFFICE: [{ label: 'Dashboard', path: '/dashboard' }, { label: 'Users', path: '/users' }, { label: 'Prosumers', path: '/prosumers' }, { label: 'Stations', path: '/stations' }, { label: 'Reservations', path: '/reservations' }],
  GRID_OPERATOR: [{ label: 'Dashboard', path: '/dashboard' }, { label: 'Stations', path: '/stations' }, { label: 'Reservations', path: '/reservations' }],
  PROSUMER: [{ label: 'Account', path: '/dashboard' }],
}

function WorkspaceFrame() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const accountsScreen = user?.role === 'BACKOFFICE'
  const dashboardScreen = accountsScreen && !/^\/(users|prosumers|stations|reservations)/.test(location.pathname)
  const searchLabel = location.pathname.startsWith('/reservations') ? 'Search reservation code' : location.pathname.startsWith('/stations') ? 'Search stations' : 'Search user accounts'
  if (!user) return null
  return <div className="app-frame workspace-frame accounts-theme overview-theme">
    <header className="topbar">
      <NavLink className="brand" to="/dashboard" aria-label="Smart Solar Microgrid dashboard"><span className="brand-sun" aria-hidden="true">&#9728;</span><span>SMART SOLAR <b>MICROGRID</b></span></NavLink>
      {accountsScreen && <form className="header-account-search" role="search" onSubmit={event => { event.preventDefault(); const fields = new FormData(event.currentTarget); const value = String(fields.get('search') ?? ''); const path = dashboardScreen ? String(fields.get('target') ?? '/users') : location.pathname; navigate(`${path}?search=${encodeURIComponent(value)}`) }}>{dashboardScreen && <select name="target" aria-label="Search category"><option value="/users">Users</option><option value="/stations">Stations</option><option value="/reservations">Reservations</option></select>}<input name="search" type="search" maxLength={100} aria-label={dashboardScreen ? 'Search workspace' : `Header ${searchLabel}`} placeholder={dashboardScreen ? 'Search the selected category…' : `${searchLabel}…`} /><button type="submit" aria-label="Submit search">⌕</button></form>}
      <div className="workspace-actions"><span className="user-avatar" aria-hidden="true">{user.fullName.slice(0, 1).toUpperCase()}</span><span className="user-chip">{user.fullName} <b>{user.role}</b></span><button className="logout-button" type="button" onClick={logout}><SolarIcon name="Logout" />Sign out</button></div>
    </header>
    <div className="workspace-body">
      <aside className="side-nav" aria-label="Role navigation">
        <div className="side-caption">Workspace</div>
        {roleLinks[user.role].map((link) => <NavLink key={link.path} className={({ isActive }) => isActive ? 'side-link is-active' : 'side-link'} to={link.path}><SolarIcon name={link.label} />{link.label}</NavLink>)}
        <div className="sidebar-scene"><SolarLandscape /><p>Clean energy<br />brighter communities</p><span /></div>
      </aside>
      <main className="workspace-content"><Routes><Route path="*" element={user.role === 'BACKOFFICE' ? <BackofficePage /> : user.role === 'GRID_OPERATOR' ? <OperatorPage /> : <RoleHomePage />} /></Routes></main>
    </div>
    <footer className="footer-line"><span className="footer-brand">Smart Solar <b>Microgrid</b></span><span>Authenticated session</span><span>API authority</span><span>{user.status}</span></footer>
  </div>
}

function App() {
  return (
    <AuthProvider><BrowserRouter><Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}><Route path="/*" element={<WorkspaceFrame />} /></Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes></BrowserRouter></AuthProvider>
  )
}

export default App
