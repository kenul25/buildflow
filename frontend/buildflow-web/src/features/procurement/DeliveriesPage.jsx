import { useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'

const emptyForm = {
  purchaseOrderId: '',
  quantity: '',
  deliveryDate: '',
  evidenceUrl: '',
}

const statuses = ['Pending', 'Received', 'Completed', 'Rejected']

const statusClass = (status) => {
  switch (status) {
    case 'Completed':
      return 'delivery-status status-completed'

    case 'Received':
      return 'delivery-status status-received'

    case 'Rejected':
      return 'delivery-status status-rejected'

    case 'Cancelled':
      return 'delivery-status status-cancelled'

    default:
      return 'delivery-status status-pending'
  }
}

export default function DeliveriesPage() {
  const [deliveries, setDeliveries] = useState([])
  const [purchaseOrders, setPurchaseOrders] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadData = async () => {
    try {
      setError('')

      const deliveryResponse = await api.get('/Deliveries')
      const purchaseOrderResponse = await api.get('/PurchaseOrders')

      const deliveryData = deliveryResponse.data
      const purchaseOrderData = purchaseOrderResponse.data

      setDeliveries(
        Array.isArray(deliveryData)
          ? deliveryData
          : deliveryData?.items ?? [],
      )

      setPurchaseOrders(
        Array.isArray(purchaseOrderData)
          ? purchaseOrderData
          : purchaseOrderData?.items ?? [],
      )
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [])

  const updateField = (event) => {
    const { name, value } = event.target

    setForm((current) => ({
      ...current,
      [name]: value,
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

      if (editingId) {
        await api.put(`/Deliveries/${editingId}`, {
          quantity: Number(form.quantity),
          deliveryDate: form.deliveryDate,
          evidenceUrl: form.evidenceUrl || null,
        })
      } else {
        await api.post('/Deliveries', {
          purchaseOrderId: Number(form.purchaseOrderId),
          quantity: Number(form.quantity),
          deliveryDate: form.deliveryDate,
          evidenceUrl: form.evidenceUrl || null,
        })
      }

      resetForm()
      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEdit = (delivery) => {
    setEditingId(delivery.id)

    setForm({
      purchaseOrderId: String(delivery.purchaseOrderId ?? ''),
      quantity: String(delivery.quantity ?? ''),
      deliveryDate: delivery.deliveryDate
        ? String(delivery.deliveryDate).substring(0, 10)
        : '',
      evidenceUrl: delivery.evidenceUrl ?? '',
    })

    setError('')
  }

  const handleStatusChange = async (id, status) => {
    try {
      setError('')

      await api.put(
        `/Deliveries/${id}/status?status=${encodeURIComponent(status)}`,
      )

      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  const handleCancel = async (id) => {
    const confirmed = window.confirm(
      'Are you sure you want to cancel this delivery?',
    )

    if (!confirmed) {
      return
    }

    try {
      setError('')

      await api.delete(`/Deliveries/${id}`)

      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  if (loading) {
    return (
      <div className="page">
        <div className="delivery-loading">
          <div className="delivery-spinner"></div>
          <p>Loading deliveries...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="page deliveries-page">

      {/* PAGE HEADER */}
      <div className="delivery-page-header">
        <div>
          <p className="eyebrow">PROCUREMENT</p>

          <h1>Deliveries</h1>

          <p className="delivery-subtitle">
            Manage purchase order deliveries, track delivery status,
            and maintain delivery evidence.
          </p>
        </div>

        <div className="delivery-header-summary">
          <div className="delivery-summary-item">
            <span>Total Deliveries</span>
            <strong>{deliveries.length}</strong>
          </div>

          <div className="delivery-summary-item">
            <span>Pending</span>
            <strong>
              {
                deliveries.filter(
                  (delivery) => delivery.status === 'Pending',
                ).length
              }
            </strong>
          </div>

          <div className="delivery-summary-item">
            <span>Completed</span>
            <strong>
              {
                deliveries.filter(
                  (delivery) => delivery.status === 'Completed',
                ).length
              }
            </strong>
          </div>
        </div>
      </div>

      {/* ERROR */}
      {error && (
        <div className="delivery-alert">
          <strong>Error</strong>
          <span>{error}</span>
        </div>
      )}

      {/* CREATE / EDIT FORM */}
      <div className="delivery-card delivery-form-card">

        <div className="delivery-card-header">
          <div>
            <h2>
              {editingId ? 'Edit Delivery' : 'Create Delivery'}
            </h2>

            <p>
              {editingId
                ? 'Update the selected delivery information.'
                : 'Create a delivery against an existing purchase order.'}
            </p>
          </div>

          {editingId && (
            <button
              className="button button-ghost"
              type="button"
              onClick={resetForm}
            >
              Cancel Edit
            </button>
          )}
        </div>

        <form onSubmit={handleSubmit}>

          <div className="delivery-form-grid">

            {/* PURCHASE ORDER */}
            <label className="delivery-field">
              <span>Purchase Order</span>

              <select
                name="purchaseOrderId"
                value={form.purchaseOrderId}
                onChange={updateField}
                required
                disabled={Boolean(editingId)}
              >
                <option value="">
                  Select purchase order
                </option>

                {purchaseOrders.map((purchaseOrder) => (
                  <option
                    key={purchaseOrder.id}
                    value={purchaseOrder.id}
                  >
                    PO #{purchaseOrder.id} -{' '}
                    {purchaseOrder.materialName ?? 'Purchase Order'}
                  </option>
                ))}
              </select>
            </label>

            {/* QUANTITY */}
            <label className="delivery-field">
              <span>Quantity</span>

              <input
                name="quantity"
                type="number"
                min="1"
                step="1"
                value={form.quantity}
                onChange={updateField}
                placeholder="Enter quantity"
                required
              />
            </label>

            {/* DELIVERY DATE */}
            <label className="delivery-field">
              <span>Delivery Date</span>

              <input
                name="deliveryDate"
                type="date"
                value={form.deliveryDate}
                onChange={updateField}
                required
              />
            </label>

            {/* EVIDENCE */}
            <label className="delivery-field">
              <span>Evidence URL</span>

              <input
                name="evidenceUrl"
                type="url"
                value={form.evidenceUrl}
                onChange={updateField}
                placeholder="https://example.com/evidence"
              />
            </label>

          </div>

          <div className="delivery-form-actions">

            <button
              className="button button-primary"
              type="submit"
              disabled={saving}
            >
              {saving
                ? 'Saving...'
                : editingId
                  ? 'Update Delivery'
                  : 'Create Delivery'}
            </button>

            {editingId && (
              <button
                className="button button-secondary"
                type="button"
                onClick={resetForm}
              >
                Reset
              </button>
            )}

          </div>

        </form>
      </div>

      {/* DELIVERY LIST */}
      <div className="delivery-card">

        <div className="delivery-card-header">
          <div>
            <h2>Delivery Records</h2>

            <p>
              View and manage all purchase order deliveries.
            </p>
          </div>
        </div>

        {deliveries.length === 0 ? (
          <div className="delivery-empty">
            <div className="delivery-empty-icon">
              D
            </div>

            <h3>No deliveries found</h3>

            <p>
              Create your first delivery using the form above.
            </p>
          </div>
        ) : (
          <div className="delivery-list">

            {deliveries.map((delivery) => (
              <div
                key={delivery.id}
                className="delivery-row"
              >

                {/* LEFT SIDE */}
                <div className="delivery-main">

                  <div className="delivery-title-row">

                    <div>
                      <span className="delivery-number">
                        Delivery #{delivery.id}
                      </span>

                      <h3>
                        {delivery.materialName ||
                          'Material Delivery'}
                      </h3>
                    </div>

                    <span
                      className={statusClass(
                        delivery.status,
                      )}
                    >
                      {delivery.status}
                    </span>

                  </div>

                  <div className="delivery-details">

                    <div className="delivery-detail">
                      <span>Purchase Order</span>
                      <strong>
                        PO #{delivery.purchaseOrderId}
                      </strong>
                    </div>

                    <div className="delivery-detail">
                      <span>Quantity</span>
                      <strong>
                        {delivery.quantity}
                      </strong>
                    </div>

                    <div className="delivery-detail">
                      <span>Delivery Date</span>
                      <strong>
                        {delivery.deliveryDate
                          ? new Date(
                              delivery.deliveryDate,
                            ).toLocaleDateString()
                          : '-'}
                      </strong>
                    </div>

                  </div>

                  {delivery.evidenceUrl && (
                    <div className="delivery-evidence">

                      <span>Evidence</span>

                      <a
                        href={delivery.evidenceUrl}
                        target="_blank"
                        rel="noreferrer"
                      >
                        View delivery evidence
                      </a>

                    </div>
                  )}

                </div>

                {/* RIGHT SIDE */}
                <div className="delivery-actions">

                  <button
                    className="button button-secondary"
                    type="button"
                    onClick={() =>
                      handleEdit(delivery)
                    }
                    disabled={
                      delivery.status === 'Completed'
                    }
                  >
                    Edit
                  </button>

                  <select
                    className="delivery-status-select"
                    value={delivery.status}
                    onChange={(event) =>
                      handleStatusChange(
                        delivery.id,
                        event.target.value,
                      )
                    }
                  >
                    {statuses.map((status) => (
                      <option
                        key={status}
                        value={status}
                      >
                        {status}
                      </option>
                    ))}
                  </select>

                  <button
                    className="button button-danger"
                    type="button"
                    onClick={() =>
                      handleCancel(delivery.id)
                    }
                    disabled={
                      delivery.status === 'Completed'
                    }
                  >
                    Cancel
                  </button>

                </div>

              </div>
            ))}

          </div>
        )}

      </div>

    </div>
  )
}