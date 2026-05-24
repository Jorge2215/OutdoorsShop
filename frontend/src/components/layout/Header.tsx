import { LogOut, Menu, ShoppingCart, User, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { authApi } from '../../api/auth.api'
import { useAuthStore } from '../../store/authStore'
import { useCartStore } from '../../store/cartStore'
import { cn } from '../../utils/cn'
import { Button } from '../ui/Button'

type NavLinkItem = {
  label: string
  to: string
}

const guestLinks: NavLinkItem[] = [
  { label: 'Home', to: '/' },
  { label: 'Products', to: '/products' },
]

const customerLinks: NavLinkItem[] = [
  { label: 'Products', to: '/products' },
  { label: 'Orders', to: '/orders' },
  { label: 'Profile', to: '/profile' },
]

const adminLinks: NavLinkItem[] = [
  { label: 'Dashboard', to: '/admin' },
  { label: 'Products', to: '/admin/products' },
  { label: 'Inventory', to: '/admin/inventory' },
  { label: 'Orders', to: '/admin/orders' },
]

function linkClass(isActive: boolean) {
  return cn(
    'rounded-full px-4 py-2 text-sm font-bold uppercase tracking-[0.22em] transition',
    isActive ? 'bg-gold text-ink shadow-sm' : 'text-ink/70 hover:bg-gold/10 hover:text-crimson',
  )
}

export function Header() {
  const navigate = useNavigate()
  const [mobileOpen, setMobileOpen] = useState(false)
  const { isAuthenticated, role, user, clearAuth } = useAuthStore((state) => ({
    isAuthenticated: state.isAuthenticated,
    role: state.role,
    user: state.user,
    clearAuth: state.clearAuth,
  }))
  const totalItems = useCartStore((state) => state.totalItems)

  const links = useMemo<NavLinkItem[]>(() => {
    if (role === 'Administrator') {
      return [...guestLinks, ...adminLinks]
    }
    if (isAuthenticated) {
      return [...guestLinks, ...customerLinks]
    }
    return guestLinks
  }, [isAuthenticated, role])

  const handleLogout = async () => {
    try {
      await authApi.logout()
    } catch {
      // noop
    }
    clearAuth()
    navigate('/')
  }

  return (
    <header className="sticky top-0 z-40 border-b border-gold/30 bg-parchment/90 backdrop-blur">
      <div className="container-shell flex items-center justify-between gap-4 py-4">
        <button className="flex items-center gap-3 text-left" onClick={() => navigate('/')} type="button">
          <div className="flex h-12 w-12 items-center justify-center rounded-full border border-gold/50 bg-ink text-gold shadow-gold">
            *
          </div>
          <div>
            <p className="font-heading text-xl text-crimson sm:text-2xl">OutdoorsShop</p>
            <p className="text-xs uppercase tracking-[0.35em] text-ink/60">Eastern Trail Bazaar</p>
          </div>
        </button>

        <nav className="hidden items-center gap-2 lg:flex">
          {links.map((link) => (
            <NavLink key={link.to} to={link.to} className={({ isActive }) => linkClass(isActive)}>
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="hidden items-center gap-3 lg:flex">
          {role !== 'Administrator' && (
            <button
              type="button"
              onClick={() => navigate('/cart')}
              className="relative flex h-11 w-11 items-center justify-center rounded-full border border-gold/40 bg-white/80 text-ink shadow-sm transition hover:border-crimson hover:text-crimson"
              aria-label="Open cart"
            >
              <ShoppingCart className="h-5 w-5" />
              {totalItems > 0 && (
                <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-crimson px-1 text-[10px] font-bold text-white">
                  {totalItems}
                </span>
              )}
            </button>
          )}

          {isAuthenticated ? (
            <div className="flex items-center gap-3 rounded-full border border-gold/35 bg-white/80 px-4 py-2 shadow-sm">
              <User className="h-4 w-4 text-gold" />
              <div>
                <p className="text-sm font-bold text-ink">{user?.fullName || user?.email}</p>
                <p className="text-xs uppercase tracking-[0.22em] text-ink/50">{role}</p>
              </div>
              <button type="button" onClick={handleLogout} className="rounded-full p-2 text-ink/60 transition hover:bg-crimson/10 hover:text-crimson" aria-label="Logout">
                <LogOut className="h-4 w-4" />
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-3">
              <Button variant="ghost" onClick={() => navigate('/login')}>Sign in</Button>
              <Button onClick={() => navigate('/register')}>Join the journey</Button>
            </div>
          )}
        </div>

        <button
          type="button"
          className="inline-flex h-11 w-11 items-center justify-center rounded-full border border-gold/40 bg-white/80 text-ink lg:hidden"
          onClick={() => setMobileOpen((current) => !current)}
          aria-label="Toggle navigation"
        >
          {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </div>

      {mobileOpen && (
        <div className="border-t border-gold/25 bg-parchment/95 lg:hidden">
          <div className="container-shell space-y-3 py-4">
            <div className="flex flex-col gap-2">
              {links.map((link) => (
                <NavLink key={link.to} to={link.to} className={({ isActive }) => linkClass(isActive)} onClick={() => setMobileOpen(false)}>
                  {link.label}
                </NavLink>
              ))}
            </div>
            {role !== 'Administrator' && (
              <Button variant="secondary" className="w-full" onClick={() => { setMobileOpen(false); navigate('/cart') }}>
                <ShoppingCart className="mr-2 h-4 w-4" /> Cart ({totalItems})
              </Button>
            )}
            {isAuthenticated ? (
              <Button variant="ghost" className="w-full" onClick={handleLogout}>
                <LogOut className="mr-2 h-4 w-4" /> Logout
              </Button>
            ) : (
              <div className="grid gap-3 sm:grid-cols-2">
                <Button variant="ghost" className="w-full" onClick={() => { setMobileOpen(false); navigate('/login') }}>
                  Sign in
                </Button>
                <Button className="w-full" onClick={() => { setMobileOpen(false); navigate('/register') }}>
                  Join the journey
                </Button>
              </div>
            )}
          </div>
        </div>
      )}
    </header>
  )
}

