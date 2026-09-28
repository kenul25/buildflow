import { useCallback, useEffect, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import './workforce.css'

const emptyForm = {
  workerId: '',
  skillId: '',
  proficiencyLevel: '',
}

const proficiencyOptions = [
  '',
  'Beginner',
  'Intermediate',
  'Advanced',
  'Expert',
]

export default function WorkerSkillPage() {
  const [workerSkills, setWorkerSkills] = useState([])
  const [workers, setWorkers] = useState([])
  const [skills, setSkills] = useState([])

  const [total, setTotal] = useState(0)
  const [search, setSearch] = useState('')
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
      const [workersResponse, skillsResponse] = await Promise.all([
        api.get('/workers', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'name',
            desc: false,
            includeArchived: false,
          },
        }),
        api.get('/skills', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'name',
            desc: false,
            includeArchived: false,
            status: 'active',
          },
        }),
      ])

      setWorkers(workersResponse.data.items ?? [])
      setSkills(skillsResponse.data.items ?? [])
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setOptionsLoading(false)
    }
  }, [])

  const loadWorkerSkills = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const result = (
        await api.get('/worker-skills', {
          params: {
            search: search || null,
            page,
            pageSize,
            sort: 'worker',
            desc: false,
          },
        })
      ).data

      setWorkerSkills(result.items ?? [])
      setTotal(result.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, page])

  useEffect(() => {
    loadOptions()
  }, [loadOptions])

  useEffect(() => {
    loadWorkerSkills()
  }, [loadWorkerSkills])

  function workerName(workerId) {
    const worker = workers.find((item) => item.id === workerId)

    if (!worker) return workerId

    return `${worker.employeeCode} — ${worker.fullName}`
  }

  function skillName(skillId) {
    const skill = skills.find((item) => item.id === skillId)

    if (!skill) return skillId

    return skill.name
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
      skillId: item.skillId ?? '',
      proficiencyLevel: item.proficiencyLevel ?? '',
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

    if (!form.workerId || !form.skillId) {
      setError('Please select a worker and a skill.')
      return
    }

    setSaving(true)
    setError('')
    setSuccess('')

    const body = {
      workerId: form.workerId,
      skillId: form.skillId,
      proficiencyLevel: form.proficiencyLevel.trim() || null,
    }

    try {
      if (editingId) {
        await api.put(`/worker-skills/${editingId}`, body)
        setSuccess('Worker skill updated successfully.')
      } else {
        await api.post('/worker-skills', body)
        setSuccess('Worker skill assigned successfully.')
      }

      closeForm()
      await loadWorkerSkills()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleRemove(item) {
    const worker = workerName(item.workerId)
    const skill = skillName(item.skillId)

    const confirmed = window.confirm(
      `Remove "${skill}" from "${worker}"?`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/worker-skills/${item.id}`)
      setSuccess('Worker skill removed successfully.')
      await loadWorkerSkills()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Workforce Management
          </span>

          <h1>Worker Skills</h1>

          <p>
            Assign skills to workers and manage their proficiency levels.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreateForm}
          disabled={optionsLoading}
        >
          Assign Skill
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
              loadWorkerSkills()
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
                  ? 'Edit Worker Skill'
                  : 'Assign Skill to Worker'}
              </h2>

              <p>
                Select a worker, skill and optional proficiency level.
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
              Skill
              <select
                name="skillId"
                value={form.skillId}
                onChange={handleChange}
                required
              >
                <option value="">Select skill</option>

                {skills.map((skill) => (
                  <option key={skill.id} value={skill.id}>
                    {skill.name}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Proficiency Level
              <select
                name="proficiencyLevel"
                value={form.proficiencyLevel}
                onChange={handleChange}
              >
                {proficiencyOptions.map((level) => (
                  <option key={level} value={level}>
                    {level || 'Not specified'}
                  </option>
                ))}
              </select>
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
                    : 'Assign Skill'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Worker Skills
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search worker, employee code, skill or proficiency"
            />
          </label>
        </div>

        {loading ? (
          <div className="workforce-state">
            <span className="spinner" />
            Loading worker skills...
          </div>
        ) : workerSkills.length === 0 ? (
          <div className="workforce-state">
            No worker skills found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Worker</th>
                  <th>Skill</th>
                  <th>Proficiency</th>
                  <th>Assigned</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {workerSkills.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>
                        {workerName(item.workerId)}
                      </strong>
                    </td>

                    <td>
                      {skillName(item.skillId)}
                    </td>

                    <td>
                      {item.proficiencyLevel || 'Not specified'}
                    </td>

                    <td>
                      {item.createdAt
                        ? new Date(item.createdAt).toLocaleDateString()
                        : '—'}
                    </td>

                    <td className="row-actions">
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
                        onClick={() => handleRemove(item)}
                      >
                        Remove
                      </button>
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