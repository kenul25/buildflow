import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'

export default function PublicLayout() {
  const { user } = useAuth()
  return (
    <div className="site-shell">
      <header className="site-header">
        <Link className="brand" to="/" aria-label="BuildFlow AI home">
          <span className="brand-mark">BF</span>
          <span>BuildFlow <strong>AI</strong></span>
        </Link>
        <nav className="public-nav" aria-label="Primary navigation">
          <NavLink to="/">Home</NavLink>
          <a href="/#platform">Platform</a>
          <a href="/#workflow">Workflow</a>
        </nav>
        <div className="header-actions">
          {user ? <Link className="button button-primary" to="/dashboard">Dashboard</Link> : (
            <><Link className="button button-ghost" to="/login">Sign in</Link><Link className="button button-primary" to="/register">Get started</Link></>
          )}
        </div>
      </header>
      <Outlet />
    </div>
  )
}
