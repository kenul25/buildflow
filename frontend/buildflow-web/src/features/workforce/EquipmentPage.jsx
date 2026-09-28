import { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import './workforce.css'

const emptyForm = {
  equipmentCode: '',
  name: '',
  type: '',
  status: 'Available',
  description: '',
}

const statusOptions = [
  'Available',
  'InUse',
  'Maintenance',
  'Unavailable',
]

export default function EquipmentPage() {
  const [equipment, setEquipment] = useState([])
  const [total, setTotal] = useState(0)

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const [saving, setSaving] = useState(false)

  const pageSize = 10

  const loadEquipment = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = (
        await api.get('/equipment', {
          params: {
            search: search || null,
            status: statusFilter || null,
            page,
            pageSize,
            sort: 'name',
            desc: false,
            includeArchived: false,
          },
        })
      ).data

      setEquipment(result.items ?? [])
      setTotal(result.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, statusFilter, page])

  useEffect(() => {
    loadEquipment()
  }, [loadEquipment])

  function handleChange(event) {
    const { name, value } = event.target

    setForm((current) => ({
      ...current,
      [name]: value,
    }))
  }

  function openCreateForm() {
    setEditingId(null)
    setForm(emptyForm)
    setError('')
    setSuccess('')
    setShowForm(true)
  }

  function openEditForm(item) {
    setEditingId(item.id)

    setForm({
      equipmentCode: item.equipmentCode ?? '',
      name: item.name ?? '',
      type: item.type ?? '',
      status: item.status ?? 'Available',
      description: item.description ?? '',
    })

    setError('')
    setSuccess('')
    setShowForm(true)
  }

  function closeForm() {
    if (saving) return

    setShowForm(false)
    setEditingId(null)
    setForm(emptyForm)
  }

  async function handleSubmit(event) {
    event.preventDefault()

    setSaving(true)
    setError('')
    setSuccess('')

    const body = {
      equipmentCode: form.equipmentCode.trim(),
      name: form.name.trim(),
      type: form.type.trim(),
      status: form.status,
      description: form.description.trim() || null,
    }

    try {
      if (editingId) {
        await api.put(`/equipment/${editingId}`, body)
        setSuccess('Equipment updated successfully.')
      } else {
        await api.post('/equipment', body)
        setSuccess('Equipment created successfully.')
      }

      closeForm()
      await loadEquipment()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleArchive(item) {
    const confirmed = window.confirm(
      `Archive ${item.name} (${item.equipmentCode})?`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/equipment/${item.id}`)
      setSuccess(`${item.name} was archived.`)
      await loadEquipment()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  function statusClass(status) {
    switch (status) {
      case 'Available':
        return 'worker-status active'

      case 'InUse':
        return 'worker-status inactive'

      case 'Maintenance':
        return 'worker-status archived'

      default:
        return 'worker-status archived'
    }
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Equipment Management
          </span>

          <h1>Equipment</h1>

          <p>
            Manage construction equipment, operational status and availability.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
        >
          Add Equipment
        </button>
      </div>

      {success && (
        <div className="success-alert" role="status">
          {success}
        </div>
      )}

      {error && (
        <div className="form-alert" role="alert">
          {error}

          <button type="button" onClick={loadEquipment}>
            Retry
          </button>
        </div>
      )}

      {showForm && (
        <section className="workforce-panel workforce-form-panel">
          <div className="workforce-panel-heading">
            <div>
              <h2>
                {editingId ? 'Edit Equipment' : 'Add Equipment'}
              </h2>

              <p>
                {editingId
                  ? 'Update the equipment information below.'
                  : 'Enter the details for the new equipment.'}
              </p>
            </div>

            <button
              className="workforce-close-button"
              type="button"
              onClick={closeForm}
              disabled={saving}
            >
              ×
            </button>
          </div>

          <form className="workforce-form" onSubmit={handleSubmit}>
            <label>
              Equipment Code
              <input
                name="equipmentCode"
                value={form.equipmentCode}
                onChange={handleChange}
                maxLength={50}
                required
                placeholder="e.g. EQ-001"
              />
            </label>

            <label>
              Equipment Name
              <input
                name="name"
                value={form.name}
                onChange={handleChange}
                maxLength={160}
                required
                placeholder="e.g. Concrete Mixer"
              />
            </label>

            <label>
              Type
              <input
                name="type"
                value={form.type}
                onChange={handleChange}
                maxLength={100}
                required
                placeholder="e.g. Construction Machine"
              />
            </label>

            <label>
              Status
              <select
                name="status"
                value={form.status}
                onChange={handleChange}
                required
              >
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Description
              <textarea
                name="description"
                value={form.description}
                onChange={handleChange}
                maxLength={500}
                rows={4}
                placeholder="Equipment description"
              />
            </label>

            <div className="workforce-form-actions">
              <button
                className="button button-secondary"
                type="button"
                onClick={closeForm}
                disabled={saving}
              >
                Cancel
              </button>

              <button
                className="button button-primary"
                type="submit"
                disabled={saving}
              >
                {saving
                  ? 'Saving...'
                  : editingId
                    ? 'Update Equipment'
                    : 'Create Equipment'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Equipment
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search code, name, type or status"
            />
          </label>

          <label>
            Filter by Status
            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value)
                setPage(1)
              }}
            >
              <option value="">All Statuses</option>

              {statusOptions.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </label>
        </div>

        {loading ? (
          <div className="workforce-state">
            <span className="spinner" />
            Loading equipment...
          </div>
        ) : equipment.length === 0 ? (
          <div className="workforce-state">
            No equipment found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Name</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Description</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {equipment.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.equipmentCode}</strong>
                    </td>

                    <td>{item.name}</td>

                    <td>{item.type}</td>

                    <td>
                      <span className={statusClass(item.status)}>
                        {item.status}
                      </span>
                    </td>

                    <td>
                      {item.description || '—'}
                    </td>

                    <td className="row-actions">
                      {!item.isArchived && (
                        <>
                          <button
                            className="table-action"
                            type="button"
                            onClick={() => openEditForm(item)}
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() => handleArchive(item)}
                          >
                            Archive
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="workforce-pagination">
          <span>{total} total</span>

          <div>
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((current) => current - 1)}
            >
              Previous
            </button>

            <span>
              Page {page}
            </span>

            <button
              type="button"
              disabled={page * pageSize >= total}
              onClick={() => setPage((current) => current + 1)}
            >
              Next
            </button>
          </div>
        </div>
      </section>
    </main>
  )
}