import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'
import { apiErrorMessage } from '../services/api.js'

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [form, setForm] = useState({ email: '', password: '', remember: false })
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) return <Navigate to="/dashboard" replace />

  const update = ({ target }) => setForm((current) => ({ ...current, [target.name]: target.type === 'checkbox' ? target.checked : target.value }))
  const submit = async (event) => {
    event.preventDefault(); setError(''); setIsSubmitting(true)
    try {
      await login({ email: form.email.trim(), password: form.password }, form.remember)
      navigate(location.state?.from?.pathname ?? '/dashboard', { replace: true })
    } catch (requestError) { setError(apiErrorMessage(requestError)) } finally { setIsSubmitting(false) }
  }

  return <AuthPage title="Welcome back" subtitle="Sign in to coordinate your construction operations.">
    <form className="auth-form" onSubmit={submit} noValidate>
      {error && <div className="form-alert" role="alert">{error}</div>}
      <Field label="Email address"><input name="email" type="email" value={form.email} onChange={update} autoComplete="email" required /></Field>
      <Field label="Password"><div className="password-field"><input name="password" type={showPassword ? 'text' : 'password'} value={form.password} onChange={update} autoComplete="current-password" required /><button type="button" onClick={() => setShowPassword((value) => !value)}>{showPassword ? 'Hide' : 'Show'}</button></div></Field>
      <label className="checkbox-row"><input name="remember" type="checkbox" checked={form.remember} onChange={update} />Keep me signed in on this device</label>
      <button className="button button-primary button-full" disabled={isSubmitting}>{isSubmitting ? 'Signing in…' : 'Sign in'}</button>
      <p className="auth-switch">New to BuildFlow? <Link to="/register">Create an account</Link></p>
    </form>
  </AuthPage>
}

export function AuthPage({ title, subtitle, children }) {
  return <main className="auth-page"><section className="auth-story"><span className="badge badge-dark">Secure operations workspace</span><h1>Keep every site decision connected.</h1><p>Plan resources, prevent conflicts and move approved work forward with one shared system.</p><div className="trust-list"><span>✓ Role-based access</span><span>✓ Human approval controls</span><span>✓ Auditable decisions</span></div></section><section className="auth-panel"><div className="auth-card"><span className="eyebrow">BuildFlow AI</span><h2>{title}</h2><p>{subtitle}</p>{children}</div></section></main>
}

export function Field({ label, error, children }) {
  return <label className="field"><span>{label}</span>{children}{error && <small className="field-error">{error}</small>}</label>
}
