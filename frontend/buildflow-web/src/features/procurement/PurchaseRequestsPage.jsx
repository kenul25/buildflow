import { useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'

const emptyForm = {
  materialName: '',
  quantity: '',
  requiredByDate: '',
}

export default function PurchaseRequestsPage() {
  const [requests, setRequests] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [editingId, setEditingId] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const loadRequests = async () => {
    try {
      setError('')

      const response = await api.get('/PurchaseRequests')

      const data = response.data

      setRequests(
        Array.isArray(data)
          ? data
          : data?.items ?? [],
      )
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadRequests()
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

      const body = {
        materialName: form.materialName.trim(),
        quantity: Number(form.quantity),
        requiredByDate: form.requiredByDate,
      }

      if (editingId) {
        await api.put(
          `/PurchaseRequests/${editingId}`,
          body,
        )
      } else {
        await api.post(
          '/PurchaseRequests',
          body,
        )
      }

      resetForm()
      await loadRequests()
    } catch (err) {
      setError(apiErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEdit = (request) => {
    setEditingId(request.id)

    setForm({
      materialName: request.materialName ?? '',
      quantity: String(request.quantity ?? ''),
      requiredByDate: request.requiredByDate
        ? String(request.requiredByDate).substring(0, 10)
        : '',
    })

    setError('')
  }

  const handleCancel = async (id) => {
    const confirmed = window.confirm(
      'Are you sure you want to cancel this purchase request?',
    )

    if (!confirmed) {
      return
    }

    try {
      setError('')

      await api.delete(
        `/PurchaseRequests/${id}`,
      )

      await loadRequests()
    } catch (err) {
      setError(apiErrorMessage(err))
    }
  }

  const getStatusClass = (status) => {
    if (status === 'Approved') {
      return 'active'
    }

    if (status === 'ConvertedToOrder') {
      return 'active'
    }

    if (status === 'Rejected') {
      return 'inactive'
    }

    if (status === 'Cancelled') {
      return 'inactive'
    }

    return 'pending'
  }

  const totalRequests = requests.length

  const pendingRequests = requests.filter(
    (request) =>
      (request.status ?? 'Pending') === 'Pending',
  ).length

  const approvedRequests = requests.filter(
    (request) =>
      (request.status ?? '') === 'Approved',
  ).length

  const convertedRequests = requests.filter(
    (request) =>
      (request.status ?? '') === 'ConvertedToOrder',
  ).length

  if (loading) {
    return (
      <div className="page-state">
        <div className="spinner" />
        <p>Loading purchase requests...</p>
      </div>
    )
  }

  return (
    <div className="dashboard-content">
      <div className="page-heading">
        <div>
          <span className="eyebrow">
            PROCUREMENT
          </span>

          <h1>
            Purchase Requests
          </h1>

          <p className="lead">
            Create and manage material purchase requests
            for construction procurement.
          </p>
        </div>

        <div className="security-note">
          Procurement workflow
        </div>
      </div>

      {error && (
        <div
          className="form-alert"
          style={{ marginTop: '1.5rem' }}
        >
          {error}
        </div>
      )}

      <div
        className="admin-metrics"
        style={{ marginTop: '2rem' }}
      >
        <article>
          <div className="metric-icon blue">
            PR
          </div>

          <div>
            <small>Total Requests</small>
            <strong>{totalRequests}</strong>
          </div>
        </article>

        <article>
          <div className="metric-icon amber">
            PN
          </div>

          <div>
            <small>Pending</small>
            <strong>{pendingRequests}</strong>
          </div>
        </article>

        <article>
          <div className="metric-icon green">
            AP
          </div>

          <div>
            <small>Approved</small>
            <strong>{approvedRequests}</strong>
          </div>
        </article>

        <article>
          <div className="metric-icon violet">
            PO
          </div>

          <div>
            <small>Converted</small>
            <strong>{convertedRequests}</strong>
          </div>
        </article>
      </div>

      <div
        className="admin-panel"
        style={{ marginTop: '1.5rem' }}
      >
        <div className="panel-heading">
          <div>
            <h2>
              {editingId
                ? 'Edit Purchase Request'
                : 'Create Purchase Request'}
            </h2>

            <p>
              Enter the material requirement and required
              delivery date.
            </p>
          </div>
        </div>

        <form
          onSubmit={handleSubmit}
          className="user-form"
        >
          <div
            style={{
              display: 'grid',
              gridTemplateColumns:
                'repeat(auto-fit, minmax(220px, 1fr))',
              gap: '1rem',
            }}
          >
            <label className="field">
              <span>
                Material Name
              </span>

              <input
                name="materialName"
                value={form.materialName}
                onChange={updateField}
                placeholder="e.g. Cement"
                required
              />
            </label>

            <label className="field">
              <span>
                Quantity
              </span>

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

            <label className="field">
              <span>
                Required By Date
              </span>

              <input
                name="requiredByDate"
                type="date"
                value={form.requiredByDate}
                onChange={updateField}
                required
              />
            </label>
          </div>

          <div className="modal-actions">
            <button
              className="button button-primary"
              type="submit"
              disabled={saving}
            >
              {saving
                ? 'Saving...'
                : editingId
                  ? 'Update Request'
                  : 'Create Request'}
            </button>

            {editingId && (
              <button
                className="button button-secondary"
                type="button"
                onClick={resetForm}
              >
                Cancel Edit
              </button>
            )}
          </div>
        </form>
      </div>

      <div
        className="admin-panel"
        style={{ marginTop: '1.5rem' }}
      >
        <div className="panel-heading">
          <div>
            <h2>
              Purchase Request List
            </h2>

            <p>
              View current procurement requests and
              manage pending requests.
            </p>
          </div>

          <div className="role-chip">
            {totalRequests} requests
          </div>
        </div>

        {requests.length === 0 ? (
          <div className="table-state">
            <div>
              <strong>
                No purchase requests found
              </strong>

              <p>
                Create your first purchase request
                using the form above.
              </p>
            </div>
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table">
              <thead>
                <tr>
                  <th>Request</th>
                  <th>Material</th>
                  <th>Quantity</th>
                  <th>Required By</th>
                  <th>Estimated Cost</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {requests.map((request) => {
                  const status =
                    request.status ?? 'Pending'

                  return (
                    <tr key={request.id}>
                      <td>
                        <div className="table-user">
                          <span className="table-avatar">
                            PR
                          </span>

                          <span>
                            <strong>
                              PR #{request.id}
                            </strong>

                            <small>
                              Purchase Request
                            </small>
                          </span>
                        </div>
                      </td>

                      <td>
                        <strong>
                          {request.materialName ?? '-'}
                        </strong>
                      </td>

                      <td>
                        {request.quantity ?? '-'}
                      </td>

                      <td>
                        {request.requiredByDate
                          ? new Date(
                              request.requiredByDate,
                            ).toLocaleDateString()
                          : '-'}
                      </td>

                      <td>
                        <div>
                          <strong>
                            {request.estimatedTotalCost ??
                              '-'}
                          </strong>

                          <small
                            style={{
                              display: 'block',
                              color: 'var(--muted)',
                              marginTop: '.2rem',
                            }}
                          >
                            Unit:{' '}
                            {request.estimatedUnitPrice ??
                              '-'}
                          </small>
                        </div>
                      </td>

                      <td>
                        <span
                          className={`account-status ${getStatusClass(
                            status,
                          )}`}
                        >
                          {status ===
                          'ConvertedToOrder'
                            ? 'Converted'
                            : status}
                        </span>
                      </td>

                      <td>
                        <span className="created-date">
                          {request.createdAt
                            ? new Date(
                                request.createdAt,
                              ).toLocaleDateString()
                            : '-'}
                        </span>
                      </td>

                      <td>
                        <div className="row-actions">
                          <button
                            className="table-action"
                            type="button"
                            onClick={() =>
                              handleEdit(request)
                            }
                            disabled={
                              status !== 'Pending'
                            }
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() =>
                              handleCancel(
                                request.id,
                              )
                            }
                            disabled={
                              status !== 'Pending'
                            }
                          >
                            Cancel
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}