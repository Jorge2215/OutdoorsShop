import { CheckCircle2 } from 'lucide-react'
import { Link, useLocation } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import type { Order } from '../types/order'
import { formatCurrency, formatDate } from '../utils/format'

interface ConfirmationState {
  order?: Order
}

export default function OrderConfirmationPage() {
  const location = useLocation()
  const state = location.state as ConfirmationState | null
  const order = state?.order

  if (!order) {
    return (
      <div className="container-shell py-16">
        <Alert tone="info" title="Confirmation not available" message="Place an order first to see the simulated payment success screen." />
      </div>
    )
  }

  return (
    <div className="container-shell py-16">
      <Card className="mx-auto max-w-3xl text-center">
        <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-jade/10 text-jade">
          <CheckCircle2 className="h-10 w-10" />
        </div>
        <p className="mt-6 text-sm font-bold uppercase tracking-[0.34em] text-gold">Order confirmed</p>
        <h1 className="mt-4 text-5xl text-crimson">Your path is prepared</h1>
        <p className="mt-4 text-lg text-ink/70">Payment simulation approved through {order.paymentMethod}. The order now lives in your account history.</p>
        <div className="mt-8 grid gap-4 rounded-[1.75rem] border border-gold/30 bg-white/80 p-6 text-left sm:grid-cols-3">
          <div><p className="text-xs font-bold uppercase tracking-[0.2em] text-gold">Order</p><p className="mt-2 text-lg text-ink">#{order.id}</p></div>
          <div><p className="text-xs font-bold uppercase tracking-[0.2em] text-gold">Placed</p><p className="mt-2 text-lg text-ink">{formatDate(order.createdAt)}</p></div>
          <div><p className="text-xs font-bold uppercase tracking-[0.2em] text-gold">Total</p><p className="mt-2 text-lg text-ink">{formatCurrency(order.totalAmount)}</p></div>
        </div>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <Link to={`/orders/${order.id}`}><Button>View order</Button></Link>
          <Link to="/products"><Button variant="secondary">Keep exploring</Button></Link>
        </div>
      </Card>
    </div>
  )
}

