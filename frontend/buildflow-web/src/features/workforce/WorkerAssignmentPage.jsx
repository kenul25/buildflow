import { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import './workforce.css'

const emptyForm = {
  workerId: '',
  activityId: '',
  startTime: '',
  endTime: '',
  status: 'Planned',
  notes: '',
}

const statusOptions = [
  'Planned',
  'InProgress',
  'Completed',
  'Cancelled',
]

export default function WorkerAssignmentPage() {
  const [assignments, setAssignments] = useState([])
  const [workers, setWorkers] = useState([])
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
      const [workersResponse, activitiesResponse] = await Promise.all([
        api.get('/workers', {
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

      setWorkers(workersResponse.data.items ?? [])
      setActivities(activitiesResponse.data.items ?? [])
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setOptionsLoading(false)
    }
  }, [])

  const loadAssignments = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = (
        await api.get('/worker-assignments', {
          params: {
            search: search || null,
            status: statusFilter || null,
            page,
            pageSize,
            sort: 'starttime',
            desc: false,
            includeArchived: false,
          },
        })
      ).data

      setAssignments(result.items ?? [])
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
    loadAssignments()
  }, [loadAssignments])

  function workerName(workerId) {
    const worker = workers.find((item) => item.id === workerId)

    if (!worker) return workerId

    return `${worker.employeeCode} — ${worker.fullName}`
  }

  function activityName(activityId) {
    const activity = activities.find((item) => item.id === activityId)

    if (!activity) return activityId

    return activity.name
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
      workerId: item.workerId ?? '',
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

    if (!form.workerId || !form.activityId) {
      setError('Please select a worker and activity.')
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
      workerId: form.workerId,
      activityId: form.activityId,
      startTime: start.toISOString(),
      endTime: end.toISOString(),
      status: form.status,
      notes: form.notes.trim() || null,
    }

    try {
      if (editingId) {
        await api.put(`/worker-assignments/${editingId}`, body)
        setSuccess('Worker assignment updated successfully.')
      } else {
        await api.post('/worker-assignments', body)
        setSuccess('Worker assigned successfully.')
      }

      closeForm()
      await loadAssignments()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleArchive(item) {
    const confirmed = window.confirm(
      `Archive this assignment for ${workerName(item.workerId)}?`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/worker-assignments/${item.id}`)
      setSuccess('Worker assignment archived successfully.')
      await loadAssignments()
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

      case 'InProgress':
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
            Member 04 · Workforce Management
          </span>

          <h1>Worker Assignments</h1>

          <p>
            Assign workers to construction activities and manage schedules.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
          disabled={optionsLoading}
        >
          Assign Worker
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
              loadAssignments()
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
                  ? 'Edit Worker Assignment'
                  : 'Assign Worker'}
              </h2>

              <p>
                Select the worker, activity and assignment time.
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
              Worker
              <select
                name="workerId"
                value={form.workerId}
                onChange={handleChange}
                required
              >
                <option value="">Select worker</option>

                {workers.map((worker) => (
                  <option key={worker.id} value={worker.id}>
                    {worker.employeeCode} — {worker.fullName}
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

                {activities.map((activity) => (
                  <option key={activity.id} value={activity.id}>
                    {activity.name}
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
                placeholder="Optional assignment notes"
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
                    ? 'Update Assignment'
                    : 'Assign Worker'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Assignments
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search worker, employee code, status or notes"
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
            Loading assignments...
          </div>
        ) : assignments.length === 0 ? (
          <div className="workforce-state">
            No worker assignments found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Worker</th>
                  <th>Activity</th>
                  <th>Start</th>
                  <th>End</th>
                  <th>Status</th>
                  <th>Notes</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {assignments.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>
                        {workerName(item.workerId)}
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