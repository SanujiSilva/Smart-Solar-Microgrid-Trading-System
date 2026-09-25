import { PasswordInput } from '../components/PasswordInput'
import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { WEB_ACCESS_MESSAGE, WebAccessError } from '../auth/webAccess'
import { emailError } from '../lib/contactValidation'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [identifier, setIdentifier] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const identifierError = identifier ? emailError(identifier) : ''

  if (user) return <Navigate to="/dashboard" replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    if (!identifier.trim() || !password) {
      setError('Enter your staff email and password.')
      return
    }
    setIsSubmitting(true)
    try {
      await login(identifier.trim(), password)
      const destination = (location.state as { from?: string } | null)?.from ?? '/dashboard'
      navigate(destination, { replace: true })
    } catch (requestError: unknown) {
      const status = typeof requestError === 'object' && requestError !== null && 'response' in requestError
        ? (requestError as { response?: { status?: number } }).response?.status
        : undefined
      setError(requestError instanceof WebAccessError ? requestError.message : status === 403
        ? 'This account is not active. Contact Backoffice for assistance.'
        : 'We could not sign you in. Check your credentials and try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-intro">
        <div className="eyebrow">Secure access / Smart Solar Microgrid</div>
        <h1>Operate the<br /><span>living grid.</span></h1>
        <p>Use your server-issued account to enter the operational workspace.</p>
      </div>
      <form className="auth-card" onSubmit={submit}>
        <div className="panel-label">Sign in</div>
        <h2>Welcome back</h2>
        <p className="auth-card-copy">{WEB_ACCESS_MESSAGE}</p>
        {error && <div className="alert alert-danger auth-alert" role="alert">{error}</div>}
        <label className="form-label" htmlFor="identifier">Staff email</label>
        <input className="form-control" id="identifier" type="email" required maxLength={254} value={identifier} onChange={(event) => setIdentifier(event.target.value)} autoComplete="username" disabled={isSubmitting} aria-invalid={!!identifierError} aria-describedby="identifier-error" />
        {identifierError && <small id="identifier-error" className="text-danger">{identifierError}</small>}
        <label className="form-label" htmlFor="password">Password</label>
        <PasswordInput className="form-control" id="password" required maxLength={128} value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" disabled={isSubmitting} />
        <button className="btn auth-submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Checking account...' : 'Enter workspace'}
        </button>
        <small className="server-note">Identity and permissions are validated by the API.</small>
      </form>
    </main>
  )
}
