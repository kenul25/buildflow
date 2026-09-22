// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import React from 'react'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthContext } from '../context/auth-context.js'
import ProtectedRoute from './ProtectedRoute.jsx'

function renderRoute({ user = null, roles } = {}) {
  render(
    <AuthContext.Provider value={{ user, isLoading: false }}>
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/login" element={<p>Login page</p>} />
          <Route path="/unauthorized" element={<p>Unauthorized page</p>} />
          <Route element={<ProtectedRoute roles={roles} />}>
            <Route path="/protected" element={<p>Protected content</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('ProtectedRoute', () => {
  it('redirects signed-out users to login', () => {
    renderRoute()
    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('blocks users without an allowed role', () => {
    renderRoute({ user: { roles: ['SiteEngineer'] }, roles: ['Administrator'] })
    expect(screen.getByText('Unauthorized page')).toBeInTheDocument()
  })

  it('renders content for an allowed role', () => {
    renderRoute({ user: { roles: ['Administrator'] }, roles: ['Administrator'] })
    expect(screen.getByText('Protected content')).toBeInTheDocument()
  })
})
