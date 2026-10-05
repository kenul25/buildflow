import React, { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import { useAuth } from '../../hooks/useAuth.js'

export default function ActivityOperations({ activity, canManage }) {
  const { user } = useAuth()
  const allowed = canManage || user.roles.includes('SiteEngineer')
  const [history, setHistory] = useState([])
  const [photos, setPhotos] = useState([])
  const [requests, setRequests] = useState([])
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const [progress, setProgress] = useState({ progressPercent: activity.progressPercent ?? 0, workCompleted: '', blockers: '' })
  const [correction, setCorrection] = useState(false)
  const [request, setRequest] = useState({ objective: '', requiredBy: '', budgetLimit: '', notes: '', items: [{ kind: 'Material', name: '', quantity: 1, unit: '' }] })
  const [editing, setEditing] = useState(null)
  const [hierarchy, setHierarchy] = useState(null)
  const load = useCallback(async () => {
    if (!allowed) return
    try {
      const [progressData, photoData, requestData, phase] = await Promise.all([api.get(`/construction/activities/${activity.id}/progress`), api.get(`/construction/activities/${activity.id}/photos`), api.get('/construction/resource-requests'), api.get(`/phases/${activity.parentId}`)])
      const site = (await api.get(`/sites/${phase.data.parentId}`)).data
      setHierarchy({ projectId: site.parentId, siteId: site.id, activityId: activity.id })
      setHistory(progressData.data); setPhotos(photoData.data); setRequests(requestData.data.filter(x => x.activityId === activity.id))
    } catch (e) { setError(apiErrorMessage(e)) }
  }, [activity.id, activity.parentId, allowed])
  useEffect(() => { queueMicrotask(load) }, [load])
  async function perform(action) {
    if (busy) return; setBusy(true); setError('')
    try { await action(); setMessage('Saved successfully.'); await load() }
    catch (e) { setError(apiErrorMessage(e)) }
    finally { setBusy(false) }
  }
  if (!allowed) return null
  return <>
    {error && <p role="alert" className="form-alert">{error}</p>}{message && <p role="status" className="success-alert">{message}</p>}
    <section className="construction-panel"><h2>Progress & history</h2><form className="construction-form" onSubmit={e => { e.preventDefault(); perform(() => api.post(`/construction/activities/${activity.id}/progress${correction ? '/corrections' : ''}`, { ...progress, progressPercent: Number(progress.progressPercent) })) }}><label>Progress (%)<input required type="number" min="0" max="100" value={progress.progressPercent} onChange={e => setProgress({ ...progress, progressPercent: e.target.value })} /></label><label>{correction ? 'Correction reason' : 'Work completed'}<textarea required minLength={2} maxLength={1900} value={progress.workCompleted} onChange={e => setProgress({ ...progress, workCompleted: e.target.value })} /></label><label>Blockers<textarea value={progress.blockers} onChange={e => setProgress({ ...progress, blockers: e.target.value })} /></label>{canManage && <label><input type="checkbox" checked={correction} onChange={e => setCorrection(e.target.checked)} />Record an audited correction</label>}<button disabled={busy} className="button button-primary">Save progress</button></form>{history.map(row => <p key={row.id}>{row.progressPercent}% · {row.workCompleted} · {new Date(row.createdAt).toLocaleString()}{row.blockers && ` · ${row.blockers}`}</p>)}</section>
    <section className="construction-panel"><h2>Site photos</h2><label>Upload photo<input disabled={busy} type="file" accept="image/png,image/jpeg,image/webp" onChange={e => { const file = e.target.files[0]; if (file) perform(() => { const body = new FormData(); body.append('file', file); return api.post(`/construction/activities/${activity.id}/photos`, body) }) }} /></label>{photos.map(photo => <Photo key={photo.id} photo={photo} disabled={busy} onCaption={caption => perform(() => api.put(`/construction/photos/${photo.id}`, { caption }))} onDelete={() => { if (globalThis.confirm('Archive this photo?')) perform(() => api.delete(`/construction/photos/${photo.id}`)) }} />)}</section>
    <section className="construction-panel"><h2>{editing ? 'Edit resource request' : 'Resource request'}</h2><form className="construction-form" onSubmit={e => { e.preventDefault(); perform(async () => { const body = { ...hierarchy, ...request, requiredBy: request.requiredBy || null, budgetLimit: request.budgetLimit === '' ? null : Number(request.budgetLimit), items: request.items.map(x => ({ ...x, quantity: Number(x.quantity) })) }; await api[editing ? 'put' : 'post'](`/construction/resource-requests${editing ? '/' + editing : ''}`, body); setEditing(null); setRequest({ objective: '', requiredBy: '', budgetLimit: '', notes: '', items: [{ kind: 'Material', name: '', quantity: 1, unit: '' }] }) }) }}><label>Objective<textarea required minLength={10} maxLength={2000} value={request.objective} onChange={e => setRequest({ ...request, objective: e.target.value })} /></label><label>Required by<input type="date" value={request.requiredBy ?? ''} onChange={e => setRequest({ ...request, requiredBy: e.target.value })} /></label><label>Budget limit<input type="number" min="0" step="0.01" value={request.budgetLimit ?? ''} onChange={e => setRequest({ ...request, budgetLimit: e.target.value })} /></label>{request.items.map((item, index) => <fieldset key={index}><legend>Resource {index + 1}</legend><label>Type<select value={item.kind} onChange={e => setRequest({ ...request, items: request.items.map((x, i) => i === index ? { ...x, kind: e.target.value } : x) })}>{['Material', 'Equipment', 'Workforce'].map(x => <option key={x}>{x}</option>)}</select></label>{[['name', 'Material name / equipment category / worker skill', 'text'], ['quantity', 'Quantity', 'number'], ['unit', 'Unit', 'text']].map(([key, label, type]) => <label key={key}>{label}<input required type={type} min={type === 'number' ? '0.001' : undefined} step={type === 'number' ? '0.001' : undefined} value={item[key]} onChange={e => setRequest({ ...request, items: request.items.map((x, i) => i === index ? { ...x, [key]: e.target.value } : x) })} /></label>)}{request.items.length > 1 && <button type="button" onClick={() => setRequest({ ...request, items: request.items.filter((_, i) => i !== index) })}>Remove</button>}</fieldset>)}<button type="button" disabled={request.items.length >= 50} onClick={() => setRequest({ ...request, items: [...request.items, { kind: 'Material', name: '', quantity: 1, unit: '' }] })}>Add resource</button><button disabled={busy || !hierarchy} className="button button-primary">Save request</button></form>
      {requests.map(row => <article key={row.id}><h3>{row.objective}</h3><p>{row.workflowStatus || 'Draft'}</p>{!row.workflowId && <button className="table-action" onClick={() => perform(async () => { const result = (await api.get(`/construction/resource-requests/${row.id}`)).data; setRequest(result); setEditing(row.id) })}>Edit request</button>}{(!row.workflowId || ['Failed', 'Rejected', 'RevisionRequested'].includes(row.workflowStatus)) && <button className="table-action danger" onClick={() => { if (globalThis.confirm('Cancel this request?')) perform(() => api.delete(`/construction/resource-requests/${row.id}`)) }}>Cancel</button>}<button disabled={busy} className="table-action" onClick={() => perform(async () => { const result = (await api.post(`/construction/resource-requests/${row.id}/planning`, null, { timeout: 150000 })).data; if (result.status === 'AwaitingAgents') await api.post(`/workflows/${result.id}/execute`, null, { timeout: 150000 }) })}>Analyze / view plan</button><a href="/workflows">Review plans</a></article>)}
    </section>
  </>
}
function Photo({ photo, disabled, onCaption, onDelete }) {
  const [src, setSrc] = useState('')
  const [caption, setCaption] = useState(photo.caption ?? '')
  useEffect(() => {
    let url, active = true
    api.get(`/construction/photos/${photo.id}`, { responseType: 'blob' }).then(response => { if (active) { url = URL.createObjectURL(response.data); setSrc(url) } }).catch(() => {})
    return () => { active = false; if (url) URL.revokeObjectURL(url) }
  }, [photo.id])
  return <article>{src && <img src={src} alt={photo.caption || 'Site progress evidence'} style={{ maxWidth: '100%', maxHeight: 240 }} />}<label>Caption<input maxLength={2000} value={caption} onChange={e => setCaption(e.target.value)} /></label><button disabled={disabled} className="table-action" onClick={() => onCaption(caption)}>Save caption</button><button disabled={disabled} className="table-action danger" onClick={onDelete}>Archive photo</button></article>
}
