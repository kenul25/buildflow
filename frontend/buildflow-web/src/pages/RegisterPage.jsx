import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'
import { apiErrorMessage } from '../services/api.js'
import { AuthPage, Field } from './LoginPage.jsx'

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const validPassword = (value) => value.length >= 8 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /\d/.test(value) && /[^A-Za-z0-9]/.test(value)

export default function RegisterPage() {
  const { user, register } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ fullName: '', email: '', password: '', confirmPassword: '' })
  const [errors, setErrors] = useState({})
  const [serverError, setServerError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  if (user) return <Navigate to="/dashboard" replace />

  const update = ({ target }) => setForm((current) => ({ ...current, [target.name]: target.value }))
  const validate = () => {
    const next = {}
    if (form.fullName.trim().length < 2) next.fullName = 'Enter your full name.'
    if (!emailPattern.test(form.email.trim())) next.email = 'Enter a valid email address.'
    if (!validPassword(form.password)) next.password = 'Use 8+ characters with uppercase, lowercase, number and symbol.'
    if (form.confirmPassword !== form.password) next.confirmPassword = 'Passwords do not match.'
    setErrors(next); return Object.keys(next).length === 0
  }
  const submit = async (event) => {
    event.preventDefault(); setServerError('')
    if (!validate()) return
    setIsSubmitting(true)
    try { await register({ ...form, fullName: form.fullName.trim(), email: form.email.trim() }); navigate('/dashboard', { replace: true }) }
    catch (requestError) { setServerError(apiErrorMessage(requestError)) }
    finally { setIsSubmitting(false) }
  }

  return <AuthPage title="Create your account" subtitle="Public accounts start with secure Site Engineer access.">
    <form className="auth-form" onSubmit={submit} noValidate>
      {serverError && <div className="form-alert" role="alert">{serverError}</div>}
      <Field label="Full name" error={errors.fullName}><input name="fullName" value={form.fullName} onChange={update} autoComplete="name" /></Field>
      <Field label="Email address" error={errors.email}><input name="email" type="email" value={form.email} onChange={update} autoComplete="email" /></Field>
      <Field label="Password" error={errors.password}><input name="password" type="password" value={form.password} onChange={update} autoComplete="new-password" /></Field>
      <Field label="Confirm password" error={errors.confirmPassword}><input name="confirmPassword" type="password" value={form.confirmPassword} onChange={update} autoComplete="new-password" /></Field>
      <p className="account-note">Project Manager and officer roles are assigned by an administrator.</p>
      <button className="button button-primary button-full" disabled={isSubmitting}>{isSubmitting ? 'Creating account…' : 'Create account'}</button>
      <p className="auth-switch">Already have an account? <Link to="/login">Sign in</Link></p>
    </form>
  </AuthPage>
}
