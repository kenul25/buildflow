// @vitest-environment jsdom
import React from 'react'
import '@testing-library/jest-dom/vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import ProcurementRecordsPage from '../features/procurement/ProcurementRecordsPage.jsx'
import { api } from '../services/api.js'
import { allRows } from '../services/lookups.js'
const auth = vi.hoisted(() => ({ user: { roles: ['ProjectManager'] } }))
vi.mock('../hooks/useAuth.js', () => ({ useAuth: () => auth }))
vi.mock('../services/api.js', () => ({ api: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }, apiErrorMessage: error => error.message }))
vi.mock('../services/lookups.js', () => ({ allRows: vi.fn() }))
afterEach(cleanup)
beforeEach(() => {
  vi.clearAllMocks(); auth.user.roles = ['ProjectManager']
  allRows.mockImplementation(path => Promise.resolve(path === '/projects' ? [{ id: 'project', name: 'Project One' }] : path === '/inventory/materials/page' ? [{ id: 'material', name: 'Cement' }] : []))
  api.post.mockResolvedValue({ data: {} }); api.put.mockResolvedValue({ data: {} })
})
test('purchase request form preserves fractional quantities and sends explicit dates and links', async () => {
  render(<ProcurementRecordsPage kind="PurchaseRequests" />)
  await screen.findByText('No records found.')
  fireEvent.click(screen.getByRole('button', { name: 'Create', exact: true }))
  fireEvent.change(screen.getByLabelText('Project'), { target: { value: 'project' } })
  fireEvent.change(screen.getByLabelText('Material'), { target: { value: 'material' } })
  fireEvent.change(screen.getByLabelText('Quantity'), { target: { value: '2.5' } })
  fireEvent.change(screen.getByLabelText('Required by'), { target: { value: '2030-01-02' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save', exact: true }))
  await waitFor(() => expect(api.post).toHaveBeenCalledWith('/PurchaseRequests', expect.objectContaining({ projectId: 'project', materialId: 'material', materialName: 'Cement', quantity: 2.5, requiredByDate: expect.stringMatching(/Z$/) })))
})
test('manager can approve a pending request', async () => {
  const original = allRows.getMockImplementation()
  allRows.mockImplementation(path => path === '/PurchaseRequests' ? Promise.resolve([{ id: 1, materialName: 'Cement', status: 'Pending', quantity: 2.5 }]) : original(path))
  vi.spyOn(globalThis, 'confirm').mockReturnValue(true)
  render(<ProcurementRecordsPage kind="PurchaseRequests" />)
  fireEvent.click(await screen.findByRole('button', { name: 'Approve' }))
  await waitFor(() => expect(api.put).toHaveBeenCalledWith('/PurchaseRequests/1/approve'))
})
test('procurement officer cannot see the manager approval action', async () => {
  auth.user.roles = ['ProcurementOfficer']
  allRows.mockResolvedValue([{ id: 1, materialName: 'Cement', status: 'Pending', quantity: 2.5 }])
  render(<ProcurementRecordsPage kind="PurchaseRequests" />)
  await screen.findByRole('button', { name: 'Edit' })
  expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
})
test('manager can link a historical request without editing financial fields', async () => {
  const original = allRows.getMockImplementation()
  allRows.mockImplementation(path => path === '/PurchaseRequests' ? Promise.resolve([{ id: 1, materialName: 'Cement', status: 'Approved', quantity: 2.5 }]) : original(path))
  render(<ProcurementRecordsPage kind="PurchaseRequests" />)
  fireEvent.click(await screen.findByRole('button', { name: 'Link historical record' }))
  expect(screen.queryByLabelText('Quantity')).not.toBeInTheDocument()
  fireEvent.change(screen.getByLabelText('Project'), { target: { value: 'project' } })
  fireEvent.change(screen.getByLabelText('Material'), { target: { value: 'material' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save', exact: true }))
  await waitFor(() => expect(api.put).toHaveBeenCalledWith('/PurchaseRequests/1/links', expect.objectContaining({ projectId: 'project', materialId: 'material' })))
})
