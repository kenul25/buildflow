import React, { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../../hooks/useAuth.js'
import { apiErrorMessage } from '../../services/api.js'
import { inventoryService } from './inventoryService.js'
import './inventory.css'

const tabs = ['materials', 'warehouses', 'reservations', 'movements', 'alerts']
const emptyMaterial = { name: '', category: '', unit: '', unitPrice: 0, currentStock: 0, reservedStock: 0, warehouseId: '' }
const emptyWarehouse = { name: '', location: '' }

export default function InventoryPage() {
  const { user } = useAuth()
  const canManage = user.roles.some((role) => ['Administrator', 'ProjectManager', 'InventoryOfficer'].includes(role))
  const [tab, setTab] = useState('materials')
  const [materials, setMaterials] = useState({ items: [], total: 0 })
  const [warehouses, setWarehouses] = useState({ items: [], total: 0 })
  const [reservations, setReservations] = useState({ items: [], total: 0 })
  const [movements, setMovements] = useState({ items: [], total: 0 })
  const [alerts, setAlerts] = useState([])
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [dialog, setDialog] = useState(null)
  const [quantityDialog, setQuantityDialog] = useState(null)

  const load = useCallback(async () => {
    setLoading(true); setError('')
    try {
      if (tab === 'materials') { setMaterials(await inventoryService.materials({ search, page, pageSize: 10, sort: 'name' })); if (!warehouses.items.length) setWarehouses(await inventoryService.warehouses({ page: 1, pageSize: 100, sort: 'name' })) }
      if (tab === 'warehouses') setWarehouses(await inventoryService.warehouses({ search, page, pageSize: 10, sort: 'name' }))
      if (tab === 'reservations') setReservations(await inventoryService.reservations({ page, pageSize: 10 }))
      if (tab === 'movements') setMovements(await inventoryService.movements({ page, pageSize: 10 }))
      if (tab === 'alerts') setAlerts(await inventoryService.alerts(0))
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setLoading(false) }
  }, [tab, search, page, warehouses.items.length])

  useEffect(() => { queueMicrotask(load) }, [load])
  const notify = (message) => { setSuccess(message); setDialog(null); load() }

  async function remove(kind, item) {
    if (!window.confirm(`Delete ${item.name}? This action is safe and may be blocked when history exists.`)) return
    try { await (kind === 'material' ? inventoryService.deleteMaterial(item.id) : inventoryService.deleteWarehouse(item.id)); notify(`${item.name} deleted.`) }
    catch (cause) { setError(apiErrorMessage(cause)) }
  }

  async function release(reservation) {
    try { await inventoryService.release(reservation.id); notify('Reservation cancelled.') }
    catch (cause) { setError(apiErrorMessage(cause)) }
  }

  async function applyQuantity(quantity) {
    const { item, action } = quantityDialog
    try {
      if (action === 'reserve') await inventoryService.reserve(item.id, { quantity, durationMinutes: 60 })
      else await inventoryService.stock(item.id, action, { quantity, reference: action })
      notify(`${item.name} ${action === 'reserve' ? 'reserved' : `${action}d`} successfully.`)
    } catch (cause) { setError(apiErrorMessage(cause)) }
    finally { setQuantityDialog(null) }
  }

  const data = tab === 'materials' ? materials : tab === 'warehouses' ? warehouses : tab === 'reservations' ? reservations : movements
  return <main className="dashboard-content inventory-page">
    <div className="page-heading"><div><span className="eyebrow">Member 02 · Inventory control</span><h1>Materials & inventory</h1><p className="lead">Track stock, reservations, movements, and shortages across every warehouse.</p></div>{canManage && <button className="button button-primary" onClick={() => setDialog(tab === 'warehouses' ? { kind: 'warehouse', value: emptyWarehouse } : { kind: 'material', value: emptyMaterial })}>Add {tab === 'warehouses' ? 'warehouse' : 'material'}</button>}</div>
    <nav className="inventory-tabs" aria-label="Inventory sections">{tabs.map((item) => <button key={item} className={tab === item ? 'active' : ''} onClick={() => { setTab(item); setPage(1); setSearch(''); setSuccess('') }}>{item}</button>)}</nav>
    {success && <p className="success-alert" role="status">{success}</p>}{error && <div className="form-alert" role="alert">{error} <button onClick={load}>Retry</button></div>}
    {(tab === 'materials' || tab === 'warehouses') && <div className="inventory-toolbar"><label>Search<input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder={`Search ${tab}`} /></label></div>}
    <section className="admin-panel inventory-panel">{loading ? <div className="table-state"><span className="spinner" /> Loading inventory…</div> : tab === 'materials' ? <MaterialTable items={materials.items} canManage={canManage} onEdit={(item) => setDialog({ kind: 'material', value: item })} onDelete={(item) => remove('material', item)} onReserve={(item) => setQuantityDialog({ item, action: 'reserve' })} onStock={(item, action) => setQuantityDialog({ item, action })} /> : tab === 'warehouses' ? <WarehouseTable items={warehouses.items} canManage={canManage} onEdit={(item) => setDialog({ kind: 'warehouse', value: item })} onDelete={(item) => remove('warehouse', item)} /> : tab === 'reservations' ? <ReservationTable items={reservations.items} canManage={canManage} onRelease={release} /> : tab === 'movements' ? <MovementTable items={movements.items} /> : <AlertTable items={alerts} />}
      {tab !== 'alerts' && <div className="inventory-pagination"><span>{data.total ?? 0} total</span><div><button disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button><span>Page {page}</span><button disabled={page * 10 >= (data.total ?? 0)} onClick={() => setPage(page + 1)}>Next</button></div></div>}
    </section>
    {dialog?.kind === 'material' && <MaterialDialog value={dialog.value} warehouses={warehouses.items} onClose={() => setDialog(null)} onSaved={notify} />}
    {dialog?.kind === 'warehouse' && <WarehouseDialog value={dialog.value} onClose={() => setDialog(null)} onSaved={notify} />}
    {quantityDialog && <QuantityDialog item={quantityDialog.item} action={quantityDialog.action} onClose={() => setQuantityDialog(null)} onSubmit={applyQuantity} />}
  </main>
}

function MaterialTable({ items, canManage, onEdit, onDelete, onReserve, onStock }) { return items.length === 0 ? <div className="table-state">No materials found.</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Material</th><th>Warehouse</th><th>Current</th><th>Reserved</th><th>Available</th><th>Actions</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.name}</strong><small className="construction-subtext">{item.category} · {item.unit}</small></td><td>{item.warehouseName}</td><td>{item.currentStock}</td><td>{item.reservedStock}</td><td><strong>{item.availableStock}</strong></td><td className="row-actions">{canManage && <><button className="table-action" onClick={() => onStock(item, 'receive')}>Receive</button><button className="table-action" onClick={() => onStock(item, 'issue')}>Issue</button><button className="table-action" onClick={() => onStock(item, 'return')}>Return</button><button className="table-action" onClick={() => onReserve(item)}>Reserve</button><button className="table-action" onClick={() => onEdit(item)}>Edit</button><button className="table-action danger" onClick={() => onDelete(item)}>Delete</button></>}</td></tr>)}</tbody></table></div> }
function WarehouseTable({ items, canManage, onEdit, onDelete }) { return items.length === 0 ? <div className="table-state">No warehouses found.</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Warehouse</th><th>Location</th><th>Actions</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td><strong>{item.name}</strong></td><td>{item.location || 'No location'}</td><td className="row-actions">{canManage && <><button className="table-action" onClick={() => onEdit(item)}>Edit</button><button className="table-action danger" onClick={() => onDelete(item)}>Delete</button></>}</td></tr>)}</tbody></table></div> }
function ReservationTable({ items, canManage, onRelease }) { return items.length === 0 ? <div className="table-state">No reservations found.</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Material</th><th>Quantity</th><th>Status</th><th>Expires</th><th>Actions</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{item.materialName}</td><td>{item.quantity}</td><td><span className={`status ${item.status === 'Active' ? 'warning' : 'success'}`}>{item.status}</span></td><td>{new Date(item.expiresAt).toLocaleString()}</td><td>{canManage && item.status === 'Active' && <button className="table-action danger" onClick={() => onRelease(item)}>Cancel</button>}</td></tr>)}</tbody></table></div> }
function MovementTable({ items }) { return items.length === 0 ? <div className="table-state">No stock movements found.</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Material</th><th>Type</th><th>Quantity</th><th>Stock after</th><th>Recorded</th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{item.materialName}</td><td>{item.type}</td><td>{item.quantity}</td><td>{item.stockAfter}</td><td>{new Date(item.createdAt).toLocaleString()}</td></tr>)}</tbody></table></div> }
function AlertTable({ items }) { return items.length === 0 ? <div className="table-state">No low-stock alerts. Inventory is above threshold.</div> : <div className="table-scroll"><table className="users-table"><thead><tr><th>Material</th><th>Available</th><th>Severity</th><th>Message</th></tr></thead><tbody>{items.map((item) => <tr key={item.materialId}><td>{item.materialName}</td><td>{item.availableStock} {item.unit}</td><td><span className={`status ${item.severity === 'Critical' ? 'danger-status' : 'warning'}`}>{item.severity}</span></td><td>{item.message}</td></tr>)}</tbody></table></div> }

function MaterialDialog({ value, warehouses, onClose, onSaved }) { const [form, setForm] = useState({ ...emptyMaterial, ...value }); const [error, setError] = useState(''); const save = async (event) => { event.preventDefault(); try { const body = { ...form, unitPrice: Number(form.unitPrice), currentStock: Number(form.currentStock), reservedStock: Number(form.reservedStock) }; const result = form.id ? await inventoryService.updateMaterial(form.id, body) : await inventoryService.createMaterial(body); onSaved(`${result.name} saved.`) } catch (cause) { setError(apiErrorMessage(cause)) } }; return <Dialog title={form.id ? 'Edit material' : 'Add material'} onClose={onClose}><form className="inventory-form" onSubmit={save}>{error && <p className="form-alert">{error}</p>}<label>Name<input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label>Category<input required value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })} /></label><label>Unit<input required value={form.unit} onChange={(e) => setForm({ ...form, unit: e.target.value })} /></label><label>Warehouse<select required value={form.warehouseId} onChange={(e) => setForm({ ...form, warehouseId: e.target.value })}><option value="">Select warehouse</option>{warehouses.map((warehouse) => <option key={warehouse.id} value={warehouse.id}>{warehouse.name}</option>)}</select></label><label>Unit price<input type="number" min="0" step="0.01" value={form.unitPrice} onChange={(e) => setForm({ ...form, unitPrice: e.target.value })} /></label><label>Current stock<input type="number" min="0" step="0.001" value={form.currentStock} onChange={(e) => setForm({ ...form, currentStock: e.target.value })} /></label><label>Reserved stock<input type="number" min="0" step="0.001" value={form.reservedStock} onChange={(e) => setForm({ ...form, reservedStock: e.target.value })} /></label><DialogActions onClose={onClose} /></form></Dialog> }
function WarehouseDialog({ value, onClose, onSaved }) { const [form, setForm] = useState({ ...emptyWarehouse, ...value }); const [error, setError] = useState(''); const save = async (event) => { event.preventDefault(); try { const result = form.id ? await inventoryService.updateWarehouse(form.id, form) : await inventoryService.createWarehouse(form); onSaved(`${result.name} saved.`) } catch (cause) { setError(apiErrorMessage(cause)) } }; return <Dialog title={form.id ? 'Edit warehouse' : 'Add warehouse'} onClose={onClose}><form className="inventory-form" onSubmit={save}>{error && <p className="form-alert">{error}</p>}<label>Name<input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label>Location<input value={form.location || ''} onChange={(e) => setForm({ ...form, location: e.target.value })} /></label><DialogActions onClose={onClose} /></form></Dialog> }
function QuantityDialog({ item, action, onClose, onSubmit }) { const [quantity, setQuantity] = useState('1'); const [error, setError] = useState(''); const submit = (event) => { event.preventDefault(); const value = Number(quantity); if (!Number.isFinite(value) || value <= 0) { setError('Enter a quantity greater than zero.'); return } if (action === 'issue' && value > item.availableStock) { setError(`Only ${item.availableStock} ${item.unit} is available.`); return } onSubmit(value) }; const title = action === 'reserve' ? 'Reserve material' : `${action.charAt(0).toUpperCase()}${action.slice(1)} stock`; return <Dialog title={title} onClose={onClose}><form className="inventory-form" onSubmit={submit}>{error && <p className="form-alert" role="alert">{error}</p>}<p>Available for {item.name}: <strong>{item.availableStock} {item.unit}</strong></p><label>Quantity ({item.unit})<input autoFocus required type="number" min="0.001" step="0.001" value={quantity} onChange={(event) => setQuantity(event.target.value)} /></label><DialogActions onClose={onClose} /></form></Dialog> }
function Dialog({ title, onClose, children }) { return <div className="modal-backdrop" role="presentation"><section className="user-modal" role="dialog" aria-modal="true" aria-label={title}><div className="modal-heading"><div><span className="eyebrow">Inventory control</span><h2>{title}</h2></div><button className="modal-close" onClick={onClose} aria-label="Close">×</button></div>{children}</section></div> }
function DialogActions({ onClose }) { return <div className="modal-actions"><button type="button" className="button button-secondary" onClick={onClose}>Cancel</button><button type="submit" className="button button-primary">Save</button></div> }