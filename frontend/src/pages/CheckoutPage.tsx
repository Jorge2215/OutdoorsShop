import { useEffect, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { customersApi } from '../api/customers.api'
import { ordersApi } from '../api/orders.api'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Textarea } from '../components/ui/Input'
import { useAuthStore } from '../store/authStore'
import { useCartStore } from '../store/cartStore'
import { formatCurrency } from '../utils/format'

const paymentOptions = ['LanternPay', 'Jade Card', 'Cash on Delivery']

export default function CheckoutPage() {
  const navigate = useNavigate()
  const items = useCartStore((state) => state.items)
  const totalPrice = useCartStore((state) => state.totalPrice)
  const clearCart = useCartStore((state) => state.clearCart)
  const user = useAuthStore((state) => state.user)
  const [shippingAddress, setShippingAddress] = useState('')
  const [paymentMethod, setPaymentMethod] = useState(paymentOptions[0])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    const loadCustomer = async () => {
      if (!user?.customerId) {
        return
      }

      try {
        const customer = await customersApi.getById(user.customerId)
        setShippingAddress(customer.address)
      } catch {
        // noop
      }
    }

    void loadCustomer()
  }, [user?.customerId])

  if (items.length === 0) {
    return <Navigate to="/cart" replace />
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setLoading(true)
    setError(null)

    try {
      const order = await ordersApi.create({
        shippingAddress,
        paymentMethod,
        items: items.map((item) => ({
          productId: item.product.id,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
        })),
      })
      clearCart()
      navigate('/checkout/confirmation', { state: { order } })
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : 'Unable to place the order right now.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="container-shell py-14">
      <div className="grid gap-8 lg:grid-cols-[1.1fr,0.9fr]">
        <Card>
          <h1 className="text-4xl text-crimson">Checkout</h1>
          <p className="mt-3 text-ink/70">Review your shipping details and choose a simulated payment path.</p>
          <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
            <Textarea label="Shipping address" value={shippingAddress} onChange={(event) => setShippingAddress(event.target.value)} required />
            <label className="block">
              <span className="field-label">Payment method</span>
              <select className="field-input" value={paymentMethod} onChange={(event) => setPaymentMethod(event.target.value)}>
                {paymentOptions.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
            </label>
            {error ? <Alert tone="error" title="Checkout failed" message={error} /> : null}
            <Button className="w-full" type="submit" loading={loading}>Place order</Button>
          </form>
        </Card>
        <Card className="h-fit">
          <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Order summary</p>
          <div className="mt-6 space-y-4">
            {items.map((item) => (
              <div key={item.product.id} className="flex items-start justify-between gap-4 border-b border-gold/20 pb-4 text-sm text-ink/70 last:border-b-0 last:pb-0">
                <div>
                  <p className="font-semibold text-ink">{item.product.name}</p>
                  <p>Qty {item.quantity}</p>
                </div>
                <p className="font-bold text-ink">{formatCurrency(item.quantity * item.unitPrice)}</p>
              </div>
            ))}
          </div>
          <div className="mt-6 rounded-3xl border border-gold/30 bg-white/75 p-5">
            <p className="text-xs font-bold uppercase tracking-[0.24em] text-gold">Payment simulation</p>
            <p className="mt-2 text-sm text-ink/70">All methods resolve instantly for demo purposes with a successful confirmation flow.</p>
          </div>
          <div className="mt-6 flex items-center justify-between">
            <span className="text-sm font-bold uppercase tracking-[0.2em] text-ink/60">Total</span>
            <span className="font-heading text-4xl text-crimson">{formatCurrency(totalPrice)}</span>
          </div>
        </Card>
      </div>
    </div>
  )
}

