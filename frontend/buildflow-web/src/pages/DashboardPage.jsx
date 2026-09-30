import { useAuth } from '../hooks/useAuth.js'

export default function DashboardPage() {
  const { user } = useAuth()
  return (
    <main className="dashboard-content">
      <span className="eyebrow">Overview</span>
      <h1>Welcome back, {user.fullName.split(' ')[0]}.</h1>
      <p className="lead">Your secure BuildFlow workspace is ready. Business modules will appear here as they are connected.</p>
      <div className="summary-grid">
        <article><span>Account status</span><strong>Active</strong><small>Session protected with JWT</small></article>
        <article><span>Access level</span><strong>{user.roles[0]?.replace(/([a-z])([A-Z])/g, '$1 $2')}</strong><small>Managed by role permissions</small></article>
        <article><span>Notifications</span><strong>0</strong><small>No new alerts</small></article>
      </div>
    </main>
  )
}
