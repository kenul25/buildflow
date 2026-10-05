// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { fireEvent, render, screen, waitFor, cleanup } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import SchedulingPage from '../features/scheduling/SchedulingPage.jsx'
import WorkflowsPage from '../features/scheduling/WorkflowsPage.jsx'
import { api } from '../services/api.js'

const auth = vi.hoisted(() => ({ user: { roles: ['ProjectManager'] } }))
vi.mock('../hooks/useAuth.js', () => ({ useAuth: () => auth }))
vi.mock('../services/api.js', () => ({ api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }, apiErrorMessage: error => error.message }))
vi.mock('../services/lookups.js', () => ({ allRows: vi.fn().mockResolvedValue([]) }))
afterEach(cleanup)
beforeEach(() => { vi.clearAllMocks(); auth.user.roles = ['ProjectManager']; api.get.mockResolvedValue({ data: { items: [], total: 0 } }); api.post.mockResolvedValue({ data: {} }) })

test('manager can create a skill using the scheduling CRUD form', async () => {
  render(<SchedulingPage />)
  await screen.findByText('No records found.')
  fireEvent.click(screen.getByRole('button', { name: 'Skills', exact: true }))
  await waitFor(() => expect(api.get).toHaveBeenCalledWith('/scheduling/skills', expect.any(Object)))
  fireEvent.click(screen.getByRole('button', { name: 'Add record' }))
  fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Masonry' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save', exact: true }))
  await waitFor(() => expect(api.post).toHaveBeenCalledWith('/scheduling/skills', expect.objectContaining({ name: 'Masonry' })))
})

test('site engineer sees assignments and cannot create workers', async () => {
  auth.user.roles = ['SiteEngineer']
  render(<SchedulingPage />)
  await screen.findByText('No records found.')
  expect(screen.queryByRole('button', { name: 'Workers', exact: true })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Add record' })).not.toBeInTheDocument()
})

test('workflow approval requires a reason and sends the chosen decision', async () => {
  const workflow = { id: 'plan', resourceRequestId: 'request', status: 'PendingProjectManagerApproval', plan: { tasks: [] } }
  api.get.mockImplementation(path => Promise.resolve({ data: path === '/workflows' ? [{ id: 'plan', objective: 'Build masonry', status: workflow.status }] : path === '/workflows/plan' ? workflow : { objective: 'Build masonry', items: [], budgetLimit: 100 } }))
  api.post.mockResolvedValue({ data: { ...workflow, status: 'Approved' } })
  vi.spyOn(globalThis, 'confirm').mockReturnValue(true)
  render(<WorkflowsPage />)
  fireEvent.click(await screen.findByRole('button', { name: 'Review' }))
  const approve = await screen.findByRole('button', { name: 'Approve', exact: true })
  expect(approve).toBeDisabled()
  fireEvent.change(screen.getByLabelText('Decision reason'), { target: { value: 'Resources verified' } })
  fireEvent.click(approve)
  await waitFor(() => expect(api.post).toHaveBeenCalledWith('/workflows/plan/decision', { decision: 'Approved', reason: 'Resources verified' }, { timeout: 150000 }))
})

test('expired proposal requires a future start and submits the selected time for validation', async () => {
  const workflow = { id: 'plan', resourceRequestId: 'request', status: 'PendingProjectManagerApproval', plan: { tasks: [{ task_id: 'schedule', agent: 'SchedulingValidationAgent', status: 'Completed', output: { startTime: '2020-01-01T08:00:00Z', endTime: '2020-01-01T09:00:00Z', workers: [], equipment: [], validation: { isValid: true } } }] } }
  api.get.mockImplementation(path => Promise.resolve({ data: path === '/workflows' ? [{ id: 'plan', objective: 'Build masonry', status: workflow.status }] : path === '/workflows/plan' ? workflow : { objective: 'Build masonry', items: [] } }))
  api.post.mockResolvedValue({ data: { ...workflow, status: 'Approved' } })
  vi.spyOn(globalThis, 'confirm').mockReturnValue(true)
  render(<WorkflowsPage />)
  fireEvent.click(await screen.findByRole('button', { name: 'Review' }))
  const approve = await screen.findByRole('button', { name: 'Approve', exact: true })
  fireEvent.change(screen.getByLabelText('Decision reason'), { target: { value: 'Use the updated time' } })
  expect(approve).toBeDisabled()
  expect(screen.getByRole('button', { name: 'Reject', exact: true })).toBeEnabled()
  const selected = '2099-01-01T10:00'
  fireEvent.change(screen.getByLabelText('Schedule start'), { target: { value: selected } })
  expect(approve).toBeEnabled()
  fireEvent.click(approve)
  await waitFor(() => expect(api.post).toHaveBeenCalledWith('/workflows/plan/decision', { decision: 'Approved', reason: 'Use the updated time', scheduleStart: new Date(selected).toISOString() }, { timeout: 150000 }))
})
