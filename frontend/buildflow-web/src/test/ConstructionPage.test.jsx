// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { ConstructionListPage } from '../features/construction/ConstructionPage.jsx'
import { constructionService } from '../features/construction/constructionService.js'

vi.mock('../hooks/useAuth.js', () => ({ useAuth: () => ({ user: { roles: ['ProjectManager'] } }) }))
vi.mock('../features/construction/constructionService.js', async (importOriginal) => ({ ...(await importOriginal()), constructionService: { list: vi.fn(), archive: vi.fn() } }))

beforeEach(() => vi.clearAllMocks())
afterEach(() => { cleanup(); vi.restoreAllMocks() })

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

const project = { id: 'project-1', name: 'Disposable test project', updatedAt: '2026-10-05T00:00:00Z' }
function renderProjects() {
  render(<MemoryRouter initialEntries={['/construction/projects']}><Routes><Route path="/construction/:kind" element={<ConstructionListPage />} /></Routes></MemoryRouter>)
}

test('confirmed Delete removes the project and refreshes the list', async () => {
  constructionService.list.mockResolvedValueOnce({ items: [project], total: 1 }).mockResolvedValue({ items: [], total: 0 })
  constructionService.archive.mockResolvedValue({})
  vi.spyOn(window, 'confirm').mockReturnValue(true)
  renderProjects()
  fireEvent.click(await screen.findByRole('button', { name: 'Delete' }))
  await waitFor(() => expect(constructionService.archive).toHaveBeenCalledWith('projects', 'project-1'))
  expect(await screen.findByText(/No projects found/)).toBeInTheDocument()
  expect(screen.getByRole('status')).toHaveTextContent('Disposable test project deleted.')
})

test('cancelled confirmation does not delete the project', async () => {
  constructionService.list.mockResolvedValue({ items: [project], total: 1 })
  vi.spyOn(window, 'confirm').mockReturnValue(false)
  renderProjects()
  fireEvent.click(await screen.findByRole('button', { name: 'Delete' }))
  expect(constructionService.archive).not.toHaveBeenCalled()
  expect(screen.getByText(project.name)).toBeInTheDocument()
})

test('blocked deletion displays the server reason and keeps the project', async () => {
  constructionService.list.mockResolvedValue({ items: [project], total: 1 })
  constructionService.archive.mockRejectedValue({ response: { status: 409, data: { detail: 'Archive active child records first.' } } })
  vi.spyOn(window, 'confirm').mockReturnValue(true)
  renderProjects()
  fireEvent.click(await screen.findByRole('button', { name: 'Delete' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('Archive active child records first.')
  expect(screen.getByText(project.name)).toBeInTheDocument()
})
