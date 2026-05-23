import { Navigate, useNavigate } from 'react-router-dom'
import { CartItemRow } from '../components/products/CartItemRow'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { useAuthStore } from '../store/authStore'
import { useCartStore } from '../store/cartStore'
import { formatCurrency } from '../utils/format'

export default function CartPage() {
  const navigate = useNavigate()
  const items = useCartStore((state) => state.items)
  const totalItems = useCartStore((state) => state.totalItems)
  const totalPrice = useCartStore((state) => state.totalPrice)
  const role = useAuthStore((state) => state.role)

  if (role === 'Administrator') {
    return <Navigate to="/admin" replace />
  }

  return (
    <div className="container-shell py-14">
      <div className="grid gap-8 lg:grid-cols-[1.2fr,0.8fr]">
        <div>
          <h1 className="text-4xl text-crimson">Your cart</h1>
          <p className="mt-3 text-ink/70">Everything you have gathered for the next route lives here until checkout.</p>
          <div className="mt-8 space-y-4">
            {items.length === 0 ? (
              <Alert tone="info" title="Your cart is empty" message="Start with the product catalog to gather gear for your next expedition." />
            ) : (
              items.map((item) => <CartItemRow key={item.product.id} item={item} />)
            )}
          </div>
        </div>
        <Card className="h-fit">
          <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Order review</p>
          <div className="mt-6 space-y-4 text-sm text-ink/70">
            <div className="flex items-center justify-between"><span>Total items</span><span className="font-bold text-ink">{totalItems}</span></div>
            <div className="flex items-center justify-between"><span>Estimated shipping</span><span className="font-bold text-ink">Calculated at checkout</span></div>
          </div>
          <div className="mt-6 rounded-3xl border border-gold/30 bg-white/75 p-5">
            <p className="text-xs font-bold uppercase tracking-[0.24em] text-gold">Current total</p>
            <p className="mt-3 font-heading text-4xl text-crimson">{formatCurrency(totalPrice)}</p>
          </div>
          <div className="mt-6 space-y-3">
            <Button className="w-full" onClick={() => navigate('/checkout')} disabled={items.length === 0}>Proceed to checkout</Button>
            <Button className="w-full" variant="secondary" onClick={() => navigate('/products')}>Continue shopping</Button>
          </div>
        </Card>
      </div>
    </div>
  )
}

