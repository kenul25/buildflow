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
    assignRole: vi.fn(),
  },
}))

const siteEngineer = {
  id: '1',
  fullName: 'Site Engineer',
  email: 'engineer@buildflow.com',
  isActive: true,
  roles: ['SiteEngineer'],
}

describe('AdminDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    adminService.getUsers.mockResolvedValue([siteEngineer])
  })

  it('loads users and assigns an operational role', async () => {
    adminService.assignRole.mockResolvedValue({ ...siteEngineer, roles: ['ProjectManager'] })
    render(<AdminDashboardPage />)

    expect(await screen.findByText('engineer@buildflow.com')).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Role for Site Engineer'), { target: { value: 'ProjectManager' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save role' }))

    await waitFor(() => expect(adminService.assignRole).toHaveBeenCalledWith('1', 'ProjectManager'))
    expect(await screen.findByText('Site Engineer is now assigned as Project Manager.')).toBeInTheDocument()
  })
})
