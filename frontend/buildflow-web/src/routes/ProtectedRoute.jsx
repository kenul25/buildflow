import React from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'

export default function ProtectedRoute({ roles }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()
  if (isLoading) return <div className="page-state" role="status"><span className="spinner" />Restoring your session…</div>
  if (!user) return <Navigate to="/login" state={{ from: location }} replace />
  if (roles?.length && !roles.some((role) => user.roles.includes(role))) return <Navigate to="/unauthorized" replace />
  return <Outlet />
}
