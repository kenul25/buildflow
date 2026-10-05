import React, { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import { allRows } from '../../services/lookups.js'
import { useAuth } from '../../hooks/useAuth.js'
import '../inventory/inventory.css'

const config = {
  Suppliers: { title: 'Suppliers', fields: [['name', 'Name', 'text'], ['contactPerson', 'Contact person', 'text'], ['email', 'Email', 'email'], ['phone', 'Phone', 'text'], ['address', 'Address', 'text']] },
  SupplierMaterials: { title: 'Supplier materials', fields: [['supplierId', 'Supplier', 'suppliers'], ['materialId', 'Material', 'materials'], ['availableQuantity', 'Available quantity', 'number'], ['unitPrice', 'Unit price', 'number'], ['leadTimeDays', 'Lead time (days)', 'number']] },
  PurchaseRequests: { title: 'Purchase requests', fields: [['projectId', 'Project', 'projects'], ['materialId', 'Material', 'materials'], ['quantity', 'Quantity', 'number'], ['budgetLimit', 'Budget limit (optional)', 'number', false], ['requiredByDate', 'Required by', 'date']] },
  Quotations: { title: 'Quotations', fields: [['supplierId', 'Supplier', 'suppliers'], ['materialId', 'Material', 'materials'], ['quantity', 'Available quoted quantity', 'number'], ['unitPrice', 'Unit price', 'number'], ['deliveryDate', 'Delivery date', 'date'], ['validUntil', 'Valid until', 'date']] },
  PurchaseOrders: { title: 'Purchase orders', fields: [['purchaseRequestId', 'Approved request', 'requests'], ['supplierId', 'Supplier', 'suppliers'], ['quotationId', 'Quotation (optional)', 'quotes', false], ['unitPrice', 'Verified unit price', 'number'], ['deliveryDate', 'Delivery date', 'date']] },
  Deliveries: { title: 'Deliveries', fields: [['purchaseOrderId', 'Purchase order', 'orders'], ['quantity', 'Quantity', 'number'], ['deliveryDate', 'Delivery date', 'date']] },
}
export default function ProcurementRecordsPage({ kind }) {
  const { user } = useAuth()
  const canApprove = user.roles.some(x => ['Administrator', 'ProjectManager'].includes(x))
  const [rows, setRows] = useState([])
  const [lookups, setLookups] = useState({})
  const [form, setForm] = useState(null)
  const [detail, setDetail] = useState(null)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [sort, setSort] = useState('updatedAt')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const load = useCallback(async () => {
    setLoading(true)
    try {
      const paths = { projects: '/projects', materials: '/inventory/materials/page', suppliers: '/Suppliers', requests: '/PurchaseRequests', orders: '/PurchaseOrders', quotes: '/Quotations', offers: '/SupplierMaterials' }
      const keys = Object.keys(paths)
      const result = await Promise.all(keys.map(key => allRows(paths[key])))
      setLookups(Object.fromEntries(keys.map((key, index) => [key, result[index]])))
      setRows(await allRows(`/${kind}`))
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setLoading(false) }
  }, [kind])
  useEffect(() => { queueMicrotask(() => { setForm(null); setPage(1); setError(''); load() }) }, [load])
  const open = row => {
    const value = { ...row }
    for (const [key, , type] of config[kind].fields) if (type === 'date' && value[key]) value[key] = String(value[key]).slice(0, 10)
    setForm(value); setError('')
  }
  const update = (key, value) => {
    const next = { ...form, [key]: value }
    if (key === 'quotationId') {
      const quote = lookups.quotes?.find(x => String(x.id) === value)
      if (quote) { next.supplierId = String(quote.supplierId); next.unitPrice = quote.unitPrice; next.deliveryDate = quote.deliveryDate.slice(0, 10) }
    }
    if (key === 'supplierId' || key === 'purchaseRequestId') {
      const request = lookups.requests?.find(x => String(x.id) === String(next.purchaseRequestId))
      const offer = lookups.offers?.find(x => String(x.supplierId) === String(next.supplierId) && x.materialId === request?.materialId)
      if (offer) next.unitPrice = offer.unitPrice
    }
    setForm(next)
  }
  const name = (key, value) => {
    const source = { supplierId: 'suppliers', projectId: 'projects', materialId: 'materials', purchaseRequestId: 'requests', purchaseOrderId: 'orders', quotationId: 'quotes' }[key]
    const row = lookups[source]?.find(x => String(x.id) === String(value))
    return row ? row.name ?? `${row.materialName} · #${row.id}` : String(value ?? '—')
  }
  async function save(event) {
    event.preventDefault(); if (saving) return; setSaving(true); setError('')
    try {
      const body = { ...form }
      for (const [key, , type] of config[kind].fields.filter(([key]) => !form.legacy || ['projectId', 'materialId'].includes(key))) {
        if (type === 'number') body[key] = body[key] === '' || body[key] == null ? null : Number(body[key])
        if (type === 'date') body[key] = new Date(`${body[key]}T23:59:59`).toISOString()
        if (['supplierId', 'purchaseRequestId', 'purchaseOrderId', 'quotationId'].includes(key)) body[key] = body[key] ? Number(body[key]) : null
      }
      const material = lookups.materials?.find(x => x.id === body.materialId)
      if (material) body.materialName = material.name
      if (form.legacy) await api.put(`/PurchaseRequests/${form.id}/links`, body); else await api[form.id ? 'put' : 'post'](`/${kind}${form.id ? `/${form.id}` : ''}`, body)
      setForm(null); setSuccess('Saved successfully.'); await load()
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setSaving(false) }
  }
  async function action(row, actionName) {
    if (saving || !globalThis.confirm(`${actionName} this record?`)) return
    setSaving(true); setError('')
    try {
      if (actionName === 'Approve') await api.put(`/PurchaseRequests/${row.id}/approve`)
      else if (['Received', 'Completed', 'Rejected'].includes(actionName)) await api.put(`/Deliveries/${row.id}/status`, null, { params: { status: actionName } })
      else await api.delete(`/${kind}/${row.id}`)
      setSuccess(`${actionName} recorded.`); await load()
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setSaving(false) }
  }
  async function evidence(row, file) {
    if (!file || saving) return; setSaving(true)
    try { const body = new FormData(); body.append('file', file); await api.post(`/Deliveries/${row.id}/evidence`, body); setSuccess('Delivery photo saved.'); await load() }
    catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setSaving(false) }
  }
  const recordLabel = { Suppliers: 'supplier', SupplierMaterials: 'supplier material', PurchaseRequests: 'purchase request', Quotations: 'quotation', PurchaseOrders: 'purchase order', Deliveries: 'delivery' }[kind]
  const filtered = rows.filter(x => `${x.name ?? ''} ${x.materialName ?? name('materialId', x.materialId)} ${x.supplierName ?? ''} ${x.status ?? ''} ${x.id}`.toLowerCase().includes(search.toLowerCase()) && (!status || x.status === status)).sort((a, b) => sort === 'name' ? String(a.name ?? a.materialName ?? '').localeCompare(String(b.name ?? b.materialName ?? '')) : String(b[sort] ?? '').localeCompare(String(a[sort] ?? '')))
  return <main className="dashboard-content inventory-page"><div className="page-heading"><div><span className="eyebrow">Procurement</span><h1>{config[kind].title}</h1><p>Manage project-linked procurement with verified quantities, prices and dates.</p></div><button className="button button-primary" onClick={() => open({})}>Create</button></div>
    {error && <p role="alert" className="form-alert">{error}</p>}{success && <p role="status" className="success-alert">{success}</p>}
    <div className="inventory-toolbar"><label>Search<input value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} /></label><label>Status<select value={status} onChange={e => { setStatus(e.target.value); setPage(1) }}><option value="">All</option>{[...new Set(rows.map(x => x.status).filter(Boolean))].map(x => <option key={x}>{x}</option>)}</select></label><label>Sort<select value={sort} onChange={e => setSort(e.target.value)}><option value="updatedAt">Latest update</option><option value="name">Name</option><option value="deliveryDate">Delivery date</option></select></label></div>
    <section className="admin-panel">{loading ? <div className="table-state" role="status">Loading…</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Reference</th><th>{kind === 'Suppliers' ? 'Supplier / contact' : 'Material / quantity'}</th><th>Status</th><th>Actions</th></tr></thead><tbody>{filtered.slice((page - 1) * 10, page * 10).map(row => <tr key={row.id}><td>#{row.id}</td><td>{row.name ?? row.materialName ?? name("materialId", row.materialId)}<small className="construction-subtext">{row.quantity ?? row.availableQuantity ?? row.contactPerson} {row.unit ?? row.supplierName}</small></td><td>{row.status ?? 'Active'}</td><td className="row-actions"><button className="table-action" onClick={async () => { try { setDetail((await api.get(`/${kind}/${row.id}`)).data) } catch (cause) { setError(apiErrorMessage(cause)) } }}>Details</button>{(!row.status || row.status === 'Pending') && <><button className="table-action" onClick={() => open(row)}>Edit</button><button disabled={saving} className="table-action danger" onClick={() => action(row, 'Cancel')}>Cancel</button></>}{kind === 'PurchaseRequests' && canApprove && (!row.materialId || !row.projectId) && <button className="table-action" onClick={() => open({ ...row, legacy: true })}>Link historical record</button>}{kind === 'PurchaseRequests' && canApprove && row.status === 'Pending' && <button disabled={saving} className="table-action" onClick={() => action(row, 'Approve')}>Approve</button>}{kind === 'Deliveries' && row.status === 'Pending' && <><button disabled={saving} className="table-action" onClick={() => action(row, 'Received')}>Confirm received</button><button disabled={saving} className="table-action" onClick={() => action(row, 'Rejected')}>Reject</button></>}{kind === 'Deliveries' && row.status === 'Received' && <button disabled={saving} className="table-action" onClick={() => action(row, 'Completed')}>Complete</button>}{kind === 'Deliveries' && ['Pending', 'Received'].includes(row.status) && <label>Delivery photo<input disabled={saving} type="file" accept="image/png,image/jpeg,image/webp" onChange={e => evidence(row, e.target.files[0])} /></label>}</td></tr>)}</tbody></table>{filtered.length === 0 && <div className="table-state">No records found.</div>}</div>}<div className="inventory-pagination"><span>{filtered.length} records</span><div><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button><span>Page {page}</span><button disabled={page * 10 >= filtered.length} onClick={() => setPage(page + 1)}>Next</button></div></div></section>
    {form && <div className="modal-backdrop"><section className="user-modal" role="dialog" aria-modal="true" aria-label="Procurement form"><div className="modal-heading"><div><span className="eyebrow">Procurement</span><h2>{form.legacy ? 'Link historical request' : `${form.id ? 'Edit' : 'Create'} ${recordLabel}`}</h2></div><button type="button" className="modal-close" disabled={saving} onClick={() => setForm(null)} aria-label="Close procurement form">×</button></div><form className="inventory-form" onSubmit={save}>{config[kind].fields.filter(([key]) => !form.legacy || ["projectId", "materialId"].includes(key)).map(([key, label, type, required = true]) => <label key={key}>{label}{lookups[type] ? <select required={required} disabled={Boolean(form.id) && ['purchaseRequestId', 'purchaseOrderId', ...(kind === 'SupplierMaterials' ? ['supplierId', 'materialId'] : [])].includes(key)} value={form[key] ?? ''} onChange={e => update(key, e.target.value)}><option value="">Select</option>{lookups[type].filter(x => type !== 'requests' || x.status === 'Approved' || x.id === form.purchaseRequestId).filter(x => type !== 'orders' || !['Cancelled', 'Completed'].includes(x.status)).map(x => <option key={x.id} value={x.id}>{x.name ?? `${x.materialName} · #${x.id}`}</option>)}</select> : <input required={required} type={type} min={type === 'number' ? key === 'quantity' ? '0.001' : '0' : undefined} step={type === 'number' ? '0.001' : undefined} value={form[key] ?? ''} onChange={e => update(key, e.target.value)} />}</label>)}{error && <p role="alert">{error}</p>}<div className="modal-actions"><button type="button" className="button button-secondary" disabled={saving} onClick={() => setForm(null)}>Cancel</button><button className="button button-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button></div></form></section></div>}
    {detail && <div className="modal-backdrop"><section className="user-modal" role="dialog" aria-modal="true" aria-label="Procurement details"><div className="modal-heading"><div><span className="eyebrow">Procurement</span><h2>Record #{detail.id}</h2></div><button type="button" className="modal-close" onClick={() => setDetail(null)} aria-label="Close details">×</button></div><div className="modal-body">{Object.entries(detail).filter(([key, value]) => value != null && !key.endsWith('ById') && typeof value !== 'object').map(([key, value]) => <p key={key}><strong>{key.replace(/([A-Z])/g, ' $1')}:</strong> {name(key, value)}</p>)}<button className="button button-secondary" onClick={() => setDetail(null)}>Close</button></div></section></div>}
  </main>
}
