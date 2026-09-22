import React, { useEffect, useMemo, useState } from 'react'
import { apiErrorMessage } from '../services/api.js'
import { adminService } from '../services/adminService.js'

const roleOptions = [
  ['SiteEngineer', 'Site Engineer'],
  ['ProjectManager', 'Project Manager'],
  ['InventoryOfficer', 'Inventory Officer'],
  ['ProcurementOfficer', 'Procurement Officer'],
]

const roleLabel = (role) => roleOptions.find(([value]) => value === role)?.[1] ?? role

export default function AdminDashboardPage() {
  const [users, setUsers] = useState([])
  const [draftRoles, setDraftRoles] = useState({})
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [savingUserId, setSavingUserId] = useState(null)

  useEffect(() => {
    adminService.getUsers()
      .then((data) => {
        setUsers(data)
        setDraftRoles(Object.fromEntries(data.map((user) => [user.id, user.roles[0] ?? 'SiteEngineer'])))
      })
      .catch((requestError) => setError(apiErrorMessage(requestError)))
      .finally(() => setIsLoading(false))
  }, [])

  const visibleUsers = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return users
    return users.filter((user) =>
      user.fullName.toLowerCase().includes(term) ||
      user.email.toLowerCase().includes(term) ||
      user.roles.some((role) => roleLabel(role).toLowerCase().includes(term)),
    )
  }, [search, users])

  const metrics = useMemo(() => ({
    total: users.length,
    siteEngineers: users.filter((user) => user.roles.includes('SiteEngineer')).length,
    privileged: users.filter((user) => user.roles.some((role) =>
      ['ProjectManager', 'InventoryOfficer', 'ProcurementOfficer'].includes(role),
    )).length,
    active: users.filter((user) => user.isActive).length,
  }), [users])

  const assignRole = async (user) => {
    const role = draftRoles[user.id]
    if (!role || user.roles.includes(role)) return
    setError('')
    setNotice('')
    setSavingUserId(user.id)
    try {
      const updated = await adminService.assignRole(user.id, role)
      setUsers((current) => current.map((item) => item.id === updated.id ? updated : item))
      setNotice(`${updated.fullName} is now assigned as ${roleLabel(role)}.`)
    } catch (requestError) {
      setError(apiErrorMessage(requestError))
    } finally {
      setSavingUserId(null)
    }
  }

  return (
    <main className="dashboard-content admin-dashboard">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Administration</span>
          <h1>User access control</h1>
          <p className="lead">Assign operational responsibilities while keeping administrator access protected.</p>
        </div>
        <span className="security-note">Protected by Administrator policy</span>
      </div>

      <section className="admin-metrics" aria-label="User summary">
        <article><span className="metric-icon blue">U</span><div><small>Total users</small><strong>{metrics.total}</strong></div></article>
        <article><span className="metric-icon amber">S</span><div><small>Site engineers</small><strong>{metrics.siteEngineers}</strong></div></article>
        <article><span className="metric-icon violet">P</span><div><small>Privileged roles</small><strong>{metrics.privileged}</strong></div></article>
        <article><span className="metric-icon green">A</span><div><small>Active accounts</small><strong>{metrics.active}</strong></div></article>
      </section>

      <section className="admin-panel">
        <div className="panel-heading">
          <div><h2>Users and roles</h2><p>Role changes take effect when the user’s session refreshes.</p></div>
          <label className="search-field"><span className="sr-only">Search users</span><input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, email or role" /></label>
        </div>

        {error && <div className="form-alert" role="alert">{error}</div>}
        {notice && <div className="success-alert" role="status">{notice}</div>}
        {isLoading ? <div className="table-state" role="status"><span className="spinner" />Loading users…</div> : (
          <div className="table-scroll">
            <table className="users-table">
              <thead><tr><th>User</th><th>Status</th><th>Current role</th><th>Assign role</th><th><span className="sr-only">Action</span></th></tr></thead>
              <tbody>
                {visibleUsers.map((user) => {
                  const isAdministrator = user.roles.includes('Administrator')
                  const currentRole = user.roles[0]
                  return <tr key={user.id}>
                    <td><div className="table-user"><span className="table-avatar">{user.fullName.charAt(0).toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.email}</small></span></div></td>
                    <td><span className={`account-status ${user.isActive ? 'active' : 'inactive'}`}>{user.isActive ? 'Active' : 'Inactive'}</span></td>
                    <td><span className="role-chip">{roleLabel(currentRole)}</span></td>
                    <td>{isAdministrator ? <span className="locked-role">Protected account</span> : <select aria-label={`Role for ${user.fullName}`} value={draftRoles[user.id] ?? currentRole} onChange={(event) => setDraftRoles((current) => ({ ...current, [user.id]: event.target.value }))}>{roleOptions.map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select>}</td>
                    <td>{!isAdministrator && <button className="button button-secondary button-small" type="button" disabled={savingUserId === user.id || draftRoles[user.id] === currentRole} onClick={() => assignRole(user)}>{savingUserId === user.id ? 'Saving…' : 'Save role'}</button>}</td>
                  </tr>
                })}
              </tbody>
            </table>
            {!visibleUsers.length && <div className="table-state">No users match your search.</div>}
          </div>
        )}
      </section>
    </main>
  )
}
