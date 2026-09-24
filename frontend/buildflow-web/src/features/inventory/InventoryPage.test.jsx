// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, expect, test, vi } from 'vitest'
import InventoryPage from './InventoryPage.jsx'
import { inventoryService } from './inventoryService.js'

vi.mock('../../hooks/useAuth.js', () => ({ useAuth: () => ({ user: { roles: ['InventoryOfficer'] } }) }))
vi.mock('./inventoryService.js', async (importOriginal) => ({ ...(await importOriginal()), inventoryService: {
  materials: vi.fn(), warehouses: vi.fn(), alerts: vi.fn(), reservations: vi.fn(), movements: vi.fn(),
} }))

beforeEach(() => {
  vi.clearAllMocks()
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