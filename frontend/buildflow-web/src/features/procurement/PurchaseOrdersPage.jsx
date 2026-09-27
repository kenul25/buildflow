import { useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'

const emptyForm = {
  purchaseRequestId: '',
  supplierId: '',
  unitPrice: '',
  deliveryDate: '',
}

export default function PurchaseOrdersPage() {
  const [orders, setOrders] = useState([])
  const [purchaseRequests, setPurchaseRequests] = useState([])
  const [suppliers, setSuppliers] = useState([])

  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState(null)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadData = async () => {
    try {
      setError('')

      const [
        purchaseOrderResponse,
        purchaseRequestResponse,
        supplierResponse,
      ] = await Promise.all([
        api.get('/PurchaseOrders'),
        api.get('/PurchaseRequests'),
        api.get('/Suppliers'),
      ])

      const orderData = purchaseOrderResponse.data
      const requestData = purchaseRequestResponse.data
      const supplierData = supplierResponse.data

      setOrders(
        Array.isArray(orderData)
          ? orderData
          : orderData?.items ?? [],
      )

      setPurchaseRequests(
        Array.isArray(requestData)
          ? requestData
          : requestData?.items ?? [],
      )

      setSuppliers(
        Array.isArray(supplierData)
          ? supplierData
          : supplierData?.items ?? [],
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

  const handlePurchaseRequestChange = (event) => {
    const purchaseRequestId = event.target.value

    setForm((current) => ({
      ...current,
      purchaseRequestId,
    }))
  }

  const resetForm = () => {
    setForm(emptyForm)
    setEditingId(null)
    setError('')
  }

  const handleSubmit = async (event) => {
    event.preventDefault()

    try {
      setSaving(true)
      setError('')

      const body = {
        purchaseRequestId: Number(form.purchaseRequestId),
        supplierId: Number(form.supplierId),
        unitPrice: Number(form.unitPrice),
        deliveryDate: form.deliveryDate,
      }

      if (editingId) {
        await api.put(
          `/PurchaseOrders/${editingId}`,
          body,
        )
      } else {
        await api.post(
          '/PurchaseOrders',
          body,
        )
      }

      resetForm()
      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEdit = (order) => {
    setEditingId(order.id)

    setForm({
      purchaseRequestId:
        String(order.purchaseRequestId ?? ''),
      supplierId:
        String(order.supplierId ?? ''),
      unitPrice:
        order.unitPrice != null
          ? String(order.unitPrice)
          : '',
      deliveryDate:
        order.deliveryDate
          ? String(order.deliveryDate).substring(0, 10)
          : '',
    })

    setError('')
  }

  const handleCancel = async (id) => {
    const confirmed = window.confirm(
      'Are you sure you want to cancel this purchase order?',
    )

    if (!confirmed) {
      return
    }

    try {
      setError('')

      await api.delete(
        `/PurchaseOrders/${id}`,
      )

      await loadData()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  const getSupplierName = (supplierId) => {
    const supplier = suppliers.find(
      (item) =>
        String(item.id) === String(supplierId),
    )

    return supplier?.name ?? `Supplier #${supplierId}`
  }

  const getRequest = (purchaseRequestId) => {
    return purchaseRequests.find(
      (request) =>
        String(request.id) ===
        String(purchaseRequestId),
    )
  }

  const formatCurrency = (value) => {
    if (value == null || value === '') {
      return '-'
    }

    const number = Number(value)

    if (Number.isNaN(number)) {
      return value
    }

    return number.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
  }

  const formatDate = (value) => {
    if (!value) {
      return '-'
    }

    return new Date(value).toLocaleDateString()
  }

  const totalOrders = orders.length

  const pendingOrders = orders.filter(
    (order) =>
      order.status === 'Pending',
  ).length

  const completedOrders = orders.filter(
    (order) =>
      order.status === 'Completed',
  ).length

  const cancelledOrders = orders.filter(
    (order) =>
      order.status === 'Cancelled',
  ).length

  const getStatusClass = (status) => {
    switch (status) {
      case 'Pending':
        return 'po-status po-status-pending'

      case 'Approved':
        return 'po-status po-status-approved'

      case 'Sent':
        return 'po-status po-status-sent'

      case 'PartiallyReceived':
        return 'po-status po-status-partial'

      case 'Completed':
        return 'po-status po-status-completed'

      case 'Cancelled':
        return 'po-status po-status-cancelled'

      default:
        return 'po-status'
    }
  }

  if (loading) {
    return (
      <div className="page po-page">
        <div className="po-loading">
          <div className="po-loading-spinner" />
          <p>Loading purchase orders...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="page po-page">

      {/* =========================
          PAGE HEADER
      ========================== */}

      <div className="po-page-header">
        <div>
          <p className="po-eyebrow">
            PROCUREMENT
          </p>

          <h1>
            Purchase Orders
          </h1>

          <p className="po-page-description">
            Create and manage purchase orders
            generated from approved purchase requests.
          </p>
        </div>

        <div className="po-workflow-badge">
          <span className="po-workflow-dot" />
          Procurement workflow
        </div>
      </div>

      {/* =========================
          ERROR
      ========================== */}

      {error && (
        <div className="po-alert">
          <strong>Error</strong>
          <span>{error}</span>
        </div>
      )}

      {/* =========================
          STAT CARDS
      ========================== */}

      <div className="po-stats-grid">

        <div className="po-stat-card">
          <div className="po-stat-icon po-stat-blue">
            PO
          </div>

          <div className="po-stat-content">
            <span>Total Orders</span>
            <strong>{totalOrders}</strong>
          </div>
        </div>

        <div className="po-stat-card">
          <div className="po-stat-icon po-stat-yellow">
            PN
          </div>

          <div className="po-stat-content">
            <span>Pending</span>
            <strong>{pendingOrders}</strong>
          </div>
        </div>

        <div className="po-stat-card">
          <div className="po-stat-icon po-stat-green">
            CO
          </div>

          <div className="po-stat-content">
            <span>Completed</span>
            <strong>{completedOrders}</strong>
          </div>
        </div>

        <div className="po-stat-card">
          <div className="po-stat-icon po-stat-purple">
            CA
          </div>

          <div className="po-stat-content">
            <span>Cancelled</span>
            <strong>{cancelledOrders}</strong>
          </div>
        </div>

      </div>

      {/* =========================
          CREATE / EDIT CARD
      ========================== */}

      <div className="po-form-card">

        <div className="po-form-header">
          <div>
            <h2>
              {editingId
                ? 'Edit Purchase Order'
                : 'Create Purchase Order'}
            </h2>

            <p>
              {editingId
                ? 'Update the supplier, unit price or delivery date for this purchase order.'
                : 'Select an approved purchase request and enter the supplier order details.'}
            </p>
          </div>
        </div>

        <div className="po-form-body">

          <form onSubmit={handleSubmit}>

            <div className="po-form-grid">

              {/* Purchase Request */}

              <label className="po-field">
                <span>
                  Purchase Request
                </span>

                <select
                  name="purchaseRequestId"
                  value={
                    form.purchaseRequestId
                  }
                  onChange={
                    handlePurchaseRequestChange
                  }
                  required
                  disabled={
                    Boolean(editingId)
                  }
                >
                  <option value="">
                    Select purchase request
                  </option>

                  {purchaseRequests
                    .filter(
                      (request) =>
                        request.status ===
                          'Approved' ||
                        String(
                          request.id,
                        ) ===
                          String(
                            form.purchaseRequestId,
                          ),
                    )
                    .map((request) => (
                      <option
                        key={request.id}
                        value={request.id}
                      >
                        PR #{request.id} -{' '}
                        {request.materialName}
                      </option>
                    ))}
                </select>
              </label>

              {/* Supplier */}

              <label className="po-field">
                <span>
                  Supplier
                </span>

                <select
                  name="supplierId"
                  value={
                    form.supplierId
                  }
                  onChange={updateField}
                  required
                >
                  <option value="">
                    Select supplier
                  </option>

                  {suppliers
                    .filter(
                      (supplier) =>
                        supplier.isActive !==
                        false,
                    )
                    .map((supplier) => (
                      <option
                        key={supplier.id}
                        value={supplier.id}
                      >
                        {supplier.name}
                      </option>
                    ))}
                </select>
              </label>

              {/* Unit Price */}

              <label className="po-field">
                <span>
                  Unit Price
                </span>

                <input
                  name="unitPrice"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={
                    form.unitPrice
                  }
                  onChange={updateField}
                  placeholder="Enter unit price"
                  required
                />
              </label>

              {/* Delivery Date */}

              <label className="po-field">
                <span>
                  Delivery Date
                </span>

                <input
                  name="deliveryDate"
                  type="date"
                  value={
                    form.deliveryDate
                  }
                  onChange={updateField}
                  required
                />
              </label>

            </div>

            {/* PURCHASE REQUEST INFORMATION */}

            {form.purchaseRequestId && (
              <div className="po-request-preview">

                {(() => {
                  const request =
                    getRequest(
                      form.purchaseRequestId,
                    )

                  if (!request) {
                    return null
                  }

                  return (
                    <>
                      <div className="po-request-preview-title">
                        Purchase Request Details
                      </div>

                      <div className="po-request-details">

                        <div>
                          <span>
                            Material
                          </span>

                          <strong>
                            {request.materialName ??
                              '-'}
                          </strong>
                        </div>

                        <div>
                          <span>
                            Quantity
                          </span>

                          <strong>
                            {request.quantity ??
                              '-'}
                          </strong>
                        </div>

                        <div>
                          <span>
                            Required By
                          </span>

                          <strong>
                            {formatDate(
                              request.requiredByDate,
                            )}
                          </strong>
                        </div>

                        <div>
                          <span>
                            Status
                          </span>

                          <strong>
                            {request.status ??
                              '-'}
                          </strong>
                        </div>

                      </div>
                    </>
                  )
                })()}

              </div>
            )}

            {/* ACTIONS */}

            <div className="po-form-actions">

              <button
                className="po-primary-button"
                type="submit"
                disabled={saving}
              >
                {saving
                  ? 'Saving...'
                  : editingId
                    ? 'Update Purchase Order'
                    : 'Create Purchase Order'}
              </button>

              {editingId && (
                <button
                  className="po-secondary-button"
                  type="button"
                  onClick={resetForm}
                >
                  Cancel
                </button>
              )}

            </div>

          </form>

        </div>
      </div>

      {/* =========================
          ORDER MANAGEMENT
      ========================== */}

      <div className="po-orders-card">

        <div className="po-orders-header">

          <div>
            <p className="po-eyebrow">
              ORDER MANAGEMENT
            </p>

            <h2>
              Purchase Orders
            </h2>

            <p>
              Review supplier orders and delivery information.
            </p>
          </div>

          <div className="po-order-count">
            {totalOrders} Orders
          </div>

        </div>

        {orders.length === 0 ? (

          <div className="po-empty">
            <div className="po-empty-icon">
              PO
            </div>

            <h3>
              No purchase orders found
            </h3>

            <p>
              Create a purchase order from an approved purchase request.
            </p>
          </div>

        ) : (

          <div className="po-order-list">

            {orders.map((order) => {

              const request =
                getRequest(
                  order.purchaseRequestId,
                )

              const isPending =
                order.status === 'Pending'

              const isCancelled =
                order.status === 'Cancelled'

              const isCompleted =
                order.status === 'Completed'

              return (
                <div
                  key={order.id}
                  className="po-order-item"
                >

                  {/* ORDER MAIN */}

                  <div className="po-order-main">

                    <div className="po-order-title-row">

                      <div>
                        <span className="po-order-label">
                          PURCHASE ORDER
                        </span>

                        <h3>
                          PO #{order.id}
                        </h3>
                      </div>

                      <span
                        className={getStatusClass(
                          order.status,
                        )}
                      >
                        {order.status ??
                          'Pending'}
                      </span>

                    </div>

                    <div className="po-order-grid">

                      <div className="po-detail">
                        <span>
                          Purchase Request
                        </span>

                        <strong>
                          PR #
                          {order.purchaseRequestId ??
                            '-'}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Supplier
                        </span>

                        <strong>
                          {getSupplierName(
                            order.supplierId,
                          )}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Material
                        </span>

                        <strong>
                          {order.materialName ??
                            request?.materialName ??
                            '-'}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Quantity
                        </span>

                        <strong>
                          {order.quantity ??
                            request?.quantity ??
                            '-'}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Unit Price
                        </span>

                        <strong>
                          {formatCurrency(
                            order.unitPrice,
                          )}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Total Cost
                        </span>

                        <strong>
                          {formatCurrency(
                            order.totalCost,
                          )}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Delivery Date
                        </span>

                        <strong>
                          {formatDate(
                            order.deliveryDate,
                          )}
                        </strong>
                      </div>

                      <div className="po-detail">
                        <span>
                          Created
                        </span>

                        <strong>
                          {formatDate(
                            order.createdAt,
                          )}
                        </strong>
                      </div>

                    </div>

                  </div>

                  {/* ACTIONS */}

                  <div className="po-order-actions">

                    <button
                      className="po-action-button"
                      type="button"
                      onClick={() =>
                        handleEdit(order)
                      }
                      disabled={
                        !isPending
                      }
                    >
                      Edit
                    </button>

                    <button
                      className="po-action-button po-action-danger"
                      type="button"
                      onClick={() =>
                        handleCancel(
                          order.id,
                        )
                      }
                      disabled={
                        !isPending ||
                        isCancelled ||
                        isCompleted
                      }
                    >
                      Cancel
                    </button>

                  </div>

                </div>
              )
            })}

          </div>
        )}

      </div>

    </div>
  )
}