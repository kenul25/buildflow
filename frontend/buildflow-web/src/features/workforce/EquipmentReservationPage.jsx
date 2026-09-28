import { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import './workforce.css'

const emptyForm = {
  equipmentId: '',
  activityId: '',
  startTime: '',
  endTime: '',
  status: 'Planned',
  notes: '',
}

const statusOptions = [
  'Planned',
  'Reserved',
  'InUse',
  'Completed',
  'Cancelled',
]

export default function EquipmentReservationPage() {
  const [reservations, setReservations] = useState([])
  const [equipment, setEquipment] = useState([])
  const [activities, setActivities] = useState([])

  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)

  const [loading, setLoading] = useState(true)
  const [optionsLoading, setOptionsLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const [saving, setSaving] = useState(false)

  const pageSize = 10

  const loadOptions = useCallback(async () => {
    setOptionsLoading(true)

    try {
      const [equipmentResponse, activitiesResponse] = await Promise.all([
        api.get('/equipment', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'name',
            desc: false,
            includeArchived: false,
          },
        }),
        api.get('/activities', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'name',
            desc: false,
            includeArchived: false,
          },
        }),
      ])

      setEquipment(equipmentResponse.data.items ?? [])
      setActivities(activitiesResponse.data.items ?? [])
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setOptionsLoading(false)
    }
  }, [])

  const loadReservations = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = (
        await api.get('/equipment-reservations', {
          params: {
            search: search || null,
            status: statusFilter || null,
            page,
            pageSize,
            sort: 'starttime',
            desc: false,
          },
        })
      ).data

      setReservations(result.items ?? [])
      setTotal(result.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, statusFilter, page])

  useEffect(() => {
    loadOptions()
  }, [loadOptions])

  useEffect(() => {
    loadReservations()
  }, [loadReservations])

  function equipmentName(equipmentId) {
    const item = equipment.find((x) => x.id === equipmentId)

    if (!item) return equipmentId

    return `${item.equipmentCode} — ${item.name}`
  }

  function activityName(activityId) {
    const item = activities.find((x) => x.id === activityId)

    if (!item) return activityId

    return item.name
  }

  function toInputDateTime(value) {
    if (!value) return ''

    const date = new Date(value)

    if (Number.isNaN(date.getTime())) return ''

    const local = new Date(
      date.getTime() - date.getTimezoneOffset() * 60000,
    )

    return local.toISOString().slice(0, 16)
  }

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
      equipmentId: item.equipmentId ?? '',
      activityId: item.activityId ?? '',
      startTime: toInputDateTime(item.startTime),
      endTime: toInputDateTime(item.endTime),
      status: item.status ?? 'Planned',
      notes: item.notes ?? '',
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

    setError('')
    setSuccess('')

    if (!form.equipmentId || !form.activityId) {
      setError('Please select equipment and activity.')
      return
    }

    if (!form.startTime || !form.endTime) {
      setError('Please select start and end times.')
      return
    }

    const start = new Date(form.startTime)
    const end = new Date(form.endTime)

    if (start >= end) {
      setError('Start time must be before end time.')
      return
    }

    setSaving(true)

    const body = {
      equipmentId: form.equipmentId,
      activityId: form.activityId,
      startTime: start.toISOString(),
      endTime: end.toISOString(),
      status: form.status,
      notes: form.notes.trim() || null,
    }

    try {
      if (editingId) {
        await api.put(`/equipment-reservations/${editingId}`, body)
        setSuccess('Equipment reservation updated successfully.')
      } else {
        await api.post('/equipment-reservations', body)
        setSuccess('Equipment reserved successfully.')
      }

      closeForm()
      await loadReservations()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleCancel(item) {
    const confirmed = window.confirm(
      `Cancel reservation for ${equipmentName(item.equipmentId)}?`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/equipment-reservations/${item.id}`)
      setSuccess('Equipment reservation cancelled successfully.')
      await loadReservations()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  function formatDateTime(value) {
    if (!value) return '—'

    return new Date(value).toLocaleString()
  }

  function statusClass(status) {
    switch (status) {
      case 'Planned':
        return 'worker-status inactive'

      case 'Reserved':
        return 'worker-status active'

      case 'InUse':
        return 'worker-status active'

      case 'Completed':
        return 'worker-status archived'

      case 'Cancelled':
        return 'worker-status archived'

      default:
        return 'worker-status inactive'
    }
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Equipment Management
          </span>

          <h1>Equipment Reservations</h1>

          <p>
            Reserve equipment for construction activities and manage booking
            times.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
          disabled={optionsLoading}
        >
          Reserve Equipment
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

          <button
            type="button"
            onClick={() => {
              loadOptions()
              loadReservations()
            }}
          >
            Retry
          </button>
        </div>
      )}

      {showForm && (
        <section className="workforce-panel workforce-form-panel">
          <div className="workforce-panel-heading">
            <div>
              <h2>
                {editingId
                  ? 'Edit Equipment Reservation'
                  : 'Reserve Equipment'}
              </h2>

              <p>
                Select equipment, activity and reservation time.
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
              Equipment
              <select
                name="equipmentId"
                value={form.equipmentId}
                onChange={handleChange}
                required
              >
                <option value="">Select equipment</option>

                {equipment.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.equipmentCode} — {item.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Activity
              <select
                name="activityId"
                value={form.activityId}
                onChange={handleChange}
                required
              >
                <option value="">Select activity</option>

                {activities.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Start Time
              <input
                type="datetime-local"
                name="startTime"
                value={form.startTime}
                onChange={handleChange}
                required
              />
            </label>

            <label>
              End Time
              <input
                type="datetime-local"
                name="endTime"
                value={form.endTime}
                onChange={handleChange}
                required
              />
            </label>

            <label>
              Status
              <select
                name="status"
                value={form.status}
                onChange={handleChange}
              >
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Notes
              <textarea
                name="notes"
                value={form.notes}
                onChange={handleChange}
                maxLength={500}
                rows={4}
                placeholder="Optional reservation notes"
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
                disabled={saving || optionsLoading}
              >
                {saving
                  ? 'Saving...'
                  : editingId
                    ? 'Update Reservation'
                    : 'Reserve Equipment'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Reservations
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search equipment, activity or notes"
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
            Loading reservations...
          </div>
        ) : reservations.length === 0 ? (
          <div className="workforce-state">
            No equipment reservations found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Equipment</th>
                  <th>Activity</th>
                  <th>Start</th>
                  <th>End</th>
                  <th>Status</th>
                  <th>Notes</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {reservations.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>
                        {equipmentName(item.equipmentId)}
                      </strong>
                    </td>

                    <td>
                      {activityName(item.activityId)}
                    </td>

                    <td>
                      {formatDateTime(item.startTime)}
                    </td>

                    <td>
                      {formatDateTime(item.endTime)}
                    </td>

                    <td>
                      <span className={statusClass(item.status)}>
                        {item.status}
                      </span>
                    </td>

                    <td>
                      {item.notes || '—'}
                    </td>

                    <td className="row-actions">
                      {!item.isArchived && (
                        <>
                          <button
                            className="table-action"
                            type="button"
                            onClick={() => openEditForm(item)}
                            disabled={
                              item.status === 'Completed' ||
                              item.status === 'Cancelled'
                            }
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() => handleCancel(item)}
                            disabled={
                              item.status === 'Completed' ||
                              item.status === 'Cancelled'
                            }
                          >
                            Cancel
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

            <span>Page {page}</span>

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