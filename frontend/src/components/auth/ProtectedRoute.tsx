import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'
import type { UserRole } from '../../types/auth'

interface ProtectedRouteProps {
  children: ReactNode
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, role } = useAuthStore((state) => ({
    isAuthenticated: state.isAuthenticated,
    role: state.role,
  }))
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

