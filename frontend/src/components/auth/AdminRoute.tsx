import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'

interface AdminRouteProps {
  children: ReactNode
}

export function AdminRoute({ children }: AdminRouteProps) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const role = useAuthStore((state) => state.role)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (role !== 'Administrator') {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

