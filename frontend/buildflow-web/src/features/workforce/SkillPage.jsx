import { useCallback, useEffect, useState } from 'react'
import { apiErrorMessage } from '../../services/api.js'
import { api } from '../../services/api.js'
import '../workforce/workforce.css'

const emptyForm = {
  name: '',
  description: '',
}

export default function SkillPage() {
  const [skills, setSkills] = useState([])
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

  const loadSkills = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = (
        await api.get('/skills', {
          params: {
            search: search || null,
            page,
            pageSize,
            sort: 'name',
            desc: false,
          },
        })
      ).data

      setSkills(result.items ?? [])
      setTotal(result.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, page])

  useEffect(() => {
    loadSkills()
  }, [loadSkills])

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

  function openEditForm(skill) {
    setEditingId(skill.id)
    setForm({
      name: skill.name ?? '',
      description: skill.description ?? '',
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
      name: form.name.trim(),
      description: form.description.trim() || null,
    }

    try {
      if (editingId) {
        await api.put(`/skills/${editingId}`, body)
        setSuccess('Skill updated successfully.')
      } else {
        await api.post('/skills', body)
        setSuccess('Skill created successfully.')
      }

      closeForm()
      await loadSkills()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleArchive(skill) {
    const confirmed = window.confirm(
      `Archive ${skill.name}? This skill will no longer appear as active.`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/skills/${skill.id}`)
      setSuccess(`${skill.name} was archived.`)
      await loadSkills()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">Member 04 · Workforce Management</span>
          <h1>Skills</h1>
          <p>
            Manage the skills that can be assigned to construction workers.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
        >
          Add Skill
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
          <button type="button" onClick={loadSkills}>
            Retry
          </button>
        </div>
      )}

      {showForm && (
        <section className="workforce-panel workforce-form-panel">
          <div className="workforce-panel-heading">
            <div>
              <h2>{editingId ? 'Edit Skill' : 'Add Skill'}</h2>
              <p>
                {editingId
                  ? 'Update the skill information below.'
                  : 'Enter the details for the new skill.'}
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
              Skill Name
              <input
                name="name"
                value={form.name}
                onChange={handleChange}
                maxLength={100}
                required
                placeholder="e.g. Carpentry"
              />
            </label>

            <label>
              Description
              <input
                name="description"
                value={form.description}
                onChange={handleChange}
                maxLength={500}
                placeholder="Describe the skill"
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
                    ? 'Update Skill'
                    : 'Create Skill'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Skills
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search by name or description"
            />
          </label>
        </div>

        {loading ? (
          <div className="workforce-state">
            <span className="spinner" />
            Loading skills...
          </div>
        ) : skills.length === 0 ? (
          <div className="workforce-state">
            No skills found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {skills.map((skill) => (
                  <tr key={skill.id}>
                    <td>
                      <strong>{skill.name}</strong>
                    </td>

                    <td>{skill.description || '—'}</td>

                    <td>
                      <span
                        className={
                          skill.isArchived
                            ? 'worker-status archived'
                            : skill.isActive
                              ? 'worker-status active'
                              : 'worker-status inactive'
                        }
                      >
                        {skill.isArchived
                          ? 'Archived'
                          : skill.isActive
                            ? 'Active'
                            : 'Inactive'}
                      </span>
                    </td>

                    <td className="row-actions">
                      {!skill.isArchived && (
                        <>
                          <button
                            className="table-action"
                            type="button"
                            onClick={() => openEditForm(skill)}
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() => handleArchive(skill)}
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