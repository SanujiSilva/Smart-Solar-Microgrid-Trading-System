import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import 'bootstrap/dist/css/bootstrap.min.css'
import './index.css'
import './App.css'
import './reference-theme.css'
import './accounts-theme.css'
import './stations-theme.css'
import './reservations-theme.css'
import './overview-theme.css'
import './shared-theme.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
