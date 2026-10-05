import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'

const navigationByRole = {
  ProjectManager: [
    { label: 'Projects', to: '/construction/projects' },
    { label: 'Sites', to: '/construction/sites' },
    { label: 'Phases', to: '/construction/phases' },
    { label: 'Activities', to: '/construction/activities' },
    { label: 'Inventory', to: '/inventory' },
    { label: 'Scheduling', to: '/scheduling' },
    { label: 'Plans & approvals', to: '/workflows' },

    { label: 'Purchase requests', to: '/purchase-requests' },
    { label: 'Purchase orders', to: '/purchase-orders' },
    { label: 'Deliveries', to: '/deliveries' },
    { label: 'Suppliers', to: '/suppliers' },
    { label: 'Supplier materials', to: '/supplier-materials' },
    { label: 'Quotations', to: '/quotations' },
  ],

  InventoryOfficer: [
    { label: 'Inventory', to: '/inventory' },
  ],

  ProcurementOfficer: [
    { label: 'Suppliers', to: '/suppliers' },
    { label: 'Supplier Materials', to: '/supplier-materials' },
    { label: 'Quotations', to: '/quotations' },
    { label: 'Compare Quotations', to: '/procurement/compare' },
    { label: 'Purchase Requests', to: '/purchase-requests' },
    { label: 'Purchase Orders', to: '/purchase-orders' },
    { label: 'Deliveries', to: '/deliveries' },
  ],

  Administrator: [
    { label: 'Users & roles', to: '/admin' },
    { label: 'Projects', to: '/construction/projects' },
    { label: 'Sites', to: '/construction/sites' },
    { label: 'Phases', to: '/construction/phases' },
    { label: 'Activities', to: '/construction/activities' },
    { label: 'Inventory', to: '/inventory' },
    { label: 'Suppliers', to: '/suppliers' },
    { label: 'Supplier Materials', to: '/supplier-materials' },
    { label: 'Quotations', to: '/quotations' },
    { label: 'Compare Quotations', to: '/procurement/compare' },
    { label: 'Purchase Requests', to: '/purchase-requests' },
    { label: 'Purchase Orders', to: '/purchase-orders' },
    { label: 'Deliveries', to: '/deliveries' },
    { label: 'Scheduling', to: '/scheduling' },
    { label: 'Plans & approvals', to: '/workflows' },
    'System Settings',
    'Audit',
  ],

  SiteEngineer: [
    { label: 'Projects', to: '/construction/projects' },
    { label: 'Requests & progress', to: '/construction/activities' },
    { label: 'Plans & approvals', to: '/workflows' },
    { label: 'Assignments & equipment', to: '/scheduling' },
  ],
}

export default function DashboardLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const items = user.roles
    .flatMap((role) => navigationByRole[role] ?? [])
    .filter(
      (item, index, array) =>
        typeof item === 'string'
          ? array.indexOf(item) === index
          : array.findIndex(
              (current) =>
                typeof current !== 'string' &&
                current.to === item.to,
            ) === index,
    )

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="dashboard-shell">
      <aside className="sidebar">
        <NavLink
          className="brand brand-inverse"
          to="/dashboard"
        >
          <span className="brand-mark">BF</span>

          <span>
            BuildFlow <strong>AI</strong>
          </span>
        </NavLink>

        <nav aria-label="Dashboard navigation">
          <NavLink to="/dashboard">
            Overview
          </NavLink>

          {items.map((item) =>
            typeof item === 'string' ? (
              <span
                className="nav-placeholder"
                key={item}
              >
                {item}
              </span>
            ) : (
              <NavLink
                to={item.to}
                key={item.to}
              >
                {item.label}
              </NavLink>
            ),
          )}
        </nav>

        <button
          className="sidebar-logout"
          type="button"
          onClick={handleLogout}
        >
          Sign out
        </button>
      </aside>

      <section className="dashboard-main">
        <header className="dashboard-topbar">
          <div>
            <span className="eyebrow">
              Workspace
            </span>

            <strong>
              Construction operations
            </strong>
          </div>

          <div className="user-summary">
            <span className="avatar">
              {user.fullName
                .charAt(0)
                .toUpperCase()}
            </span>

            <span>
              <strong>
                {user.fullName}
              </strong>

              <small>
                {user.roles.join(', ')}
              </small>
            </span>
          </div>
        </header>

        <Outlet />
      </section>
    </div>
  )
}
