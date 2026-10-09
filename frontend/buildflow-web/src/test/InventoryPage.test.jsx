// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import InventoryPage from '../features/inventory/InventoryPage.jsx'
import { inventoryService } from '../features/inventory/inventoryService.js'

afterEach(() => {
  cleanup()
})

const mockUseAuth = vi.fn()
vi.mock('../hooks/useAuth.js', () => ({
  useAuth: () => mockUseAuth()
}))

vi.mock('../features/inventory/inventoryService.js', async (importOriginal) => ({
  ...(await importOriginal()),
  inventoryService: {
    materials: vi.fn(),
    warehouses: vi.fn(),
    alerts: vi.fn(),
    reservations: vi.fn(),
    movements: vi.fn(),
    deleteMaterial: vi.fn(),
    deleteWarehouse: vi.fn(),
    reserve: vi.fn(),
    stock: vi.fn(),
    release: vi.fn(),
    createMaterial: vi.fn(),
    updateMaterial: vi.fn(),
  }
}))

beforeEach(() => {
  vi.resetAllMocks()
  mockUseAuth.mockReturnValue({ user: { roles: ['InventoryOfficer'] } })
  inventoryService.materials.mockResolvedValue({ items: [], total: 0 })
  inventoryService.warehouses.mockResolvedValue({ items: [], total: 0 })
  inventoryService.alerts.mockResolvedValue([])
  inventoryService.reservations.mockResolvedValue({ items: [], total: 0 })
  inventoryService.movements.mockResolvedValue({ items: [], total: 0 })
})

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/inventory']}>
      <Routes>
        <Route path="/inventory" element={<InventoryPage />} />
      </Routes>
    </MemoryRouter>
  )
}

test('shows the inventory empty state and management action', async () => {
  renderPage()
  expect(await screen.findByText('No materials found.')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Add material' })).toBeInTheDocument()
})

test('shows API failure and retry control', async () => {
  inventoryService.materials.mockRejectedValueOnce(new Error('network'))
  renderPage()
  await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
  expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
})

test('happy path data display: mocks valid material records and asserts stock metrics', async () => {
  inventoryService.materials.mockResolvedValue({
    items: [
      {
        id: 'mat-1',
        name: 'Portland Cement',
        category: 'Raw Materials',
        unit: 'bags',
        warehouseName: 'Main Depot',
        currentStock: 150,
        reservedStock: 30,
        availableStock: 120,
      },
    ],
    total: 1,
  })

  renderPage()

  expect(await screen.findByText('Portland Cement')).toBeInTheDocument()
  expect(screen.getByText('150')).toBeInTheDocument()
  expect(screen.getByText('30')).toBeInTheDocument()
  expect(screen.getByText('120')).toBeInTheDocument()
})

test('role-based access control (RBAC): hides management actions for read-only Worker role', async () => {
  mockUseAuth.mockReturnValue({ user: { roles: ['Worker'] } })
  inventoryService.materials.mockResolvedValue({
    items: [
      {
        id: 'mat-1',
        name: 'Portland Cement',
        category: 'Raw Materials',
        unit: 'bags',
        warehouseName: 'Main Depot',
        currentStock: 100,
        reservedStock: 10,
        availableStock: 90,
      },
    ],
    total: 1,
  })

  renderPage()

  expect(await screen.findByText('Portland Cement')).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Add material' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
})

test('search filtering: typing a search term re-queries inventory service with search params', async () => {
  renderPage()
  await screen.findByText('No materials found.')

  const searchInput = screen.getByPlaceholderText('Search materials')
  fireEvent.change(searchInput, { target: { value: 'Cement' } })

  await waitFor(() => {
    expect(inventoryService.materials).toHaveBeenCalledWith(
      expect.objectContaining({ search: 'Cement' })
    )
  })
})

test('low-stock alerts tab: displays critical low-stock warning notifications', async () => {
  inventoryService.alerts.mockResolvedValue([
    {
      materialId: 'mat-1',
      materialName: 'Steel Rebar',
      availableStock: 2,
      unit: 'tons',
      severity: 'Critical',
      message: 'Stock critically low below minimum threshold',
    },
  ])

  renderPage()
  await screen.findByText('No materials found.')

  const alertsTab = screen.getByRole('button', { name: 'alerts' })
  fireEvent.click(alertsTab)

  expect(await screen.findByText('Stock critically low below minimum threshold')).toBeInTheDocument()
  expect(screen.getByText('Critical')).toBeInTheDocument()
  expect(screen.getByText('Steel Rebar')).toBeInTheDocument()
})

test('client-side stock issue validation: prevents API call when issuing more than available stock', async () => {
  inventoryService.materials.mockResolvedValue({
    items: [
      {
        id: 'mat-1',
        name: 'Portland Cement',
        category: 'Raw Materials',
        unit: 'bags',
        warehouseName: 'Main Depot',
        currentStock: 50,
        reservedStock: 10,
        availableStock: 40,
      },
    ],
    total: 1,
  })

  renderPage()
  expect(await screen.findByText('Portland Cement')).toBeInTheDocument()

  const issueBtn = screen.getByRole('button', { name: 'Issue' })
  fireEvent.click(issueBtn)

  const quantityInput = screen.getByRole('spinbutton')
  fireEvent.change(quantityInput, { target: { value: '100' } })

  const saveBtn = screen.getByRole('button', { name: 'Save' })
  fireEvent.click(saveBtn)

  expect(await screen.findByRole('alert')).toHaveTextContent('Only 40 bags is available.')
  expect(inventoryService.stock).not.toHaveBeenCalled()
})

test('stock reservation workflow: completes material reservation and displays confirmation', async () => {
  inventoryService.materials.mockResolvedValue({
    items: [
      {
        id: 'mat-1',
        name: 'Portland Cement',
        category: 'Raw Materials',
        unit: 'bags',
        warehouseName: 'Main Depot',
        currentStock: 100,
        reservedStock: 0,
        availableStock: 100,
      },
    ],
    total: 1,
  })
  inventoryService.reserve.mockResolvedValue({ id: 'res-1' })

  renderPage()
  expect(await screen.findByText('Portland Cement')).toBeInTheDocument()

  const reserveBtn = screen.getByRole('button', { name: 'Reserve' })
  fireEvent.click(reserveBtn)

  const quantityInput = screen.getByRole('spinbutton')
  fireEvent.change(quantityInput, { target: { value: '25' } })

  const saveBtn = screen.getByRole('button', { name: 'Save' })
  fireEvent.click(saveBtn)

  await waitFor(() => {
    expect(inventoryService.reserve).toHaveBeenCalledWith('mat-1', {
      quantity: 25,
      durationMinutes: 60,
    })
  })

  expect(await screen.findByRole('status')).toHaveTextContent('Portland Cement reserved successfully.')
})
