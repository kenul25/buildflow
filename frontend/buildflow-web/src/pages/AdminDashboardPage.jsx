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
const blankUser = { mode: 'create', fullName: '', email: '', password: '', role: 'SiteEngineer', isActive: true }

export default function AdminDashboardPage() {
  const [users, setUsers] = useState([])
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [editor, setEditor] = useState(null)
  const [editorError, setEditorError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deletingUserId, setDeletingUserId] = useState(null)

  useEffect(() => {
    adminService.getUsers()
      .then(setUsers)
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

  const openEditor = (user) => {
    setError('')
    setNotice('')
    setEditorError('')
    setEditor(user ? {
      mode: 'edit',
      id: user.id,
      fullName: user.fullName,
      email: user.email,
      password: '',
      role: user.roles[0] ?? 'SiteEngineer',
      isActive: user.isActive,
    } : { ...blankUser })
  }

  const saveUser = async (event) => {
    event.preventDefault()
    setIsSubmitting(true)
    setEditorError('')
    const payload = {
      fullName: editor.fullName.trim(),
      email: editor.email.trim(),
      role: editor.role,
      isActive: editor.isActive,
      ...(editor.mode === 'create' || editor.password ? { password: editor.password } : {}),
    }
    try {
      const saved = editor.mode === 'create'
        ? await adminService.createUser(payload)
        : await adminService.updateUser(editor.id, payload)
      setUsers((current) => editor.mode === 'create'
        ? [...current, saved].sort((left, right) => left.fullName.localeCompare(right.fullName))
        : current.map((user) => user.id === saved.id ? saved : user))
      setNotice(editor.mode === 'create'
        ? `${saved.fullName} was added successfully.`
        : `${saved.fullName} was updated successfully.`)
      setEditor(null)
    } catch (requestError) {
      setEditorError(apiErrorMessage(requestError))
    } finally {
      setIsSubmitting(false)
    }
  }

  const deleteUser = async (user) => {
    if (!window.confirm(`Delete ${user.fullName}? This permanently removes the account.`)) return
    setError('')
    setNotice('')
    setDeletingUserId(user.id)
    try {
      await adminService.deleteUser(user.id)
      setUsers((current) => current.filter((item) => item.id !== user.id))
      setNotice(`${user.fullName} was deleted.`)
    } catch (requestError) {
      setError(apiErrorMessage(requestError))
    } finally {
      setDeletingUserId(null)
    }
  }

  return (
    <main className="dashboard-content admin-dashboard">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Administration</span>
          <h1>User access control</h1>
          <p className="lead">Create accounts and manage operational access while keeping administrator accounts protected.</p>
        </div>
        <div className="heading-actions"><span className="security-note">Administrator protected</span><button className="button button-primary" type="button" onClick={() => openEditor()}>Add new user</button></div>
      </div>

      <section className="admin-metrics" aria-label="User summary">
        <article><span className="metric-icon blue">U</span><div><small>Total users</small><strong>{metrics.total}</strong></div></article>
        <article><span className="metric-icon amber">S</span><div><small>Site engineers</small><strong>{metrics.siteEngineers}</strong></div></article>
        <article><span className="metric-icon violet">P</span><div><small>Privileged roles</small><strong>{metrics.privileged}</strong></div></article>
        <article><span className="metric-icon green">A</span><div><small>Active accounts</small><strong>{metrics.active}</strong></div></article>
      </section>

      <section className="admin-panel">
        <div className="panel-heading">
          <div><h2>Users and roles</h2><p>Edit account details, reset passwords, change roles or remove accounts.</p></div>
          <label className="search-field"><span className="sr-only">Search users</span><input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, email or role" /></label>
        </div>

        {error && <div className="form-alert" role="alert">{error}</div>}
        {notice && <div className="success-alert" role="status">{notice}</div>}
        {isLoading ? <div className="table-state" role="status"><span className="spinner" />Loading users…</div> : (
          <div className="table-scroll">
            <table className="users-table">
              <thead><tr><th>User</th><th>Status</th><th>Role</th><th>Created</th><th><span className="sr-only">Actions</span></th></tr></thead>
              <tbody>
                {visibleUsers.map((user) => {
                  const isAdministrator = user.roles.includes('Administrator')
                  return <tr key={user.id}>
                    <td><div className="table-user"><span className="table-avatar">{user.fullName.charAt(0).toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.email}</small></span></div></td>
                    <td><span className={`account-status ${user.isActive ? 'active' : 'inactive'}`}>{user.isActive ? 'Active' : 'Inactive'}</span></td>
                    <td><span className="role-chip">{roleLabel(user.roles[0])}</span></td>
                    <td><span className="created-date">{new Date(user.createdAt).toLocaleDateString()}</span></td>
                    <td>{isAdministrator ? <span className="locked-role">Protected account</span> : <div className="row-actions"><button className="table-action" type="button" onClick={() => openEditor(user)}>Edit</button><button className="table-action danger" type="button" disabled={deletingUserId === user.id} onClick={() => deleteUser(user)}>{deletingUserId === user.id ? 'Deleting…' : 'Delete'}</button></div>}</td>
                  </tr>
                })}
              </tbody>
            </table>
            {!visibleUsers.length && <div className="table-state">No users match your search.</div>}
          </div>
        )}
      </section>

      {editor && <UserEditor editor={editor} setEditor={setEditor} error={editorError} onSubmit={saveUser} onClose={() => setEditor(null)} isSubmitting={isSubmitting} />}
    </main>
  )
}

function UserEditor({ editor, setEditor, error, onSubmit, onClose, isSubmitting }) {
  const update = ({ target }) => setEditor((current) => ({
    ...current,
    [target.name]: target.type === 'checkbox' ? target.checked : target.value,
  }))
  const isCreating = editor.mode === 'create'
  return <div className="modal-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
    <section className="user-modal" role="dialog" aria-modal="true" aria-labelledby="user-editor-title">
      <div className="modal-heading"><div><span className="eyebrow">User management</span><h2 id="user-editor-title">{isCreating ? 'Add new user' : 'Edit user'}</h2></div><button className="modal-close" type="button" aria-label="Close" onClick={onClose}>×</button></div>
      <form className="user-form" onSubmit={onSubmit}>
        {error && <div className="form-alert" role="alert">{error}</div>}
        <label className="field"><span>Full name</span><input name="fullName" value={editor.fullName} onChange={update} minLength="2" maxLength="120" required /></label>
        <label className="field"><span>Email address</span><input name="email" type="email" value={editor.email} onChange={update} maxLength="254" required /></label>
        <label className="field"><span>{isCreating ? 'Temporary password' : 'New password (optional)'}</span><input name="password" type="password" value={editor.password} onChange={update} minLength="8" maxLength="128" required={isCreating} autoComplete="new-password" /><small>Use uppercase, lowercase, a number and a special character.</small></label>
        <label className="field"><span>Operational role</span><select name="role" value={editor.role} onChange={update}>{roleOptions.map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
        <label className="active-toggle"><input name="isActive" type="checkbox" checked={editor.isActive} onChange={update} /><span><strong>Active account</strong><small>Inactive users cannot sign in or refresh their session.</small></span></label>
        <div className="modal-actions"><button className="button button-secondary" type="button" onClick={onClose}>Cancel</button><button className="button button-primary" disabled={isSubmitting}>{isSubmitting ? 'Saving…' : isCreating ? 'Create user' : 'Save changes'}</button></div>
      </form>
    </section>
  </div>
}
