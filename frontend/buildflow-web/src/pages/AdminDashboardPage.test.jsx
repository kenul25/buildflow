// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AdminDashboardPage from './AdminDashboardPage.jsx'
import { adminService } from '../services/adminService.js'

vi.mock('../services/adminService.js', () => ({
  adminService: {
    getUsers: vi.fn(),
    createUser: vi.fn(),
    updateUser: vi.fn(),
    deleteUser: vi.fn(),
  },
}))

const siteEngineer = {
  id: '1',
  fullName: 'Site Engineer',
  email: 'engineer@buildflow.com',
  isActive: true,
  createdAt: '2026-09-23T00:00:00Z',
  roles: ['SiteEngineer'],
}

describe('AdminDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    adminService.getUsers.mockResolvedValue([siteEngineer])
  })

  it('creates a user with an administrator-selected role', async () => {
    adminService.createUser.mockResolvedValue({
      ...siteEngineer,
      id: '2',
      fullName: 'Project Manager',
      email: 'manager@buildflow.com',
      roles: ['ProjectManager'],
    })
    render(<AdminDashboardPage />)

    expect(await screen.findByText('engineer@buildflow.com')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Add new user' }))
    fireEvent.change(screen.getByLabelText('Full name'), { target: { value: 'Project Manager' } })
    fireEvent.change(screen.getByLabelText('Email address'), { target: { value: 'manager@buildflow.com' } })
    fireEvent.change(screen.getByLabelText(/Temporary password/), { target: { value: 'Manager@1234' } })
    fireEvent.change(screen.getByLabelText('Operational role'), { target: { value: 'ProjectManager' } })
    fireEvent.click(screen.getByRole('button', { name: 'Create user' }))

    await waitFor(() => expect(adminService.createUser).toHaveBeenCalledWith({
      fullName: 'Project Manager',
      email: 'manager@buildflow.com',
      password: 'Manager@1234',
      role: 'ProjectManager',
      isActive: true,
    }))
    expect(await screen.findByText('Project Manager was added successfully.')).toBeInTheDocument()
  })
})
