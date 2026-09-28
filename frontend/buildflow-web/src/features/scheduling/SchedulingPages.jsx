import { useCallback, useEffect, useMemo, useState } from 'react'
import { api, apiErrorMessage } from '../../services/api.js'
import '../workforce/workforce.css'

const scheduleStatuses = [
  'Proposed',
  'Scheduled',
  'InProgress',
  'Completed',
  'Cancelled',
]

function toInputDateTime(value) {
  if (!value) return ''

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return ''

  const local = new Date(
    date.getTime() - date.getTimezoneOffset() * 60000,
  )

  return local.toISOString().slice(0, 16)
}

function formatDateTime(value) {
  if (!value) return '—'

  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? String(value)
    : date.toLocaleString()
}

function statusClass(status) {
  switch (status) {
    case 'Approved':
    case 'Scheduled':
    case 'InProgress':
      return 'worker-status active'

    case 'PendingProjectManagerApproval':
    case 'RevisionRequested':
    case 'Proposed':
      return 'worker-status inactive'

    case 'Completed':
    case 'Rejected':
    case 'Cancelled':
      return 'worker-status archived'

    default:
      return 'worker-status inactive'
  }
}

function ErrorMessage({ error, onRetry }) {
  if (!error) return null

  return (
    <div className="form-alert" role="alert">
      {error}
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Retry
        </button>
      )}
    </div>
  )
}

function SuccessMessage({ message }) {
  if (!message) return null

  return (
    <div className="success-alert" role="status">
      {message}
    </div>
  )
}

/* =========================================================
   1. SCHEDULES
========================================================= */

export function SchedulePage() {
  const [schedules, setSchedules] = useState([])
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

  const [form, setForm] = useState({
    activityId: '',
    startTime: '',
    endTime: '',
    status: 'Proposed',
    notes: '',
  })

  const [saving, setSaving] = useState(false)

  const pageSize = 10

  const loadActivities = useCallback(async () => {
    setOptionsLoading(true)

    try {
      const response = await api.get('/activities', {
        params: {
          page: 1,
          pageSize: 100,
          sort: 'name',
          desc: false,
          includeArchived: false,
        },
      })

      setActivities(response.data.items ?? [])
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setOptionsLoading(false)
    }
  }, [])

  const loadSchedules = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const response = await api.get('/schedules', {
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

      setSchedules(response.data.items ?? [])
      setTotal(response.data.total ?? 0)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [search, statusFilter, page])

  useEffect(() => {
    loadActivities()
  }, [loadActivities])

  useEffect(() => {
    loadSchedules()
  }, [loadSchedules])

  function activityName(activityId) {
    const activity = activities.find((item) => item.id === activityId)

    return activity?.name ?? activityId
  }

  function openCreate() {
    setEditingId(null)
    setForm({
      activityId: '',
      startTime: '',
      endTime: '',
      status: 'Proposed',
      notes: '',
    })
    setError('')
    setSuccess('')
    setShowForm(true)
  }

  function openEdit(schedule) {
    setEditingId(schedule.id)

    setForm({
      activityId: schedule.activityId ?? '',
      startTime: toInputDateTime(schedule.startTime),
      endTime: toInputDateTime(schedule.endTime),
      status: schedule.status ?? 'Proposed',
      notes: schedule.notes ?? '',
    })

    setError('')
    setSuccess('')
    setShowForm(true)
  }

  function closeForm() {
    if (saving) return

    setShowForm(false)
    setEditingId(null)
  }

  async function handleSubmit(event) {
    event.preventDefault()

    setError('')
    setSuccess('')

    if (!form.activityId) {
      setError('Please select an activity.')
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

    try {
      const body = {
        activityId: form.activityId,
        startTime: start.toISOString(),
        endTime: end.toISOString(),
        status: form.status,
        notes: form.notes.trim() || null,
      }

      if (editingId) {
        await api.put(`/schedules/${editingId}`, body)
        setSuccess('Schedule updated successfully.')
      } else {
        await api.post('/schedules', body)
        setSuccess('Schedule created successfully.')
      }

      closeForm()
      await loadSchedules()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleArchive(schedule) {
    const confirmed = window.confirm(
      `Archive this schedule for ${activityName(schedule.activityId)}?`,
    )

    if (!confirmed) return

    setError('')
    setSuccess('')

    try {
      await api.delete(`/schedules/${schedule.id}`)
      setSuccess('Schedule archived successfully.')
      await loadSchedules()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    }
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Scheduling Management
          </span>
          <h1>Schedules</h1>
          <p>
            Create and manage proposed construction schedules.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={openCreate}
          disabled={optionsLoading}
        >
          Create Schedule
        </button>
      </div>

      <SuccessMessage message={success} />
      <ErrorMessage error={error} onRetry={loadSchedules} />

      {showForm && (
        <section className="workforce-panel workforce-form-panel">
          <div className="workforce-panel-heading">
            <div>
              <h2>
                {editingId ? 'Edit Schedule' : 'Create Schedule'}
              </h2>
              <p>
                Define the activity and proposed schedule period.
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
              Activity
              <select
                name="activityId"
                value={form.activityId}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    activityId: event.target.value,
                  }))
                }
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
              Status
              <select
                name="status"
                value={form.status}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    status: event.target.value,
                  }))
                }
              >
                {scheduleStatuses.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Start Time
              <input
                type="datetime-local"
                value={form.startTime}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    startTime: event.target.value,
                  }))
                }
                required
              />
            </label>

            <label>
              End Time
              <input
                type="datetime-local"
                value={form.endTime}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    endTime: event.target.value,
                  }))
                }
                required
              />
            </label>

            <label>
              Notes
              <textarea
                value={form.notes}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    notes: event.target.value,
                  }))
                }
                maxLength={500}
                rows={4}
                placeholder="Optional schedule notes"
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
                    ? 'Update Schedule'
                    : 'Create Schedule'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="workforce-panel">
        <div className="workforce-toolbar">
          <label>
            Search Schedules
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search status, approval status or notes"
            />
          </label>

          <label>
            Status
            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value)
                setPage(1)
              }}
            >
              <option value="">All Statuses</option>

              {scheduleStatuses.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </label>
        </div>

        {loading ? (
          <div className="workforce-state">
            Loading schedules...
          </div>
        ) : schedules.length === 0 ? (
          <div className="workforce-state">
            No schedules found.
          </div>
        ) : (
          <div className="table-scroll">
            <table className="users-table workforce-table">
              <thead>
                <tr>
                  <th>Activity</th>
                  <th>Start</th>
                  <th>End</th>
                  <th>Status</th>
                  <th>Approval</th>
                  <th>Notes</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {schedules.map((schedule) => (
                  <tr key={schedule.id}>
                    <td>
                      <strong>
                        {activityName(schedule.activityId)}
                      </strong>
                    </td>

                    <td>
                      {formatDateTime(schedule.startTime)}
                    </td>

                    <td>
                      {formatDateTime(schedule.endTime)}
                    </td>

                    <td>
                      <span className={statusClass(schedule.status)}>
                        {schedule.status}
                      </span>
                    </td>

                    <td>
                      <span
                        className={statusClass(
                          schedule.approvalStatus,
                        )}
                      >
                        {schedule.approvalStatus}
                      </span>
                    </td>

                    <td>{schedule.notes || '—'}</td>

                    <td className="row-actions">
                      {!schedule.isArchived && (
                        <>
                          <button
                            className="table-action"
                            type="button"
                            onClick={() => openEdit(schedule)}
                          >
                            Edit
                          </button>

                          <button
                            className="table-action danger"
                            type="button"
                            onClick={() => handleArchive(schedule)}
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

/* =========================================================
   SHARED RESOURCE LOADING
========================================================= */

function useSchedulingResources() {
  const [workers, setWorkers] = useState([])
  const [assignments, setAssignments] = useState([])
  const [equipment, setEquipment] = useState([])
  const [reservations, setReservations] = useState([])

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const [
        workersResponse,
        assignmentsResponse,
        equipmentResponse,
        reservationsResponse,
      ] = await Promise.all([
        api.get('/workers', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'name',
            desc: false,
            includeArchived: false,
          },
        }),

        api.get('/worker-assignments', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'starttime',
            desc: false,
            includeArchived: false,
          },
        }),

        api.get('/equipment', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'name',
            desc: false,
            includeArchived: false,
          },
        }),

        api.get('/equipment-reservations', {
          params: {
            page: 1,
            pageSize: 100,
            sort: 'starttime',
            desc: false,
          },
        }),
      ])

      setWorkers(workersResponse.data.items ?? [])
      setAssignments(assignmentsResponse.data.items ?? [])
      setEquipment(equipmentResponse.data.items ?? [])
      setReservations(reservationsResponse.data.items ?? [])
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  return {
    workers,
    assignments,
    equipment,
    reservations,
    loading,
    error,
    reload: load,
  }
}

function overlaps(firstStart, firstEnd, secondStart, secondEnd) {
  return (
    new Date(firstStart) < new Date(secondEnd) &&
    new Date(firstEnd) > new Date(secondStart)
  )
}

/* =========================================================
   2. AVAILABILITY / CONFLICTS
========================================================= */

export function AvailabilityConflictPage() {
  const {
    workers,
    assignments,
    equipment,
    reservations,
    loading,
    error,
    reload,
  } = useSchedulingResources()

  const workerConflictIds = useMemo(() => {
    const result = new Set()

    for (const assignment of assignments) {
      const conflict = assignments.some(
        (other) =>
          other.id !== assignment.id &&
          other.workerId === assignment.workerId &&
          other.status !== 'Completed' &&
          other.status !== 'Cancelled' &&
          assignment.status !== 'Completed' &&
          assignment.status !== 'Cancelled' &&
          overlaps(
            assignment.startTime,
            assignment.endTime,
            other.startTime,
            other.endTime,
          ),
      )

      if (conflict) result.add(assignment.workerId)
    }

    return result
  }, [assignments])

  const equipmentConflictIds = useMemo(() => {
    const result = new Set()

    for (const reservation of reservations) {
      const conflict = reservations.some(
        (other) =>
          other.id !== reservation.id &&
          other.equipmentId === reservation.equipmentId &&
          other.status !== 'Completed' &&
          other.status !== 'Cancelled' &&
          reservation.status !== 'Completed' &&
          reservation.status !== 'Cancelled' &&
          overlaps(
            reservation.startTime,
            reservation.endTime,
            other.startTime,
            other.endTime,
          ),
      )

      if (conflict) result.add(reservation.equipmentId)
    }

    return result
  }, [reservations])

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Scheduling Validation
          </span>
          <h1>Availability & Conflicts</h1>
          <p>
            Review worker availability and equipment booking conflicts.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={reload}
          disabled={loading}
        >
          Refresh
        </button>
      </div>

      <ErrorMessage error={error} onRetry={reload} />

      {loading ? (
        <div className="workforce-state">
          Loading availability...
        </div>
      ) : (
        <>
          <section
            style={{
              display: 'grid',
              gridTemplateColumns:
                'repeat(auto-fit, minmax(210px, 1fr))',
              gap: '1rem',
              marginBottom: '1rem',
            }}
          >
            <div className="workforce-panel" style={{ padding: '1.2rem' }}>
              <strong>Total Workers</strong>
              <h2>{workers.length}</h2>
            </div>

            <div className="workforce-panel" style={{ padding: '1.2rem' }}>
              <strong>Worker Conflicts</strong>
              <h2>{workerConflictIds.size}</h2>
            </div>

            <div className="workforce-panel" style={{ padding: '1.2rem' }}>
              <strong>Total Equipment</strong>
              <h2>{equipment.length}</h2>
            </div>

            <div className="workforce-panel" style={{ padding: '1.2rem' }}>
              <strong>Equipment Conflicts</strong>
              <h2>{equipmentConflictIds.size}</h2>
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Worker Availability</h2>
                <p>
                  Active workers with overlapping assignment detection.
                </p>
              </div>
            </div>

            <div className="table-scroll">
              <table className="users-table workforce-table">
                <thead>
                  <tr>
                    <th>Employee Code</th>
                    <th>Worker</th>
                    <th>Role</th>
                    <th>Active</th>
                    <th>Conflict</th>
                  </tr>
                </thead>

                <tbody>
                  {workers.map((worker) => (
                    <tr key={worker.id}>
                      <td>
                        <strong>{worker.employeeCode}</strong>
                      </td>

                      <td>{worker.fullName}</td>

                      <td>{worker.role}</td>

                      <td>
                        <span
                          className={
                            worker.isActive
                              ? 'worker-status active'
                              : 'worker-status archived'
                          }
                        >
                          {worker.isActive ? 'Available' : 'Inactive'}
                        </span>
                      </td>

                      <td>
                        <span
                          className={
                            workerConflictIds.has(worker.id)
                              ? 'worker-status archived'
                              : 'worker-status active'
                          }
                        >
                          {workerConflictIds.has(worker.id)
                            ? 'Conflict'
                            : 'No Conflict'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Equipment Availability</h2>
                <p>
                  Operational equipment and double-booking detection.
                </p>
              </div>
            </div>

            <div className="table-scroll">
              <table className="users-table workforce-table">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Booking Conflict</th>
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
                        <span
                          className={statusClass(item.status)}
                        >
                          {item.status}
                        </span>
                      </td>

                      <td>
                        <span
                          className={
                            equipmentConflictIds.has(item.id)
                              ? 'worker-status archived'
                              : 'worker-status active'
                          }
                        >
                          {equipmentConflictIds.has(item.id)
                            ? 'Conflict'
                            : 'No Conflict'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </main>
  )
}

/* =========================================================
   3. AI PROPOSAL
========================================================= */

function useWorkflowSelector() {
  const [requests, setRequests] = useState([])
  const [workflow, setWorkflow] = useState(null)
  const [selectedRequestId, setSelectedRequestId] = useState('')

  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const loadRequests = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const response = await api.get('/construction/resource-requests')

      const data = response.data ?? []

      setRequests(data)

      const firstWithWorkflow = data.find((item) => item.workflowId)

      if (firstWithWorkflow) {
        setSelectedRequestId(firstWithWorkflow.id)
      }
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadRequests()
  }, [loadRequests])

  const selectedRequest = requests.find(
    (request) => request.id === selectedRequestId,
  )

  const loadWorkflow = useCallback(async (workflowId) => {
    if (!workflowId) {
      setWorkflow(null)
      return
    }

    setBusy(true)
    setError('')

    try {
      const response = await api.get(
        `/construction/planning-workflows/${workflowId}`,
      )

      setWorkflow(response.data)
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setBusy(false)
    }
  }, [])

  useEffect(() => {
    if (selectedRequest?.workflowId) {
      loadWorkflow(selectedRequest.workflowId)
    } else {
      setWorkflow(null)
    }
  }, [selectedRequest?.workflowId, loadWorkflow])

  async function startPlanning() {
    if (!selectedRequest) {
      setError('Select a resource request first.')
      return
    }

    setBusy(true)
    setError('')
    setSuccess('')

    try {
      const response = await api.post(
        `/construction/resource-requests/${selectedRequest.id}/planning`,
      )

      setWorkflow(response.data)
      setSuccess('Planning workflow loaded successfully.')

      await loadRequests()
    } catch (cause) {
      setError(apiErrorMessage(cause))
    } finally {
      setBusy(false)
    }
  }

  return {
    requests,
    selectedRequestId,
    setSelectedRequestId,
    selectedRequest,
    workflow,
    loading,
    busy,
    error,
    success,
    loadRequests,
    loadWorkflow,
    startPlanning,
  }
}

function WorkflowSelector({
  requests,
  value,
  onChange,
}) {
  return (
    <div className="workforce-toolbar">
      <label>
        Resource Request
        <select
          value={value}
          onChange={(event) => onChange(event.target.value)}
        >
          <option value="">Select resource request</option>

          {requests.map((request) => (
            <option key={request.id} value={request.id}>
              {request.objective}
              {request.workflowStatus
                ? ` — ${request.workflowStatus}`
                : ''}
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}

function getSchedulingTask(workflow) {
  const tasks = workflow?.plan?.tasks

  if (!Array.isArray(tasks)) return null

  return (
    tasks.find(
      (task) =>
        task?.agent === 'SchedulingValidationAgent',
    ) ?? null
  )
}

export function AIProposalPage() {
  const workflowData = useWorkflowSelector()

  const schedulingTask = getSchedulingTask(
    workflowData.workflow,
  )

  const output = schedulingTask?.output
  const proposal = output?.proposal

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · AI Scheduling
          </span>
          <h1>AI Proposal</h1>
          <p>
            Review the Scheduling & Validation Agent proposal.
          </p>
        </div>

        <button
          className="button button-primary"
          type="button"
          onClick={workflowData.startPlanning}
          disabled={workflowData.busy || workflowData.loading}
        >
          {workflowData.busy
            ? 'Loading...'
            : 'Generate Proposal'}
        </button>
      </div>

      <SuccessMessage message={workflowData.success} />
      <ErrorMessage
        error={workflowData.error}
        onRetry={workflowData.loadRequests}
      />

      <WorkflowSelector
        requests={workflowData.requests}
        value={workflowData.selectedRequestId}
        onChange={workflowData.setSelectedRequestId}
      />

      {workflowData.loading ? (
        <div className="workforce-state">
          Loading planning requests...
        </div>
      ) : !workflowData.workflow ? (
        <div className="workforce-state">
          Select a resource request with a workflow.
        </div>
      ) : (
        <>
          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Workflow</h2>
                <p>{workflowData.workflow.id}</p>
              </div>

              <span
                className={statusClass(
                  workflowData.workflow.status,
                )}
              >
                {workflowData.workflow.status}
              </span>
            </div>

            <div style={{ padding: '1.4rem' }}>
              <p>
                <strong>Resource Request:</strong>{' '}
                {workflowData.workflow.resourceRequestId}
              </p>

              {workflowData.workflow.error && (
                <p>
                  <strong>Error:</strong>{' '}
                  {workflowData.workflow.error}
                </p>
              )}
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Scheduling Agent</h2>
                <p>
                  Proposed schedule and selected resources.
                </p>
              </div>

              {schedulingTask && (
                <span
                  className={statusClass(
                    schedulingTask.status,
                  )}
                >
                  {schedulingTask.status}
                </span>
              )}
            </div>

            {!schedulingTask ? (
              <div className="workforce-state">
                Scheduling agent task is not available yet.
              </div>
            ) : !output ? (
              <div className="workforce-state">
                Scheduling agent output is not available yet.
              </div>
            ) : (
              <div style={{ padding: '1.4rem' }}>
                <p>
                  <strong>Accepted:</strong>{' '}
                  {output.accepted ? 'Yes' : 'No'}
                </p>

                {proposal && (
                  <>
                    <p>
                      <strong>Activity:</strong>{' '}
                      {proposal.activityId}
                    </p>

                    <p>
                      <strong>Start:</strong>{' '}
                      {formatDateTime(proposal.startTime)}
                    </p>

                    <p>
                      <strong>End:</strong>{' '}
                      {formatDateTime(proposal.endTime)}
                    </p>

                    <h3>Workers</h3>

                    {proposal.workerIds?.length ? (
                      <ul>
                        {proposal.workerIds.map((id) => (
                          <li key={id}>{id}</li>
                        ))}
                      </ul>
                    ) : (
                      <p>No workers proposed.</p>
                    )}

                    <h3>Equipment</h3>

                    {proposal.equipmentIds?.length ? (
                      <ul>
                        {proposal.equipmentIds.map((id) => (
                          <li key={id}>{id}</li>
                        ))}
                      </ul>
                    ) : (
                      <p>No equipment proposed.</p>
                    )}

                    {proposal.procurementCost != null && (
                      <p>
                        <strong>Procurement Cost:</strong>{' '}
                        {proposal.procurementCost}
                      </p>
                    )}
                  </>
                )}
              </div>
            )}
          </section>
        </>
      )}
    </main>
  )
}

/* =========================================================
   4. VALIDATION
========================================================= */

function ValidationBlock({ title, value, tone = 'normal' }) {
  return (
    <div
      className="workforce-panel"
      style={{
        padding: '1.2rem',
        borderLeft:
          tone === 'success'
            ? '4px solid #22c55e'
            : tone === 'error'
              ? '4px solid #ef4444'
              : '4px solid #cbd5e1',
      }}
    >
      <h3 style={{ marginTop: 0 }}>{title}</h3>

      <div style={{ whiteSpace: 'pre-wrap' }}>
        {value}
      </div>
    </div>
  )
}

export function ValidationPage() {
  const workflowData = useWorkflowSelector()

  const schedulingTask = getSchedulingTask(
    workflowData.workflow,
  )

  const output = schedulingTask?.output
  const backendValidation =
    workflowData.workflow?.plan?.backendValidation

  const validationErrors =
    output?.validationErrors ??
    output?.validation_errors ??
    []

  const risks = output?.risks ?? []

  let validationState = 'Pending'

  if (backendValidation?.accepted === true) {
    validationState = 'Valid'
  } else if (
    backendValidation?.accepted === false ||
    output?.accepted === false
  ) {
    validationState = 'Invalid'
  }

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Deterministic Validation
          </span>
          <h1>Validation</h1>
          <p>
            Review AI results and application-level validation.
          </p>
        </div>
      </div>

      <ErrorMessage
        error={workflowData.error}
        onRetry={workflowData.loadRequests}
      />

      <WorkflowSelector
        requests={workflowData.requests}
        value={workflowData.selectedRequestId}
        onChange={workflowData.setSelectedRequestId}
      />

      {workflowData.loading ? (
        <div className="workforce-state">
          Loading validation data...
        </div>
      ) : !workflowData.workflow ? (
        <div className="workforce-state">
          Select a resource request first.
        </div>
      ) : (
        <>
          <section
            style={{
              display: 'grid',
              gridTemplateColumns:
                'repeat(auto-fit, minmax(220px, 1fr))',
              gap: '1rem',
            }}
          >
            <ValidationBlock
              title="Overall Validation"
              value={validationState}
              tone={
                validationState === 'Valid'
                  ? 'success'
                  : validationState === 'Invalid'
                    ? 'error'
                    : 'normal'
              }
            />

            <ValidationBlock
              title="Backend Deterministic Validation"
              value={
                backendValidation
                  ? backendValidation.accepted
                    ? 'Accepted'
                    : 'Rejected'
                  : 'Not completed'
              }
              tone={
                backendValidation?.accepted
                  ? 'success'
                  : backendValidation
                    ? 'error'
                    : 'normal'
              }
            />

            <ValidationBlock
              title="Agent Recommendation"
              value={
                output
                  ? output.accepted
                    ? 'Accepted by agent recommendation'
                    : 'Rejected by agent recommendation'
                  : 'No agent output'
              }
            />
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Validation Errors</h2>
                <p>
                  Deterministic and agent-reported validation issues.
                </p>
              </div>
            </div>

            <div style={{ padding: '1.4rem' }}>
              {validationErrors.length === 0 ? (
                <p>No validation errors reported.</p>
              ) : (
                <ul>
                  {validationErrors.map((error, index) => (
                    <li key={`${error}-${index}`}>
                      {error}
                    </li>
                  ))}
                </ul>
              )}

              {backendValidation?.errors?.length > 0 && (
                <>
                  <h3>Backend Errors</h3>
                  <ul>
                    {backendValidation.errors.map(
                      (error, index) => (
                        <li key={`${error}-${index}`}>
                          {error}
                        </li>
                      ),
                    )}
                  </ul>
                </>
              )}
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Risks</h2>
                <p>
                  Risks returned by the Scheduling & Validation Agent.
                </p>
              </div>
            </div>

            <div style={{ padding: '1.4rem' }}>
              {risks.length === 0 ? (
                <p>No risks reported.</p>
              ) : (
                <ul>
                  {risks.map((risk, index) => (
                    <li key={`${risk}-${index}`}>
                      {risk}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Approval Readiness</h2>
                <p>
                  PM approval should happen only after deterministic
                  validation succeeds.
                </p>
              </div>
            </div>

            <div style={{ padding: '1.4rem' }}>
              <span
                className={statusClass(
                  workflowData.workflow.status,
                )}
              >
                {workflowData.workflow.status}
              </span>
            </div>
          </section>
        </>
      )}
    </main>
  )
}

/* =========================================================
   5. PROJECT MANAGER APPROVAL
========================================================= */

export function PMApprovalPage() {
  const workflowData = useWorkflowSelector()
  const [decisionBusy, setDecisionBusy] = useState(false)
  const [decisionError, setDecisionError] = useState('')
  const [decisionSuccess, setDecisionSuccess] = useState('')

  async function decide(action) {
    if (!workflowData.workflow) {
      setDecisionError('Select a workflow first.')
      return
    }

    setDecisionBusy(true)
    setDecisionError('')
    setDecisionSuccess('')

    try {
      const workflowId = workflowData.workflow.id

      let endpoint = ''

      if (action === 'approve') {
        endpoint = `/construction/planning-workflows/${workflowId}/approve`
      }

      if (action === 'reject') {
        endpoint = `/construction/planning-workflows/${workflowId}/reject`
      }

      if (action === 'revision') {
        endpoint = `/construction/planning-workflows/${workflowId}/request-revision`
      }

      const response = await api.post(endpoint)

      setDecisionSuccess(
        action === 'approve'
          ? 'Workflow approved successfully.'
          : action === 'reject'
            ? 'Workflow rejected successfully.'
            : 'Revision requested successfully.',
      )

      await workflowData.loadRequests()

      if (response.data?.id) {
        await workflowData.loadWorkflow(response.data.id)
      }
    } catch (cause) {
      setDecisionError(apiErrorMessage(cause))
    } finally {
      setDecisionBusy(false)
    }
  }

  const currentStatus = workflowData.workflow?.status

  const canDecide =
    currentStatus === 'PendingProjectManagerApproval'

  return (
    <main className="dashboard-content workforce-page">
      <div className="workforce-heading">
        <div>
          <span className="eyebrow">
            Member 04 · Human Approval
          </span>
          <h1>Project Manager Approval</h1>
          <p>
            Review the validated plan and make the final workflow decision.
          </p>
        </div>
      </div>

      <SuccessMessage message={decisionSuccess} />

      <ErrorMessage
        error={decisionError || workflowData.error}
        onRetry={workflowData.loadRequests}
      />

      <WorkflowSelector
        requests={workflowData.requests}
        value={workflowData.selectedRequestId}
        onChange={workflowData.setSelectedRequestId}
      />

      {workflowData.loading ? (
        <div className="workforce-state">
          Loading approval workflows...
        </div>
      ) : !workflowData.workflow ? (
        <div className="workforce-state">
          Select a resource request with a planning workflow.
        </div>
      ) : (
        <>
          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Workflow Status</h2>
                <p>
                  {workflowData.workflow.id}
                </p>
              </div>

              <span
                className={statusClass(currentStatus)}
              >
                {currentStatus}
              </span>
            </div>

            <div style={{ padding: '1.4rem' }}>
              <p>
                <strong>Created:</strong>{' '}
                {formatDateTime(
                  workflowData.workflow.createdAt,
                )}
              </p>

              {workflowData.workflow.completedAt && (
                <p>
                  <strong>Completed:</strong>{' '}
                  {formatDateTime(
                    workflowData.workflow.completedAt,
                  )}
                </p>
              )}

              {workflowData.workflow.plan?.backendValidation && (
                <p>
                  <strong>Backend Validation:</strong>{' '}
                  {workflowData.workflow.plan.backendValidation.accepted
                    ? 'Passed'
                    : 'Failed'}
                </p>
              )}
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>Proposed Schedule</h2>
                <p>
                  Review before approving.
                </p>
              </div>
            </div>

            <div style={{ padding: '1.4rem' }}>
              {(() => {
                const task = getSchedulingTask(
                  workflowData.workflow,
                )

                const proposal = task?.output?.proposal

                if (!proposal) {
                  return (
                    <p>
                      No scheduling proposal available yet.
                    </p>
                  )
                }

                return (
                  <>
                    <p>
                      <strong>Activity:</strong>{' '}
                      {proposal.activityId}
                    </p>

                    <p>
                      <strong>Start:</strong>{' '}
                      {formatDateTime(proposal.startTime)}
                    </p>

                    <p>
                      <strong>End:</strong>{' '}
                      {formatDateTime(proposal.endTime)}
                    </p>

                    <p>
                      <strong>Workers:</strong>{' '}
                      {proposal.workerIds?.length ?? 0}
                    </p>

                    <p>
                      <strong>Equipment:</strong>{' '}
                      {proposal.equipmentIds?.length ?? 0}
                    </p>
                  </>
                )
              })()}
            </div>
          </section>

          <section className="workforce-panel">
            <div className="workforce-panel-heading">
              <div>
                <h2>PM Decision</h2>
                <p>
                  No execution, reservation or assignment should happen
                  before approval.
                </p>
              </div>
            </div>

            <div
              style={{
                display: 'flex',
                gap: '0.75rem',
                flexWrap: 'wrap',
                padding: '1.4rem',
              }}
            >
              <button
                className="button button-primary"
                type="button"
                disabled={!canDecide || decisionBusy}
                onClick={() => decide('approve')}
              >
                {decisionBusy
                  ? 'Processing...'
                  : 'Approve'}
              </button>

              <button
                className="button button-secondary"
                type="button"
                disabled={!canDecide || decisionBusy}
                onClick={() => decide('reject')}
              >
                Reject
              </button>

              <button
                className="button button-secondary"
                type="button"
                disabled={!canDecide || decisionBusy}
                onClick={() => decide('revision')}
              >
                Request Revision
              </button>
            </div>

            {!canDecide && (
              <div style={{ padding: '0 1.4rem 1.4rem' }}>
                <p>
                  Approval actions become available only when the
                  workflow status is{' '}
                  <strong>
                    PendingProjectManagerApproval
                  </strong>.
                </p>
              </div>
            )}
          </section>
        </>
      )}
    </main>
  )
}