import { Component, lazy, Suspense, useEffect, useState } from 'react'
import type { ErrorInfo, ReactNode } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AdminRoute } from './components/auth/AdminRoute'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { Layout } from './components/layout/Layout'
import { Spinner } from './components/ui/Spinner'
import { useAuthStore } from './store/authStore'

class AppErrorBoundary extends Component<{ children: ReactNode }, { error: string | null }> {
  constructor(props: { children: ReactNode }) {
    super(props)
    this.state = { error: null }
  }

  static getDerivedStateFromError(error: Error) {
    return { error: error.message }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[AppErrorBoundary]', error, info.componentStack)
  }

  render() {
    if (this.state.error) {
      return (
        <div className="flex min-h-screen flex-col items-center justify-center gap-6 bg-parchment p-8 text-center">
          <p className="font-heading text-2xl text-crimson">Something went wrong</p>
          <p className="max-w-md text-sm text-ink/70">
            The app encountered an unexpected error. Please refresh the page or contact support.
          </p>
          <pre className="max-w-xl overflow-auto rounded-2xl border border-gold/30 bg-white/60 p-4 text-left text-xs text-ink/80">
            {this.state.error}
          </pre>
          <button
            type="button"
            className="rounded-full bg-crimson px-6 py-2 text-sm font-bold text-white transition hover:opacity-90"
            onClick={() => window.location.reload()}
          >
            Reload page
          </button>
        </div>
      )
    }
    return this.props.children
  }
}

const HomePage = lazy(() => import('./pages/HomePage'))
const ProductsPage = lazy(() => import('./pages/ProductsPage'))
const ProductDetailPage = lazy(() => import('./pages/ProductDetailPage'))
const LoginPage = lazy(() => import('./pages/LoginPage'))
const RegisterPage = lazy(() => import('./pages/RegisterPage'))
const CartPage = lazy(() => import('./pages/CartPage'))
const CheckoutPage = lazy(() => import('./pages/CheckoutPage'))
const OrderConfirmationPage = lazy(() => import('./pages/OrderConfirmationPage'))
const OrdersPage = lazy(() => import('./pages/OrdersPage'))
const OrderDetailPage = lazy(() => import('./pages/OrderDetailPage'))
const ProfilePage = lazy(() => import('./pages/ProfilePage'))
const AdminDashboardPage = lazy(() => import('./pages/admin/AdminDashboardPage'))
const AdminProductsPage = lazy(() => import('./pages/admin/AdminProductsPage'))
const AdminInventoryPage = lazy(() => import('./pages/admin/AdminInventoryPage'))
const AdminOrdersPage = lazy(() => import('./pages/admin/AdminOrdersPage'))

function FullScreenLoader() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-parchment">
      <div className="text-center text-ink">
        <Spinner className="mx-auto mb-4" />
        <p className="font-heading text-lg text-crimson">Preparing the bazaar...</p>
      </div>
    </div>
  )
}

function AppShell() {
  const refreshToken = useAuthStore((state) => state.refreshToken)
  const [bootstrapping, setBootstrapping] = useState(true)

  useEffect(() => {
    let active = true

    const bootstrap = async () => {
      await refreshToken()
      if (active) {
        setBootstrapping(false)
      }
    }

    void bootstrap()

    return () => {
      active = false
    }
  }, [refreshToken])

  if (bootstrapping) {
    return <FullScreenLoader />
  }

  return (
    <AppErrorBoundary>
      <Suspense fallback={<FullScreenLoader />}>
        <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/products/:id" element={<ProductDetailPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route
            path="/cart"
            element={
              <ProtectedRoute allowedRoles={['Customer']}>
                <CartPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/checkout"
            element={
              <ProtectedRoute allowedRoles={['Customer']}>
                <CheckoutPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/checkout/confirmation"
            element={
              <ProtectedRoute allowedRoles={['Customer']}>
                <OrderConfirmationPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/orders"
            element={
              <ProtectedRoute>
                <OrdersPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/orders/:id"
            element={
              <ProtectedRoute>
                <OrderDetailPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/profile"
            element={
              <ProtectedRoute>
                <ProfilePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/admin"
            element={
              <AdminRoute>
                <AdminDashboardPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/products"
            element={
              <AdminRoute>
                <AdminProductsPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/inventory"
            element={
              <AdminRoute>
                <AdminInventoryPage />
              </AdminRoute>
            }
          />
          <Route
            path="/admin/orders"
            element={
              <AdminRoute>
                <AdminOrdersPage />
              </AdminRoute>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
      </Suspense>
    </AppErrorBoundary>
  )
}

export default function App() {
  return (
    <AppErrorBoundary>
      <BrowserRouter>
        <AppShell />
      </BrowserRouter>
    </AppErrorBoundary>
  )
}

