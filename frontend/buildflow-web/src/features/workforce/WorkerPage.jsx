import { useCallback, useEffect, useState } from 'react'
import { apiErrorMessage } from '../../services/api.js'
import { workforceService } from './workforceService.js'
import './workforce.css'

const emptyForm = {
  employeeCode: '',
  fullName: '',
  phoneNumber: '',
  role: '',
}

export default function WorkerPage() {
  const [workers, setWorkers] = useState([])
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

  const loadWorkers = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = await workforceService.listWorkers({
        search: search || null,
        page,
        pageSize,
        sort: 'name',
        desc: false,
      })

      setWorkers(result.items ?? [])
      setTotal(result.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, page])

  useEffect(() => {
    loadWorkers()
  }, [loadWorkers])

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

  function openEditForm(worker) {
    setEditingId(worker.id)
    setForm({
      employeeCode: worker.employeeCode ?? '',
      fullName: worker.fullName ?? '',
      phoneNumber: worker.phoneNumber ?? '',
      role: worker.role ?? '',
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
      employeeCode: form.employeeCode.trim(),
      fullName: form.fullName.trim(),
      phoneNumber: form.phoneNumber.trim() || null,
      role: form.role.trim(),
    }

    try {
      if (editingId) {
        await workforceService.updateWorker(editingId, body)
        setSuccess('Worker updated successfully.')
      } else {
        await workforceService.createWorker(body)
        setSuccess('Worker created successfully.')
      }

      closeForm()
      await loadWorkers()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleArchive(worker) {
    const confirmed = window.confirm(
      `Archive ${worker.fullName}? This worker will no longer appear in the active list.`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await workforceService.archiveWorker(worker.id)
      setSuccess(`${worker.fullName} was archived.`)
      await loadWorkers()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">Member 04 · Workforce Management</span>
          <h1>Workers</h1>
          <p>
            Manage construction workers and their basic workforce information.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
        >
          Add Worker
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
          <button type="button" onClick={loadWorkers}>
            Retry
          </button>
        </div>
      )}

      {showForm && (
        <section className="workforce-panel workforce-form-panel">
          <div className="workforce-panel-heading">
            <div>
              <h2>{editingId ? 'Edit Worker' : 'Add Worker'}</h2>
              <p>
                {editingId
                  ? 'Update the worker information below.'
                  : 'Enter the details for the new worker.'}
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
              Employee Code
              <input
                name="employeeCode"
                value={form.employeeCode}
                onChange={handleChange}
                maxLength={50}
                required
                placeholder="e.g. W001"
              />
            </label>

            <label>
              Full Name
              <input
                name="fullName"
                value={form.fullName}
                onChange={handleChange}
                maxLength={120}
                required
                placeholder="e.g. Kasun Perera"
              />
            </label>

            <label>
              Phone Number
              <input
                name="phoneNumber"
                value={form.phoneNumber}
                onChange={handleChange}
                maxLength={30}
                placeholder="e.g. 0771234567"
              />
            </label>

            <label>
              Role
              <input
                name="role"
                value={form.role}
                onChange={handleChange}
                maxLength={100}
                required
                placeholder="e.g. Concrete Worker"
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
                    ? 'Update Worker'
                    : 'Create Worker'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Workers
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search by code, name or role"
            />
          </label>
        </div>

        {loading ? (
          <div className="workforce-state">
            <span className="spinner" />
            Loading workers...
          </div>
        ) : workers.length === 0 ? (
          <div className="workforce-state">
            No workers found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Employee Code</th>
                  <th>Name</th>
                  <th>Phone</th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {workers.map((worker) => (
                  <tr key={worker.id}>
                    <td>
                      <strong>{worker.employeeCode}</strong>
                    </td>

                    <td>{worker.fullName}</td>

                    <td>{worker.phoneNumber || '—'}</td>

                    <td>{worker.role}</td>

                    <td>
                      <span
                        className={
                          worker.isArchived
                            ? 'worker-status archived'
                            : worker.isActive
                              ? 'worker-status active'
                              : 'worker-status inactive'
                        }
                      >
                        {worker.isArchived
                          ? 'Archived'
                          : worker.isActive
                            ? 'Active'
                            : 'Inactive'}
                      </span>
                    </td>

                    <td className="row-actions">
                      {!worker.isArchived && (
                        <>
                          <button
                            className="table-action"
                            type="button"
                            onClick={() => openEditForm(worker)}
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() => handleArchive(worker)}
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
