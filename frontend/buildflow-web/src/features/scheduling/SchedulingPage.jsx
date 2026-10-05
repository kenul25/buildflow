import React, { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import { allRows } from '../../services/lookups.js'
import { useAuth } from '../../hooks/useAuth.js'
import '../construction/construction.css'
import '../inventory/inventory.css'

const sections = {
  workers: { title: 'Workers', fields: [['phone', 'Phone', 'text', false], ['isActive', 'Active', 'checkbox']] },
  skills: { title: 'Skills', fields: [] },
  'worker-skills': { title: 'Worker skills', fields: [['workerId', 'Worker', 'workers'], ['skillId', 'Skill', 'skills']] },
  shifts: { title: 'Shifts', fields: [['workerId', 'Worker', 'workers'], ['startTime', 'Start', 'datetime-local'], ['endTime', 'End', 'datetime-local']] },
  equipment: { title: 'Equipment', fields: [['code', 'Equipment / QR code', 'text'], ['category', 'Category', 'text'], ['status', 'Status', ['Operational', 'Maintenance', 'OutOfService']]] },
  schedules: { title: 'Schedules', fields: [['activityId', 'Activity', 'activities'], ['dependencyId', 'Predecessor schedule (optional)', 'schedules', false], ['startTime', 'Start', 'datetime-local'], ['endTime', 'End', 'datetime-local'], ['status', 'Status', ['Draft', 'Approved', 'InProgress', 'Completed']]] },
  'worker-assignments': { title: 'Worker assignments', fields: [['workerId', 'Worker', 'workers'], ['scheduleId', 'Schedule', 'schedules'], ['requiredSkillId', 'Required skill (optional)', 'skills', false], ['startTime', 'Start', 'datetime-local'], ['endTime', 'End', 'datetime-local'], ['status', 'Status', ['Upcoming', 'InProgress', 'Completed', 'Delayed']]] },
  'equipment-reservations': { title: 'Equipment reservations', fields: [['equipmentId', 'Equipment', 'equipment'], ['scheduleId', 'Schedule', 'schedules'], ['startTime', 'Start', 'datetime-local'], ['endTime', 'End', 'datetime-local'], ['status', 'Status', ['Reserved', 'Received', 'Returned']]] },
  'equipment-requests': { title: 'Equipment requests', fields: [['activityId', 'Activity', 'activities'], ['equipmentId', 'Equipment', 'equipment']] },
  issues: { title: 'Site issues', fields: [['activityId', 'Activity', 'activities'], ['equipmentId', 'Equipment (optional)', 'equipment', false], ['severity', 'Severity', ['Normal', 'High', 'Critical']], ['status', 'Status', ['Open', 'Resolved']]] },
}
const blank = { name: '', notes: '', phone: '', code: '', category: '', isActive: true }
const dateInput = (value) => {
  const d = new Date(value)
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}

export default function SchedulingPage() {
  const { user } = useAuth()
  const canManage = user.roles.some(r => ['Administrator', 'ProjectManager'].includes(r))
  const [kind, setKind] = useState(canManage ? 'workers' : 'worker-assignments')
  const [data, setData] = useState({ items: [], total: 0 })
  const [lookups, setLookups] = useState({})
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [sort, setSort] = useState('name')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [form, setForm] = useState(null)
  const [detail, setDetail] = useState(null)
  const [saving, setSaving] = useState(false)
  const [window, setWindow] = useState({ startTime: '', endTime: '', skillId: '' })
  const [availability, setAvailability] = useState(null)
  const editable = canManage || ['equipment-requests', 'issues'].includes(kind)

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try {
      setData((await api.get(`/scheduling/${kind}`, { params: { search, status: status || undefined, page, pageSize: 10, sort } })).data)
      const keys = ['workers', 'skills', 'equipment', 'schedules', 'activities']
      const result = await Promise.all(keys.map(k => allRows(k === 'activities' ? '/activities' : `/scheduling/${k}`)))
      setLookups(Object.fromEntries(keys.map((k, i) => [k, result[i]])))
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setLoading(false) }
  }, [kind, search, status, page, sort])
  useEffect(() => { queueMicrotask(load) }, [load])
  const label = (key, value) => {
    const source = { workerId: 'workers', skillId: 'skills', requiredSkillId: 'skills', equipmentId: 'equipment', activityId: 'activities', scheduleId: 'schedules', dependencyId: 'schedules' }[key]
    return source ? lookups[source]?.find(x => x.id === value)?.name ?? 'Archived record' : key.endsWith('Time') ? new Date(value).toLocaleString() : String(value)
  }
  const openForm = (row = null) => {
    const value = { ...blank, ...row }
    for (const [key, , type] of sections[kind].fields) {
      if (Array.isArray(type) && !value[key]) value[key] = type[0]
      if (type === 'datetime-local' && value[key]) value[key] = dateInput(value[key])
    }
    setForm(value); setError('')
  }
  async function save(event) {
    event.preventDefault(); if (saving) return
    setSaving(true); setError('')
    try {
      const body = { ...form }
      for (const [key, , type] of sections[kind].fields) {
        if (type === 'datetime-local') body[key] = new Date(body[key]).toISOString()
        else if (key.endsWith('Id')) body[key] = body[key] || null
      }
      await api[form.id ? 'put' : 'post'](`/scheduling/${kind}${form.id ? `/${form.id}` : ''}`, body)
      setForm(null); setMessage('Record saved.'); await load()
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setSaving(false) }
  }
  async function archive(row) {
    if (!globalThis.confirm(`Archive or cancel ${row.name}? History will be preserved.`)) return
    try { await api.delete(`/scheduling/${kind}/${row.id}`); setMessage('Record archived.'); await load() }
    catch (cause) { setError(apiErrorMessage(cause)) }
  }
  async function inspect(row) {
    try { setDetail((await api.get(`/scheduling/${kind}/${row.id}`)).data) }
    catch (cause) { setError(apiErrorMessage(cause)) }
  }
  async function checkAvailability(event) {
    event.preventDefault()
    try { setAvailability((await api.get('/scheduling/availability', { params: { startTime: new Date(window.startTime).toISOString(), endTime: new Date(window.endTime).toISOString(), skillId: window.skillId || undefined } })).data) }
    catch (cause) { setError(apiErrorMessage(cause)) }
  }
  return <main className="dashboard-content construction-page">
    <div className="page-heading"><div><span className="eyebrow">Workforce & equipment</span><h1>Scheduling</h1><p>Manage qualified workers, shifts, equipment and site schedules.</p></div>{editable && <button className="button button-primary" onClick={() => openForm()}>Add record</button>}</div>
    <nav className="inventory-tabs" aria-label="Scheduling sections">{Object.entries(sections).filter(([key]) => canManage || ['schedules', 'worker-assignments', 'equipment', 'equipment-reservations', 'equipment-requests', 'issues'].includes(key)).map(([key, config]) => <button key={key} className={kind === key ? 'active' : ''} onClick={() => { setKind(key); setPage(1); setStatus(''); setMessage('') }}>{config.title}</button>)}</nav>
    {error && <p className="form-alert" role="alert">{error}</p>}{message && <p role="status" className="success-alert">{message}</p>}
    <section className="construction-panel"><h2>{sections[kind].title}</h2><div className="construction-toolbar"><label>Search<input value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /></label><label>Status<input value={status} onChange={e => { setStatus(e.target.value); setPage(1) }} placeholder="All statuses" /></label><label>Sort<select value={sort} onChange={e => setSort(e.target.value)}><option value="name">Name</option><option value="updatedAt">Updated</option><option value="startTime">Start time</option></select></label></div>
      {loading ? <p>Loading…</p> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Name</th><th>Details</th><th>Status</th><th>Actions</th></tr></thead><tbody>{data.items.map(row => <tr key={row.id}><td>{row.name}</td><td>{sections[kind].fields.filter(([key]) => row[key] != null && !['status', 'isActive'].includes(key)).map(([key, name]) => <small className="construction-subtext" key={key}>{name}: {label(key, row[key])}</small>)}</td><td>{row.status ?? (row.isActive === false ? 'Inactive' : 'Active')}</td><td className="row-actions"><button className="table-action" onClick={() => inspect(row)}>Details</button>{editable && <><button className="table-action" onClick={() => openForm(row)}>Edit</button><button className="table-action danger" onClick={() => archive(row)}>Archive / cancel</button></>}</td></tr>)}</tbody></table>{data.items.length === 0 && <div className="table-state">No records found.</div>}</div>}
      <div className="construction-pagination"><span>{data.total} records</span><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button><span>Page {page}</span><button disabled={page * 10 >= data.total} onClick={() => setPage(page + 1)}>Next</button></div>
    </section>
    <section className="construction-panel"><h2>Availability & conflicts</h2><form className="construction-toolbar" onSubmit={checkAvailability}><label>Start<input required type="datetime-local" value={window.startTime} onChange={e => setWindow({ ...window, startTime: e.target.value })} /></label><label>End<input required type="datetime-local" value={window.endTime} onChange={e => setWindow({ ...window, endTime: e.target.value })} /></label><label>Skill<select value={window.skillId} onChange={e => setWindow({ ...window, skillId: e.target.value })}><option value="">All skills</option>{lookups.skills?.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label><button className="button button-secondary">Check availability</button></form>{availability && <p role="status">Available workers: {availability.workers.map(x => x.name).join(', ') || 'None'}. Equipment: {availability.equipment.map(x => x.name).join(', ') || 'None'}.</p>}</section>
    {form && <div className="modal-backdrop"><section className="user-modal" role="dialog" aria-modal="true" aria-label={form.id ? 'Edit scheduling record' : 'Create scheduling record'}><div className="modal-heading"><div><span className="eyebrow">Workforce & equipment</span><h2>{form.id ? 'Edit' : 'Create'} {sections[kind].title.toLowerCase()}</h2></div><button type="button" className="modal-close" disabled={saving} onClick={() => setForm(null)} aria-label="Close scheduling form">×</button></div><form className="inventory-form" onSubmit={save}><label>Name<input required minLength={2} maxLength={160} value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></label>{sections[kind].fields.map(([key, name, type, required = true]) => <label key={key}>{name}{Array.isArray(type) ? <select value={form[key] ?? type[0]} onChange={e => setForm({ ...form, [key]: e.target.value })}>{type.map(x => <option key={x}>{x}</option>)}</select> : lookups[type] ? <select required={required} value={form[key] ?? ''} onChange={e => setForm({ ...form, [key]: e.target.value })}><option value="">Select {name.toLowerCase()}</option>{lookups[type].filter(x => x.id !== form.id).map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select> : <input type={type} required={type !== 'checkbox' && required} checked={type === 'checkbox' ? Boolean(form[key]) : undefined} value={type === 'checkbox' ? undefined : form[key] ?? ''} onChange={e => setForm({ ...form, [key]: type === 'checkbox' ? e.target.checked : e.target.value })} />}</label>)}<label>Notes<textarea maxLength={2000} value={form.notes ?? ''} onChange={e => setForm({ ...form, notes: e.target.value })} /></label>{error && <p role="alert">{error}</p>}<div className="modal-actions"><button type="button" className="button button-secondary" disabled={saving} onClick={() => setForm(null)}>Cancel</button><button className="button button-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button></div></form></section></div>}
    {detail && <div className="modal-backdrop"><section className="user-modal" role="dialog" aria-modal="true" aria-label="Record details"><div className="modal-heading"><h2>{detail.name}</h2><button type="button" className="modal-close" onClick={() => setDetail(null)} aria-label="Close details">×</button></div><div className="modal-body"><p>{detail.notes || 'No notes'}</p>{sections[kind].fields.map(([key, name]) => detail[key] != null && <p key={key}><strong>{name}:</strong> {label(key, detail[key])}</p>)}<p>Updated: {new Date(detail.updatedAt).toLocaleString()}</p><button className="button button-secondary" onClick={() => setDetail(null)}>Close</button></div></section></div>}
  </main>
}
