// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import InventoryPage from './InventoryPage.jsx'
import { inventoryService } from './inventoryService.js'

let roles = ['InventoryOfficer']
vi.mock('../../hooks/useAuth.js', () => ({ useAuth: () => ({ user: { roles } }) }))
vi.mock('./inventoryService.js', async (importOriginal) => ({ ...(await importOriginal()), inventoryService: {
  materials: vi.fn(), warehouses: vi.fn(), alerts: vi.fn(), reservations: vi.fn(),   movements: vi.fn(), reserve: vi.fn(), stock: vi.fn(),
} }))

afterEach(cleanup)

beforeEach(() => {
  vi.clearAllMocks()
  roles = ['InventoryOfficer']
  inventoryService.materials.mockResolvedValue({ items: [], total: 0 })
  inventoryService.warehouses.mockResolvedValue({ items: [], total: 0 })
  inventoryService.alerts.mockResolvedValue([])
  inventoryService.reservations.mockResolvedValue({ items: [], total: 0 })
  inventoryService.movements.mockResolvedValue({ items: [], total: 0 })
})

function renderPage() { return render(<MemoryRouter initialEntries={['/inventory']}><Routes><Route path="/inventory" element={<InventoryPage />} /></Routes></MemoryRouter>) }

test('shows the inventory empty state and management action', async () => {
  renderPage()
  expect(await screen.findByText('No materials found.')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Add material' })).toBeInTheDocument()
})

test('shows API failure and retry control', async () => {
  inventoryService.materials.mockRejectedValue(new Error('network'))
  renderPage()
  await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
  expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
})

const cement = { id: 'm1', name: 'Cement', category: 'Binder', unit: 'bag', warehouseName: 'Main', currentStock: 100, reservedStock: 20, availableStock: 80 }

test('lists materials with current, reserved and available stock', async () => {
  inventoryService.materials.mockResolvedValue({ items: [cement], total: 1 })
  renderPage()
  expect(await screen.findByText('Cement')).toBeInTheDocument()
  expect(screen.getByText('80')).toBeInTheDocument()
  expect(screen.getByText('1 total')).toBeInTheDocument()
})

test('hides management actions for read-only roles', async () => {
  roles = ['Worker']
  inventoryService.materials.mockResolvedValue({ items: [cement], total: 1 })
  renderPage()
  expect(await screen.findByText('Cement')).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Add material' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
})

test('searching reloads materials with the search term', async () => {
  renderPage()
  await screen.findByText('No materials found.')
  fireEvent.change(screen.getByLabelText('Search'), { target: { value: 'cem' } })
  await waitFor(() => expect(inventoryService.materials).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'cem', page: 1 })))
})

test('switching to the alerts tab shows low-stock alerts', async () => {
  inventoryService.alerts.mockResolvedValue([{ materialId: 'm1', materialName: 'Cement', availableStock: 2, unit: 'bag', severity: 'Critical', message: 'Below threshold' }])
  renderPage()
  await screen.findByText('No materials found.')
  fireEvent.click(screen.getByRole('button', { name: 'alerts' }))
  expect(await screen.findByText('Below threshold')).toBeInTheDocument()
  expect(screen.getByText('Critical')).toBeInTheDocument()
})

test('issue dialog rejects a quantity above available stock', async () => {
  inventoryService.materials.mockResolvedValue({ items: [cement], total: 1 })
  renderPage()
  fireEvent.click(await screen.findByRole('button', { name: 'Issue' }))
  fireEvent.change(screen.getByLabelText(/Quantity/), { target: { value: '999' } })
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
  expect(await screen.findByText('Only 80 bag is available.')).toBeInTheDocument()
  expect(inventoryService.stock).not.toHaveBeenCalled()
})

test('reserving material calls the service and shows a success message', async () => {
  inventoryService.materials.mockResolvedValue({ items: [cement], total: 1 })
  inventoryService.reserve.mockResolvedValue({})
  renderPage()
  fireEvent.click(await screen.findByRole('button', { name: 'Reserve' }))
  fireEvent.click(screen.getByRole('button', { name: 'Save' }))
  expect(await screen.findByText('Cement reserved successfully.')).toBeInTheDocument()
  expect(inventoryService.reserve).toHaveBeenCalledWith('m1', { quantity: 1, durationMinutes: 60 })
})