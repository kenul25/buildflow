import React, { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import { useAuth } from '../../hooks/useAuth.js'
import { allRows } from '../../services/lookups.js'
import './workflows.css'

const statusLabels = { PendingProjectManagerApproval: 'Awaiting approval', AwaitingAgents: 'Ready for analysis', RevisionRequested: 'Revision requested', AwaitingBackendValidation: 'Validating resources' }
const readable = value => statusLabels[value] ?? (value ?? 'Pending').replace(/([a-z])([A-Z])/g, '$1 $2')
const amount = value => Number(value ?? 0).toLocaleString()
const money = value => `LKR ${amount(value)}`
const dateTime = value => value && !Number.isNaN(new Date(value).getTime()) ? new Date(value).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' }) : 'Not scheduled'
const localDateTime = value => {
  const date = new Date(value)
  if (!value || Number.isNaN(date.getTime())) return ''
  const pad = number => String(number).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function StatusBadge({ status }) {
  const tone = ['Approved', 'Completed', 'Valid'].includes(status) ? 'success' : ['Failed', 'Rejected', 'Requires changes'].includes(status) ? 'danger' : ['PendingProjectManagerApproval', 'RevisionRequested'].includes(status) ? 'warning' : 'neutral'
  return <span className={`workflow-status workflow-status--${tone}`}><span aria-hidden="true" />{readable(status)}</span>
}

function ReviewCard({ title, children, className = '' }) {
  return <section className={`workflow-card ${className}`}><h3>{title}</h3>{children}</section>
}

export default function WorkflowsPage() {
  const { user } = useAuth()
  const manager = user.roles.some(x => ['Administrator', 'ProjectManager'].includes(x))
  const [rows, setRows] = useState([])
  const [detail, setDetail] = useState(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [reason, setReason] = useState('')
  const [request, setRequest] = useState(null)
  const [resources, setResources] = useState({ workers: [], equipment: [] })
  const [reviewing, setReviewing] = useState(null)
  const [scheduleStart, setScheduleStart] = useState('')
  const [now, setNow] = useState(Date.now)
  const load = useCallback(async () => { try { setRows((await api.get('/workflows')).data) } catch (e) { setError(apiErrorMessage(e)) } }, [])
  useEffect(() => { queueMicrotask(load) }, [load])
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 30000)
    return () => clearInterval(timer)
  }, [])
  async function review(id) {
    setReviewing(id)
    try {
      const workflow = (await api.get(`/workflows/${id}`)).data
      const [requestData, workers, equipment] = await Promise.all([api.get(`/construction/resource-requests/${workflow.resourceRequestId}`), allRows('/scheduling/workers'), allRows('/scheduling/equipment')])
      setDetail(workflow); setRequest(requestData.data); setResources({ workers, equipment }); setReason(''); setError('')
      setScheduleStart(localDateTime(workflow.plan?.tasks?.find(task => task.agent === 'SchedulingValidationAgent')?.output?.startTime))
    } catch (e) { setError(apiErrorMessage(e)) }
    finally { setReviewing(null) }
  }
  const resourceName = (kind, id) => resources[kind].find(x => x.id === id)?.name ?? 'Archived resource'
  async function run(path, body) {
    if (busy) return
    setBusy(true); setError('')
    try {
      const updated = (await api.post(path, body, { timeout: 150000 })).data
      setDetail(updated)
      setScheduleStart(localDateTime(updated.plan?.tasks?.find(task => task.agent === 'SchedulingValidationAgent')?.output?.startTime))
      await load()
    }
    catch (e) { setError(apiErrorMessage(e)) }
    finally { setBusy(false) }
  }
  const scheduling = detail?.plan?.tasks?.find(x => x.agent === 'SchedulingValidationAgent')?.output
  const procurement = detail?.plan?.tasks?.find(x => x.agent === 'ProcurementAgent')?.output
  const inventory = detail?.plan?.tasks?.find(x => x.agent === 'InventoryAgent')?.output
  const startChanged = scheduling && scheduleStart !== localDateTime(scheduling.startTime)
  const selectedStart = startChanged ? new Date(scheduleStart).getTime() : new Date(scheduling?.startTime).getTime()
  const invalidStart = scheduling && (!Number.isFinite(selectedStart) || selectedStart <= now)
  const scheduleEnd = scheduling && Number.isFinite(selectedStart) ? new Date(selectedStart + new Date(scheduling.endTime).getTime() - new Date(scheduling.startTime).getTime()).toISOString() : null
  return <main className="dashboard-content workflows-page">
    <header className="page-heading"><div><span className="eyebrow">Resource plans</span><h1>Plans & approvals</h1><p>Review material coverage, procurement and proposed schedules before committing resources.</p></div></header>
    {error && <p role="alert" className="form-alert">{error}</p>}
    <section className="admin-panel workflow-list" aria-label="Resource plans">
      <div className="panel-heading"><div><h2>Resource plans</h2><p>Select a plan to review its requirements and analysis.</p></div><span className="workflow-count">{rows.length} plans</span></div>
      <div className="table-scroll"><table className="users-table workflow-table"><thead><tr><th scope="col">Objective</th><th scope="col">Status</th><th scope="col">Action</th></tr></thead><tbody>{rows.map(row => <tr key={row.id} className={detail?.id === row.id ? 'workflow-row-selected' : ''}><td><span className="workflow-objective">{row.objective}</span></td><td><StatusBadge status={row.status} /></td><td><button disabled={busy || reviewing !== null} className="button button-secondary button-small" aria-pressed={detail?.id === row.id} onClick={() => review(row.id)}>{reviewing === row.id ? 'Opening…' : 'Review'}</button></td></tr>)}</tbody></table></div>
      {!rows.length && <p className="workflow-empty">No resource plans yet. Submit a resource request from a site activity.</p>}
    </section>
    {detail && <section className="admin-panel workflow-review" aria-labelledby="proposal-heading" aria-busy={busy}>
      <header className="workflow-review-heading"><div><span className="eyebrow">Plan review</span><h2 id="proposal-heading">Resource proposal</h2></div><StatusBadge status={detail.status} /></header>
      <div className="workflow-review-body">
        {request && <><p className="workflow-review-objective">{request.objective}</p><dl className="workflow-summary"><div><dt>Required by</dt><dd>{request.requiredBy ? new Date(`${request.requiredBy}T00:00:00`).toLocaleDateString(undefined, { dateStyle: 'medium' }) : 'Activity / project deadline'}</dd></div><div><dt>Budget limit</dt><dd>{request.budgetLimit == null ? 'No limit supplied' : money(request.budgetLimit)}</dd></div><div><dt>Estimated procurement</dt><dd>{procurement ? money(procurement.estimatedTotal) : 'Analysis pending'}</dd></div></dl></>}
        {detail.error && <p role="alert" className="form-alert">Planning error: {readable(detail.error).replaceAll('_', ' ')}</p>}
        <div className="workflow-review-grid">
          {request && <ReviewCard title="Requirements"><ul className="workflow-resource-list">{request.items.map((item, index) => <li key={index}><div><span className="workflow-resource-kind">{item.kind}</span><strong>{item.name}</strong>{item.resourceCount != null && <small>{amount(item.resourceCount)} {item.kind === 'Equipment' ? 'machines' : 'workers'}</small>}</div><span className="workflow-quantity">{amount(item.quantity)} <small>{item.unit}</small></span></li>)}</ul>{!request.items.length && <p className="workflow-muted">No requirements recorded.</p>}</ReviewCard>}
          <ReviewCard title="Analysis steps"><ol className="workflow-steps">{detail.plan?.tasks?.map((task, index) => <li key={task.task_id}><span className="workflow-step-number" aria-hidden="true">{index + 1}</span><strong>{({ InventoryAgent: 'Inventory check', ProcurementAgent: 'Procurement review', SchedulingValidationAgent: 'Schedule validation' })[task.agent] ?? readable(task.agent.replace(/Agent$/, ''))}</strong><StatusBadge status={task.status} /></li>)}</ol>{!detail.plan?.tasks?.length && <p className="workflow-muted">Analysis results will appear after planning completes.</p>}</ReviewCard>
          {scheduling && <ReviewCard title="Proposed schedule"><div className="workflow-schedule-times"><div><span>Start</span><strong>{dateTime(scheduling.startTime)}</strong></div><span aria-hidden="true">→</span><div><span>End</span><strong>{dateTime(scheduling.endTime)}</strong></div></div><dl className="workflow-detail-list"><div><dt>Workers</dt><dd>{scheduling.workers?.map(x => resourceName('workers', x.workerId)).join(', ') || 'None requested'}</dd></div><div><dt>Equipment</dt><dd>{scheduling.equipment?.map(x => resourceName('equipment', x.equipmentId)).join(', ') || 'None requested'}</dd></div><div><dt>Validation</dt><dd><StatusBadge status={scheduling.validation?.isValid ? 'Valid' : 'Requires changes'} /></dd></div></dl>{scheduling.validation?.issues?.length > 0 && <ul className="workflow-issues">{scheduling.validation.issues.map((issue, index) => <li key={index}>{issue}</li>)}</ul>}</ReviewCard>}
          {inventory && <ReviewCard title="Material coverage"><ul className="workflow-coverage">{inventory.items?.map((item, index) => <li key={index}><strong>{item.name}</strong><dl><div><dt>Available</dt><dd>{amount(item.availableQuantity)} {item.unit}</dd></div><div><dt>Shortage</dt><dd className={Number(item.shortageQuantity) > 0 ? 'workflow-shortage' : 'workflow-covered'}>{amount(item.shortageQuantity)} {item.unit}</dd></div></dl></li>)}</ul>{!inventory.items?.length && <p className="workflow-muted">No materials to check.</p>}</ReviewCard>}
          {procurement && <ReviewCard title="Procurement" className="workflow-card-wide"><div className="workflow-cost"><span>Estimated cost</span><strong>{money(procurement.estimatedTotal)}</strong></div>{procurement.recommendations?.length ? <ul className="workflow-resource-list">{procurement.recommendations.map((item, index) => <li key={index}><div><strong>{item.materialName}</strong><small>{amount(item.shortageQuantity)} {item.unit} · {item.recommendedSupplier?.supplierName ?? 'Supplier not selected'}</small></div><span className="workflow-quantity">{item.recommendedSupplier ? money(item.recommendedSupplier.totalPrice) : 'Awaiting quote'}</span></li>)}</ul> : <p className="workflow-muted">No procurement recommendations for this plan.</p>}</ReviewCard>}
        </div>
        {detail.plan?.approval && <div className="workflow-recorded-decision"><strong>Recorded decision</strong><p>{detail.plan.approval.reason}</p></div>}
      </div>
      {(detail.status === 'AwaitingAgents' || ['Failed', 'RevisionRequested'].includes(detail.status)) && <footer className="workflow-actions"><p>{detail.status === 'AwaitingAgents' ? 'Run the resource checks before reviewing approval.' : 'Retry planning using the saved resource request.'}</p><button disabled={busy} className="button button-primary" onClick={() => detail.status === 'AwaitingAgents' ? run(`/workflows/${detail.id}/execute`) : run(`/construction/resource-requests/${detail.resourceRequestId}/planning`)}>{busy ? 'Processing…' : detail.status === 'AwaitingAgents' ? 'Analyze resource plan' : 'Retry / revise plan'}</button></footer>}
      {manager && detail.status === 'PendingProjectManagerApproval' && <footer className="workflow-decision">
        <div><h3>Manager decision</h3><p>Approval commits resource bookings and purchase orders. Add a reason for your decision.</p></div>
        {scheduling && <div className="workflow-approval-schedule"><label className="workflow-reason">Schedule start<input type="datetime-local" required disabled={busy} value={scheduleStart} onChange={event => { setScheduleStart(event.target.value); setNow(Date.now()) }} /></label><div><span>Schedule end</span><strong>{dateTime(scheduleEnd)}</strong><small>Duration stays the same. Availability and deadlines are checked again when you approve.</small></div></div>}
        {invalidStart && <p role="alert" className="form-alert">The proposed start time has passed or is invalid. Choose a future schedule start to approve this plan.</p>}
        <label className="workflow-reason">Decision reason<textarea required rows={3} maxLength={2000} placeholder="Summarize your review or explain any changes needed…" disabled={busy} value={reason} onChange={event => { setReason(event.target.value); setNow(Date.now()) }} /></label>
        <div className="workflow-decision-bottom"><small>{busy ? 'Recording decision…' : !reason.trim() ? 'Enter a reason to enable decision actions.' : 'Your reason will be saved with this decision.'}</small><div className="workflow-decision-buttons">{['Rejected', 'RevisionRequested', 'Approved'].map(decision => <button key={decision} disabled={busy || !reason.trim() || (decision === 'Approved' && invalidStart)} className={`button ${decision === 'Approved' ? 'button-primary' : decision === 'Rejected' ? 'workflow-reject' : 'button-secondary'}`} onClick={() => {
          if (decision === 'Approved' && scheduling && selectedStart <= Date.now()) { setNow(Date.now()); return }
          if (globalThis.confirm(`Record ${readable(decision)} decision?${decision === 'Approved' ? ` Schedule: ${dateTime(startChanged ? new Date(selectedStart).toISOString() : scheduling?.startTime)}. Approval commits resource bookings and purchase orders.` : ''}`)) run(`/workflows/${detail.id}/decision`, { decision, reason, ...(decision === 'Approved' && startChanged ? { scheduleStart: new Date(selectedStart).toISOString() } : {}) })
        }}>{decision === 'RevisionRequested' ? 'Request revision' : decision === 'Approved' ? 'Approve' : 'Reject'}</button>)}</div></div>
      </footer>}
    </section>}
  </main>
}
