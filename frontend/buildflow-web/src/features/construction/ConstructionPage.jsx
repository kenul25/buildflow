import React, { useCallback, useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth.js'
import { apiErrorMessage } from '../../services/api.js'
import { constructionService, kinds } from './constructionService.js'
import './construction.css'

const fields = {
  projects: [['code', 'Project code', 'text', true], ['status', 'Status', 'select', false, ['Planned', 'Active', 'OnHold', 'Completed']], ['startDate', 'Start date', 'date'], ['endDate', 'End date', 'date']],
  sites: [['address', 'Site address', 'text', true]],
  phases: [['sequence', 'Sequence', 'number', true], ['startDate', 'Start date', 'date'], ['endDate', 'End date', 'date']],
  activities: [['status', 'Status', 'select', false, ['Planned', 'InProgress', 'OnHold', 'Completed']], ['dueDate', 'Due date', 'date']],
}

function useKind() {
  const { kind } = useParams()
  return kinds[kind] ? kind : 'projects'
}

export function ConstructionListPage() {
  const kind = useKind()
  const [params] = useSearchParams()
  const parentId = params.get('parentId')
  const { user } = useAuth()
  const canEdit = user.roles.some((role) => ['Administrator', 'ProjectManager'].includes(role))
  const [items, setItems] = useState([])
  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [sort, setSort] = useState('createdAt')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const load = useCallback(async () => {
    setLoading(true); setError('')
    try {
      const result = await constructionService.list(kind, { search, status: status || null, page, pageSize: 10, sort, parentId })
      setItems(result.items); setTotal(result.total)
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setLoading(false) }
  }, [kind, search, status, page, sort, parentId])
  useEffect(() => { queueMicrotask(load) }, [load])

  async function archive(item) {
    if (!window.confirm(`Archive ${item.name}? This record will leave active lists.`)) return
    try { await constructionService.archive(kind, item.id); setSuccess(`${item.name} archived.`); load() }
    catch (cause) { setError(apiErrorMessage(cause)) }
  }

  return <main className="dashboard-content construction-page">
    <div className="construction-heading"><div><span className="eyebrow">Member 01 · Site management</span><h1>{kinds[kind].title}</h1><p>Manage the construction hierarchy and keep site work organized.</p></div>{canEdit && <Link className="button button-primary" to={`/construction/${kind}/new${parentId ? `?parentId=${parentId}` : ''}`}>Create {kind.slice(0, -1)}</Link>}</div>
    <nav className="construction-tabs" aria-label="Construction sections">{Object.entries(kinds).map(([key, value]) => <Link key={key} className={key === kind ? 'active' : ''} to={`/construction/${key}`}>{value.title}</Link>)}</nav>
    {success && <p role="status" className="success-alert">{success}</p>}
    <section className="construction-panel"><div className="construction-toolbar"><label>Search <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder={`Search ${kind}`} /></label>{['projects', 'activities'].includes(kind) && <label>Status <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}><option value="">All statuses</option>{(kind === 'projects' ? ['Planned', 'Active', 'OnHold', 'Completed'] : ['Planned', 'InProgress', 'OnHold', 'Completed']).map((item) => <option key={item}>{item}</option>)}</select></label>}<label>Sort <select value={sort} onChange={(event) => setSort(event.target.value)}><option value="createdAt">Newest</option><option value="name">Name</option><option value="updatedAt">Recently updated</option></select></label></div>
      {error && <div className="form-alert" role="alert">{error} <button onClick={load}>Retry</button></div>}
      {loading ? <div className="table-state"><span className="spinner" /> Loading {kind}…</div> : items.length === 0 ? <div className="table-state">No {kind} found. Try another search or create one.</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Name</th><th>Context</th><th>Updated</th><th>Actions</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.name}</strong><small className="construction-subtext">{item.description || 'No description'}</small></td><td>{item.code || item.address || item.status || `Sequence ${item.sequence ?? '—'}`}</td><td>{new Date(item.updatedAt).toLocaleDateString()}</td><td className="row-actions"><Link className="table-action" to={`/construction/${kind}/${item.id}`}>Details</Link>{canEdit && <><Link className="table-action" to={`/construction/${kind}/${item.id}/edit`}>Edit</Link><button className="table-action danger" onClick={() => archive(item)}>Archive</button></>}</td></tr>)}</tbody></table></div>}
      <div className="construction-pagination"><span>{total} total</span><div><button disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button><span>Page {page}</span><button disabled={page * 10 >= total} onClick={() => setPage(page + 1)}>Next</button></div></div>
    </section>
  </main>
}

export function ConstructionDetailsPage() {
  const kind = useKind(); const { id } = useParams(); const { user } = useAuth()
  const location = useLocation()
  const canEdit = user.roles.some((role) => ['Administrator', 'ProjectManager'].includes(role))
  const [item, setItem] = useState(null); const [error, setError] = useState('')
  useEffect(() => { constructionService.get(kind, id).then(setItem).catch((cause) => setError(apiErrorMessage(cause))) }, [kind, id])
  const child = { projects: 'sites', sites: 'phases', phases: 'activities' }[kind]
  return <main className="dashboard-content construction-page"><Link to={`/construction/${kind}`}>← Back to {kinds[kind].title}</Link>{location.state?.success && <p role="status" className="success-alert">{location.state.success}</p>}{error && <p role="alert" className="form-alert">{error}</p>}{!item && !error && <div className="table-state"><span className="spinner" /> Loading details…</div>}{item && <><div className="construction-heading"><div><span className="eyebrow">{kinds[kind].title} / Details</span><h1>{item.name}</h1><p>{item.description || 'No description provided.'}</p></div>{canEdit && <Link className="button button-secondary" to={`/construction/${kind}/${id}/edit`}>Edit details</Link>}</div><section className="construction-panel construction-details">{Object.entries(item).filter(([key, value]) => value != null && !['id', 'description', 'name'].includes(key)).map(([key, value]) => <div key={key}><small>{key.replace(/([A-Z])/g, ' $1')}</small><strong>{String(value)}</strong></div>)}</section>{child && <section className="construction-panel"><h2>Related {kinds[child].title.toLowerCase()}</h2><p>Browse child records in the {kinds[child].title} section.</p><Link className="button button-secondary" to={`/construction/${child}?parentId=${id}`}>View {kinds[child].title}</Link></section>}</>}</main>
}

export function ConstructionFormPage() {
  const kind = useKind(); const { id } = useParams(); const navigate = useNavigate()
  const [params] = useSearchParams()
  const [form, setForm] = useState({ name: '', description: '', code: '', status: 'Planned', parentId: '', address: '', sequence: 0, startDate: '', endDate: '', dueDate: '', assignedEngineerId: '' })
  const [parents, setParents] = useState([]); const [loading, setLoading] = useState(Boolean(id)); const [saving, setSaving] = useState(false); const [error, setError] = useState('')
  const [engineers, setEngineers] = useState([])
  useEffect(() => { if (!id && params.get('parentId')) queueMicrotask(() => setForm((current) => ({ ...current, parentId: params.get('parentId') }))) }, [id, params])
  useEffect(() => {
    if (id) constructionService.get(kind, id).then((data) => setForm(Object.fromEntries(Object.entries(form).map(([key]) => [key, data[key] ?? ''])))).catch((cause) => setError(apiErrorMessage(cause))).finally(() => setLoading(false))
    if (kinds[kind].parent) constructionService.list(kinds[kind].parent, { pageSize: 100 }).then((data) => setParents(data.items)).catch((cause) => setError(apiErrorMessage(cause)))
    if (kind === 'projects') constructionService.engineers().then(setEngineers).catch((cause) => setError(apiErrorMessage(cause)))
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [kind, id])
  async function submit(event) {
    event.preventDefault(); setSaving(true); setError('')
    try {
      const body = { ...form, parentId: form.parentId || null, assignedEngineerId: form.assignedEngineerId || null, startDate: form.startDate || null, endDate: form.endDate || null, dueDate: form.dueDate || null, sequence: Number(form.sequence) }
      const result = id ? await constructionService.update(kind, id, body) : await constructionService.create(kind, body)
      navigate(`/construction/${kind}/${result.id}`, { state: { success: `${result.name} ${id ? 'updated' : 'created'} successfully.` } })
    } catch (cause) { setError(apiErrorMessage(cause)) } finally { setSaving(false) }
  }
  const set = (key, value) => setForm((current) => ({ ...current, [key]: value }))
  return <main className="dashboard-content construction-page"><Link to={`/construction/${kind}`}>← Back to {kinds[kind].title}</Link><div className="construction-heading"><div><span className="eyebrow">{kinds[kind].title}</span><h1>{id ? 'Edit' : 'Create'} {kind.slice(0, -1)}</h1></div></div>{loading ? <div className="table-state"><span className="spinner" /> Loading…</div> : <form className="construction-panel construction-form" onSubmit={submit}>{error && <p className="form-alert" role="alert">{error}</p>}<label>Name<input required minLength={2} maxLength={160} value={form.name} onChange={(e) => set('name', e.target.value)} /></label><label>Description<textarea maxLength={2000} value={form.description || ''} onChange={(e) => set('description', e.target.value)} /></label>{kinds[kind].parent && <label>Parent {kinds[kinds[kind].parent].title}<select required value={form.parentId} onChange={(e) => set('parentId', e.target.value)}><option value="">Select parent</option>{parents.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>}{fields[kind].map(([key, label, type, required, options]) => <label key={key}>{label}{type === 'select' ? <select value={form[key] || ''} onChange={(e) => set(key, e.target.value)}>{options.map((option) => <option key={option}>{option}</option>)}</select> : <input type={type} required={required} value={form[key] || ''} onChange={(e) => set(key, e.target.value)} />}</label>)}{kind === 'projects' && <label>Assigned site engineer<select value={form.assignedEngineerId || ''} onChange={(e) => set('assignedEngineerId', e.target.value)}><option value="">Unassigned</option>{engineers.map((engineer) => <option value={engineer.id} key={engineer.id}>{engineer.fullName}</option>)}</select></label>}<div className="modal-actions"><Link className="button button-secondary" to={`/construction/${kind}`}>Cancel</Link><button className="button button-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button></div></form>}</main>
}
