import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'

const navigationByRole = {
  ProjectManager: [{ label: 'Projects', to: '/construction/projects' }, { label: 'Sites', to: '/construction/sites' }, { label: 'Phases', to: '/construction/phases' }, { label: 'Activities', to: '/construction/activities' }, 'Scheduling', 'AI Workflows', 'Approvals', 'Reports'],
  InventoryOfficer: ['Materials', 'Warehouses', 'Stock', 'Reservations', 'Alerts'],
  ProcurementOfficer: ['Suppliers', 'Quotations', 'Purchase Requests', 'Purchase Orders', 'Deliveries'],
  Administrator: [{ label: 'Users & roles', to: '/admin' }, { label: 'Projects', to: '/construction/projects' }, { label: 'Sites', to: '/construction/sites' }, { label: 'Phases', to: '/construction/phases' }, { label: 'Activities', to: '/construction/activities' }, 'System Settings', 'Audit'],
  SiteEngineer: [{ label: 'Projects', to: '/construction/projects' }, 'Requests', 'Progress', 'Approved Plans'],
}

export default function DashboardLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const items = user.roles.flatMap((role) => navigationByRole[role] ?? [])
  const handleLogout = async () => { await logout(); navigate('/login', { replace: true }) }

  return (
    <div className="dashboard-shell">
      <aside className="sidebar">
        <NavLink className="brand brand-inverse" to="/dashboard"><span className="brand-mark">BF</span><span>BuildFlow <strong>AI</strong></span></NavLink>
        <nav aria-label="Dashboard navigation">
          <NavLink to="/dashboard">Overview</NavLink>
          {items.map((item) => typeof item === 'string'
            ? <span className="nav-placeholder" key={item}>{item}</span>
            : <NavLink to={item.to} key={item.to}>{item.label}</NavLink>)}
        </nav>
        <button className="sidebar-logout" type="button" onClick={handleLogout}>Sign out</button>
      </aside>
      <section className="dashboard-main">
        <header className="dashboard-topbar">
          <div><span className="eyebrow">Workspace</span><strong>Construction operations</strong></div>
          <div className="user-summary"><span className="avatar">{user.fullName.charAt(0).toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.roles.join(', ')}</small></span></div>
        </header>
        <Outlet />
      </section>
    </div>
  )
}
