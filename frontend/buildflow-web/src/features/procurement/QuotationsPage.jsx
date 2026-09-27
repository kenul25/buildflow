import { useEffect, useMemo, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import { supplierService } from './supplierService.js'

const emptyForm = {
  supplierId: '',
  materialName: '',
  quantity: '',
  unitPrice: '',
  deliveryDate: '',
}

const getTomorrow = () => {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  return date.toISOString().slice(0, 10)
}

const formatMoney = (value) =>
  Number(value ?? 0).toLocaleString('en-LK', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })

const formatDate = (value) => {
  if (!value) return '-'

  return new Date(value).toLocaleDateString('en-LK', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export default function QuotationsPage() {
  const [quotations, setQuotations] = useState([])
  const [suppliers, setSuppliers] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState(null)
  const [search, setSearch] = useState('')
  const [supplierFilter, setSupplierFilter] = useState('')
  const [sort, setSort] = useState('deliveryDate')
  const [desc, setDesc] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const loadData = async () => {
    try {
      setLoading(true)
      setError('')

      const [supplierData, quotationData] = await Promise.all([
        supplierService.suppliers(),
        api.get('/Quotations', {
          params: {
            search: search || undefined,
            supplierId: supplierFilter || undefined,
            sort,
            desc,
          },
        }),
      ])

      setSuppliers(supplierData ?? [])
      setQuotations(quotationData.data ?? [])
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [search, supplierFilter, sort, desc])

  const updateField = ({ target }) => {
    setForm((current) => ({
      ...current,
      [target.name]: target.value,
    }))
  }

  const resetForm = () => {
    setForm(emptyForm)
    setEditingId(null)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()

    try {
      setSaving(true)
      setError('')
      setSuccess('')

      const body = {
        supplierId: Number(form.supplierId),
        materialName: form.materialName.trim(),
        quantity: Number(form.quantity),
        unitPrice: Number(form.unitPrice),
        deliveryDate: new Date(`${form.deliveryDate}T23:59:59`).toISOString(),
      }

      if (editingId) {
        await api.put(`/Quotations/${editingId}`, body)
        setSuccess('Quotation updated successfully.')
      } else {
        await api.post('/Quotations', body)
        setSuccess('Quotation created successfully.')
      }

      resetForm()
      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEdit = (quotation) => {
    setEditingId(quotation.id)
    setForm({
      supplierId: String(quotation.supplierId),
      materialName: quotation.materialName ?? '',
      quantity: String(quotation.quantity ?? ''),
      unitPrice: String(quotation.unitPrice ?? ''),
      deliveryDate: quotation.deliveryDate
        ? new Date(quotation.deliveryDate).toISOString().slice(0, 10)
        : '',
    })
    setError('')
    setSuccess('')
  }

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this quotation?')) return

    try {
      setError('')
      setSuccess('')
      await api.delete(`/Quotations/${id}`)
      setSuccess('Quotation deleted successfully.')
      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  const totalValue = useMemo(
    () => quotations.reduce((sum, item) => sum + Number(item.totalPrice ?? 0), 0),
    [quotations],
  )

  const minDeliveryDate = getTomorrow()

  return (
    <main className="dashboard-content">
      <div className="page-heading">
        <div>
          <span className="eyebrow">PROCUREMENT</span>
          <h1>Supplier Quotations</h1>
          <p className="lead">
            Create, review and manage supplier quotations for construction materials.
          </p>
        </div>
      </div>

      {error && <div className="form-alert" role="alert">{error}</div>}
      {success && <div className="success-alert" role="status">{success}</div>}

      <section className="admin-metrics">
        <article>
          <div className="metric-icon blue">Q</div>
          <div>
            <small>Total quotations</small>
            <strong>{quotations.length}</strong>
          </div>
        </article>

        <article>
          <div className="metric-icon green">LKR</div>
          <div>
            <small>Visible quotation value</small>
            <strong>LKR {formatMoney(totalValue)}</strong>
          </div>
        </article>

        <article>
          <div className="metric-icon amber">S</div>
          <div>
            <small>Active suppliers</small>
            <strong>{suppliers.filter((supplier) => supplier.isActive !== false).length}</strong>
          </div>
        </article>
      </section>

      <section className="admin-panel" style={{ marginBottom: '1.5rem' }}>
        <div className="panel-heading">
          <div>
            <span className="eyebrow">QUOTATION FORM</span>
            <h2>{editingId ? 'Edit quotation' : 'Add quotation'}</h2>
            <p>Total price is calculated by the backend from quantity × unit price.</p>
          </div>
        </div>

        <form className="user-form" onSubmit={handleSubmit}>
          <div className="form-grid">
            <label className="field">
              <span>Supplier</span>
              <select
                name="supplierId"
                value={form.supplierId}
                onChange={updateField}
                required
              >
                <option value="">Select supplier</option>
                {suppliers
                  .filter((supplier) => supplier.isActive !== false)
                  .map((supplier) => (
                    <option key={supplier.id} value={supplier.id}>
                      {supplier.name}
                    </option>
                  ))}
              </select>
            </label>

            <label className="field">
              <span>Material Name</span>
              <input
                name="materialName"
                value={form.materialName}
                onChange={updateField}
                placeholder="e.g. Cement"
                required
              />
            </label>

            <label className="field">
              <span>Quantity</span>
              <input
                name="quantity"
                type="number"
                min="1"
                step="1"
                value={form.quantity}
                onChange={updateField}
                required
              />
            </label>

            <label className="field">
              <span>Unit Price</span>
              <input
                name="unitPrice"
                type="number"
                min="0"
                step="0.01"
                value={form.unitPrice}
                onChange={updateField}
                required
              />
            </label>

            <label className="field">
              <span>Delivery Date</span>
              <input
                name="deliveryDate"
                type="date"
                min={minDeliveryDate}
                value={form.deliveryDate}
                onChange={updateField}
                required
              />
            </label>
          </div>

          <div className="page-actions">
            <button className="button button-primary" type="submit" disabled={saving}>
              {saving
                ? 'Saving...'
                : editingId
                  ? 'Update quotation'
                  : 'Add quotation'}
            </button>

            {editingId && (
              <button className="button button-secondary" type="button" onClick={resetForm}>
                Cancel
              </button>
            )}
          </div>
        </form>
      </section>

      <section className="admin-panel">
        <div className="panel-heading">
          <div>
            <span className="eyebrow">QUOTATION LIST</span>
            <h2>Supplier quotations</h2>
            <p>Search, filter and sort quotations before procurement comparison.</p>
          </div>
        </div>

        <div className="user-form" style={{ paddingBottom: '1rem' }}>
          <div className="form-grid">
            <label className="field">
              <span>Search</span>
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Material or supplier"
              />
            </label>

            <label className="field">
              <span>Supplier Filter</span>
              <select
                value={supplierFilter}
                onChange={(event) => setSupplierFilter(event.target.value)}
              >
                <option value="">All suppliers</option>
                {suppliers.map((supplier) => (
                  <option key={supplier.id} value={supplier.id}>
                    {supplier.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <span>Sort By</span>
              <select value={sort} onChange={(event) => setSort(event.target.value)}>
                <option value="deliveryDate">Delivery date</option>
                <option value="supplier">Supplier</option>
                <option value="material">Material</option>
                <option value="quantity">Quantity</option>
                <option value="unitprice">Unit price</option>
                <option value="totalprice">Total price</option>
              </select>
            </label>

            <label className="field">
              <span>Order</span>
              <select
                value={desc ? 'desc' : 'asc'}
                onChange={(event) => setDesc(event.target.value === 'desc')}
              >
                <option value="asc">Ascending</option>
                <option value="desc">Descending</option>
              </select>
            </label>
          </div>
        </div>

        {loading ? (
          <div className="table-state">
            <span className="spinner" />
            Loading quotations...
          </div>
        ) : quotations.length === 0 ? (
          <div className="table-state">No quotations found.</div>
        ) : (
          <div className="table-scroll">
            <table className="users-table">
              <thead>
                <tr>
                  <th>Supplier</th>
                  <th>Material</th>
                  <th>Quantity</th>
                  <th>Unit Price</th>
                  <th>Total Price</th>
                  <th>Delivery Date</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {quotations.map((quotation) => (
                  <tr key={quotation.id}>
                    <td><strong>{quotation.supplierName}</strong></td>
                    <td>{quotation.materialName}</td>
                    <td>{quotation.quantity}</td>
                    <td>LKR {formatMoney(quotation.unitPrice)}</td>
                    <td><strong>LKR {formatMoney(quotation.totalPrice)}</strong></td>
                    <td className="created-date">{formatDate(quotation.deliveryDate)}</td>
                    <td className="row-actions">
                      <button
                        className="table-action"
                        type="button"
                        onClick={() => handleEdit(quotation)}
                      >
                        Edit
                      </button>
                      <button
                        className="table-action danger"
                        type="button"
                        onClick={() => handleDelete(quotation.id)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  )
}
