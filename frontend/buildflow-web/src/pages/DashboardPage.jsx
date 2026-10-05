import React, { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'
import { api, apiErrorMessage } from '../services/api.js'

export default function DashboardPage() {
  const { user } = useAuth()
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  useEffect(() => { let active = true; api.get('/dashboard').then(response => { if (active) setData(response.data) }).catch(cause => { if (active) setError(apiErrorMessage(cause)) }); return () => { active = false } }, [])
  const manager = user.roles.some(role => ['Administrator', 'ProjectManager'].includes(role))
  const cards = [['Active projects', 'activeProjects', '/construction/projects'], ['Active activities', 'activeActivities', '/construction/activities'], ['Approved schedules', 'approvedPlans', '/scheduling'], ['Pending resource requests', 'pendingRequests', '/construction/activities'], ['Stock shortages', 'lowStock', '/inventory']]
  if (manager) cards.push(['Plans awaiting approval', 'pendingApprovals', '/workflows'], ['Pending purchase requests', 'pendingPurchaseRequests', '/purchase-requests'], ['Pending deliveries', 'pendingDeliveries', '/deliveries'])
  return <main className="dashboard-content"><span className="eyebrow">Overview</span><h1>Welcome back, {user.fullName.split(' ')[0]}.</h1><p className="lead">Current project operations and resource status.</p>{error && <p className="form-alert" role="alert">{error}</p>}<div className="summary-grid">{cards.map(([label, key, to]) => <article key={key}><span>{label}</span><strong>{data ? data[key] : '…'}</strong><Link to={to}>Open workspace</Link></article>)}</div></main>
}
