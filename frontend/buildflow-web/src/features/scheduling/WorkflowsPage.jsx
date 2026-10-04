import React, { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import { useAuth } from '../../hooks/useAuth.js'
import { allRows } from '../../services/lookups.js'

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
  const load = useCallback(async () => { try { setRows((await api.get('/workflows')).data) } catch (e) { setError(apiErrorMessage(e)) } }, [])
  useEffect(() => { queueMicrotask(load) }, [load])
  async function review(id) {
    try {
      const workflow = (await api.get(`/workflows/${id}`)).data
      const [requestData, workers, equipment] = await Promise.all([api.get(`/construction/resource-requests/${workflow.resourceRequestId}`), allRows('/scheduling/workers'), allRows('/scheduling/equipment')])
      setDetail(workflow); setRequest(requestData.data); setResources({ workers, equipment }); setReason(''); setError('')
    } catch (e) { setError(apiErrorMessage(e)) }
  }
  const resourceName = (kind, id) => resources[kind].find(x => x.id === id)?.name ?? 'Archived resource'
  async function run(path, body) {
    if (busy) return
    setBusy(true); setError('')
    try { setDetail((await api.post(path, body, { timeout: 150000 })).data); await load() }
    catch (e) { setError(apiErrorMessage(e)) }
    finally { setBusy(false) }
  }
  const scheduling = detail?.plan?.tasks?.find(x => x.agent === 'SchedulingValidationAgent')?.output
  const procurement = detail?.plan?.tasks?.find(x => x.agent === 'ProcurementAgent')?.output
  const inventory = detail?.plan?.tasks?.find(x => x.agent === 'InventoryAgent')?.output
  return <main className="dashboard-content"><span className="eyebrow">Resource plans</span><h1>Plans & approvals</h1><p>Review material coverage, procurement and proposed schedules before committing resources.</p>{error && <p role="alert" className="form-alert">{error}</p>}
    <section className="admin-panel"><table className="users-table"><thead><tr><th>Objective</th><th>Status</th><th>Action</th></tr></thead><tbody>{rows.map(row => <tr key={row.id}><td>{row.objective}</td><td>{row.status}</td><td><button className="table-action" onClick={() => review(row.id)}>Review</button></td></tr>)}</tbody></table>{!rows.length && <p>No resource plans yet. Submit a resource request from a site activity.</p>}</section>
    {detail && <section className="admin-panel"><h2>Resource proposal</h2>{request && <><h3>{request.objective}</h3><p>Deadline: {request.requiredBy ?? "Activity/project deadline"} · Budget: {request.budgetLimit ?? "No limit supplied"}</p><h3>Requirements</h3>{request.items.map((item, index) => <p key={index}>{item.kind}: {item.name} · {item.quantity} {item.unit}</p>)}</>}<h3>Analysis steps</h3>{detail.plan?.tasks?.map(task => <p key={task.task_id}>{task.agent.replace(/Agent$/, "")} · {task.status}</p>)}<p>Status: {detail.status}</p>{detail.error && <p role="alert">{detail.error}</p>}{scheduling && <><h3>Schedule</h3><p>{new Date(scheduling.startTime).toLocaleString()} – {new Date(scheduling.endTime).toLocaleString()}</p><p>Workers: {scheduling.workers?.map(x => resourceName("workers", x.workerId)).join(", ") || "None"}</p><p>Equipment: {scheduling.equipment?.map(x => resourceName("equipment", x.equipmentId)).join(", ") || "None"}</p><p>Validation: {scheduling.validation?.isValid ? 'Valid' : 'Requires changes'}</p>{scheduling.validation?.issues?.map((x, i) => <p key={i}>{x}</p>)}</>}{inventory && <><h3>Material coverage</h3>{inventory.items?.map((item, index) => <p key={index}>{item.name} · Available: {item.availableQuantity ?? 0} {item.unit} · Shortage: {item.shortageQuantity ?? 0}</p>)}</>}{procurement && <><h3>Procurement</h3><p>Estimated cost: LKR {Number(procurement.estimatedTotal ?? 0).toLocaleString()}</p>{procurement.recommendations?.map((x, i) => <p key={i}>{x.materialName}: {x.shortageQuantity} {x.unit} · {x.recommendedSupplier.supplierName} · LKR {x.recommendedSupplier.totalPrice}</p>)}</>}{detail.plan?.approval && <p>Decision: {detail.plan.approval.reason}</p>}
      {detail.status === 'AwaitingAgents' && <button disabled={busy} className="button button-primary" onClick={() => run(`/workflows/${detail.id}/execute`)}>Analyze resource plan</button>}
      {['Failed', 'RevisionRequested'].includes(detail.status) && <button disabled={busy} className="button button-secondary" onClick={() => run(`/construction/resource-requests/${detail.resourceRequestId}/planning`)}>Retry / revise plan</button>}
      {manager && detail.status === 'PendingProjectManagerApproval' && <><label>Decision reason<textarea required maxLength={2000} value={reason} onChange={e => setReason(e.target.value)} /></label><div className="modal-actions">{['Approved', 'Rejected', 'RevisionRequested'].map(decision => <button key={decision} disabled={busy || !reason.trim()} className="button button-secondary" onClick={() => { if (globalThis.confirm(`Record ${decision} decision? Approval commits resource bookings and purchase orders.`)) run(`/workflows/${detail.id}/decision`, { decision, reason }) }}>{decision === 'RevisionRequested' ? 'Request revision' : decision === 'Approved' ? 'Approve' : 'Reject'}</button>)}</div></>}
    </section>}
  </main>
}
