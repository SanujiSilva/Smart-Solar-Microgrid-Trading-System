import { BrowserRouter, NavLink, Route, Routes } from 'react-router-dom'
import { apiClient } from './lib/api'
import './App.css'

function FoundationPage() {
  const apiUrl = apiClient.defaults.baseURL ?? '/api'

  return (
    <section className="foundation-page">
      <div className="eyebrow">Phase 11 / web foundation</div>
      <h1>Smart Solar<br /><span>Microgrid</span></h1>
      <p className="lead-copy">The web client is connected to the central API boundary and ready for authenticated role workflows.</p>
      <div className="foundation-grid">
        <article className="status-panel status-panel--primary">
          <div className="status-mark" aria-hidden="true">01</div>
          <div>
            <div className="panel-label">Client stack</div>
            <h2>React + Bootstrap</h2>
            <p>Responsive presentation layer with routing, reusable API access, and no client-side business rules.</p>
          </div>
        </article>
        <article className="status-panel">
          <div className="panel-label">API base URL</div>
          <code>{apiUrl}</code>
          <p>Configured with <code>VITE_API_BASE_URL</code> when provided.</p>
        </article>
        <article className="status-panel">
          <div className="panel-label">Next delivery</div>
          <h2>Authentication shell</h2>
          <p>Login, protected routing, and role-aware navigation arrive in Phase 12.</p>
        </article>
      </div>
    </section>
  )
}

function App() {
  return (
    <BrowserRouter>
      <div className="app-frame">
        <header className="topbar">
          <NavLink className="brand" to="/" aria-label="Smart Solar Microgrid home">
            <span className="brand-symbol" aria-hidden="true">S</span>
            <span>SMART SOLAR <b>MICROGRID</b></span>
          </NavLink>
          <nav className="topnav" aria-label="Primary navigation">
            <NavLink className={({ isActive }) => isActive ? 'topnav-link is-active' : 'topnav-link'} to="/">Foundation</NavLink>
            <span className="phase-chip">API connected</span>
          </nav>
        </header>
        <main>
          <Routes>
            <Route path="*" element={<FoundationPage />} />
          </Routes>
        </main>
        <footer className="footer-line">
          <span>REST / JSON</span><span>ASP.NET Core API</span><span>MongoDB authoritative</span>
        </footer>
      </div>
    </BrowserRouter>
  )
}

export default App
