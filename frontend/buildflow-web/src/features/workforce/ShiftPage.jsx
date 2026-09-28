import { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import './workforce.css'

const emptyForm = {
  name: '',
  startTime: '',
  endTime: '',
}

export default function ShiftPage() {
  const [shifts, setShifts] = useState([])
  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const [saving, setSaving] = useState(false)

  const pageSize = 10

  const loadShifts = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = (
        await api.get('/shifts', {
          params: {
            search: search || null,
            page,
            pageSize,
            sort: 'starttime',
            desc: false,
          },
        })
      ).data

      setShifts(result.items ?? [])
      setTotal(result.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, page])

  useEffect(() => {
    loadShifts()
  }, [loadShifts])

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

  function openEditForm(shift) {
    setEditingId(shift.id)
    setForm({
      name: shift.name ?? '',
      startTime: String(shift.startTime ?? '').slice(0, 5),
      endTime: String(shift.endTime ?? '').slice(0, 5),
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

    if (form.startTime === form.endTime) {
      setError('Shift start time and end time cannot be the same.')
      setSaving(false)
      return
    }

    const body = {
      name: form.name.trim(),
      startTime: form.startTime,
      endTime: form.endTime,
    }

    try {
      if (editingId) {
        await api.put(`/shifts/${editingId}`, body)
        setSuccess('Shift updated successfully.')
      } else {
        await api.post('/shifts', body)
        setSuccess('Shift created successfully.')
      }

      closeForm()
      await loadShifts()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleArchive(shift) {
    const confirmed = window.confirm(
      `Archive ${shift.name}? This shift will no longer appear as active.`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/shifts/${shift.id}`)
      setSuccess(`${shift.name} was archived.`)
      await loadShifts()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  function formatTime(value) {
    if (!value) return '—'
    return String(value).slice(0, 5)
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">Member 04 · Workforce Management</span>
          <h1>Shifts</h1>
          <p>Manage worker shifts and working hours.</p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
        >
          Add Shift
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
          <button type="button" onClick={loadShifts}>
            Retry
          </button>
        </div>
      )}

      {showForm && (
        <section className="workforce-panel workforce-form-panel">
          <div className="workforce-panel-heading">
            <div>
              <h2>{editingId ? 'Edit Shift' : 'Add Shift'}</h2>
              <p>
                {editingId
                  ? 'Update the shift information below.'
                  : 'Enter the details for the new shift.'}
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
              Shift Name
              <input
                name="name"
                value={form.name}
                onChange={handleChange}
                maxLength={100}
                required
                placeholder="e.g. Morning Shift"
              />
            </label>

            <label>
              Start Time
              <input
                type="time"
                name="startTime"
                value={form.startTime}
                onChange={handleChange}
                required
              />
            </label>

            <label>
              End Time
              <input
                type="time"
                name="endTime"
                value={form.endTime}
                onChange={handleChange}
                required
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
                    ? 'Update Shift'
                    : 'Create Shift'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Shifts
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search by shift name"
            />
          </label>
        </div>

        {loading ? (
          <div className="workforce-state">
            <span className="spinner" />
            Loading shifts...
          </div>
        ) : shifts.length === 0 ? (
          <div className="workforce-state">
            No shifts found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Start Time</th>
                  <th>End Time</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {shifts.map((shift) => (
                  <tr key={shift.id}>
                    <td>
                      <strong>{shift.name}</strong>
                    </td>

                    <td>{formatTime(shift.startTime)}</td>

                    <td>{formatTime(shift.endTime)}</td>

                    <td>
                      <span
                        className={
                          shift.isArchived
                            ? 'worker-status archived'
                            : shift.isActive
                              ? 'worker-status active'
                              : 'worker-status inactive'
                        }
                      >
                        {shift.isArchived
                          ? 'Archived'
                          : shift.isActive
                            ? 'Active'
                            : 'Inactive'}
                      </span>
                    </td>

                    <td className="row-actions">
                      {!shift.isArchived && (
                        <>
                          <button
                            className="table-action"
                            type="button"
                            onClick={() => openEditForm(shift)}
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() => handleArchive(shift)}
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