import { useId, useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'
import { apiErrorMessage } from '../services/api.js'
import './auth.css'

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [form, setForm] = useState({ email: '', password: '', remember: false })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) return <Navigate to="/dashboard" replace />

  const update = ({ target }) => setForm((current) => ({ ...current, [target.name]: target.type === 'checkbox' ? target.checked : target.value }))
  const submit = async (event) => {
    event.preventDefault(); setError(''); setIsSubmitting(true)
    try {
      const signedInUser = await login({ email: form.email.trim(), password: form.password }, form.remember)
      const defaultPath = signedInUser.roles.includes('Administrator') ? '/admin' : '/dashboard'
      navigate(location.state?.from?.pathname ?? defaultPath, { replace: true })
    } catch (requestError) { setError(apiErrorMessage(requestError)) } finally { setIsSubmitting(false) }
  }

  return <AuthPage title="Welcome back" subtitle="Sign in to your BuildFlow workspace.">
    <form className="auth-form" onSubmit={submit} noValidate>
      {error && <div className="form-alert" role="alert">{error}</div>}
      <Field label="Email address"><input name="email" type="email" value={form.email} onChange={update} autoComplete="email" placeholder="you@company.com" required /></Field>
      <PasswordField label="Password" name="password" value={form.password} onChange={update} autoComplete="current-password" placeholder="Enter your password" />
      <label className="checkbox-row"><input name="remember" type="checkbox" checked={form.remember} onChange={update} />Keep me signed in on this device</label>
      <button className="button button-primary button-full" disabled={isSubmitting}>{isSubmitting ? 'Signing in…' : 'Sign in'}</button>
      <p className="auth-switch">New to BuildFlow? <Link to="/register">Create an account</Link></p>
    </form>
  </AuthPage>
}

export function AuthPage({ title, subtitle, children }) {
  return (
    <main className="auth-page">
      <div className="auth-container">
        <Link className="auth-back" to="/">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><path d="m12 5-7 7 7 7M5 12h14" /></svg>
          Back to home
        </Link>
        <section className="auth-card" aria-labelledby="auth-title">
          <header className="auth-card-header">
            <span className="auth-brand-mark" aria-hidden="true">BF<span /></span>
            <span className="auth-brand-name">BuildFlow <strong>AI</strong></span>
            <h1 id="auth-title">{title}</h1>
            <p>{subtitle}</p>
          </header>
          {children}
        </section>
        <p className="auth-footer">A connected workspace for every project.</p>
      </div>
    </main>
  )
}

export function PasswordField({ label, error, hint, ...inputProps }) {
  const id = useId()
  const [visible, setVisible] = useState(false)
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <div className="password-field">
        <input {...inputProps} id={id} type={visible ? 'text' : 'password'} required aria-invalid={error ? true : undefined} aria-describedby={error || hint ? `${id}-help` : undefined} />
        <button type="button" aria-label={`${visible ? 'Hide' : 'Show'} ${label.toLowerCase()}`} title={`${visible ? 'Hide' : 'Show'} ${label.toLowerCase()}`} aria-pressed={visible} onClick={() => setVisible((value) => !value)}>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
            {visible ? <><path d="m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 5.2A10.7 10.7 0 0 1 12 5c5.5 0 9 7 9 7a16.7 16.7 0 0 1-3.2 4.1M6.2 6.2C4.2 7.8 3 10 3 12c0 0 3.5 7 9 7a10.6 10.6 0 0 0 4.3-.9" /></> : <><path d="M3 12s3.5-7 9-7 9 7 9 7-3.5 7-9 7-9-7-9-7Z" /><circle cx="12" cy="12" r="3" /></>}
          </svg>
        </button>
      </div>
      {(error || hint) && <small id={`${id}-help`} className={error ? 'field-error' : 'auth-field-hint'}>{error || hint}</small>}
    </div>
  )
}

export function Field({ label, error, children }) {
  return <label className="field"><span>{label}</span>{children}{error && <small className="field-error">{error}</small>}</label>
}
