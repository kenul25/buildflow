// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, expect, test, vi } from 'vitest'
import { ConstructionListPage } from './ConstructionPage.jsx'
import { constructionService } from './constructionService.js'

vi.mock('../../hooks/useAuth.js', () => ({ useAuth: () => ({ user: { roles: ['ProjectManager'] } }) }))
vi.mock('./constructionService.js', async (importOriginal) => ({ ...(await importOriginal()), constructionService: { list: vi.fn(), archive: vi.fn() } }))

beforeEach(() => vi.clearAllMocks())

test('shows an empty state and create action', async () => {
  constructionService.list.mockResolvedValue({ items: [], total: 0 })
  render(<MemoryRouter initialEntries={['/construction/projects']}><Routes><Route path="/construction/:kind" element={<ConstructionListPage />} /></Routes></MemoryRouter>)
  expect(await screen.findByText(/No projects found/)).toBeInTheDocument()
  expect(screen.getByRole('link', { name: /Create project/i })).toHaveAttribute('href', '/construction/projects/new')
})

test('shows API failure and retry control', async () => {
  constructionService.list.mockRejectedValue(new Error('network'))
  render(<MemoryRouter initialEntries={['/construction/sites']}><Routes><Route path="/construction/:kind" element={<ConstructionListPage />} /></Routes></MemoryRouter>)
  await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
  expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
})
