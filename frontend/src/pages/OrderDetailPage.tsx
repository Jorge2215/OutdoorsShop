import { Link, useParams } from 'react-router-dom'
import { ordersApi } from '../api/orders.api'
import { Alert } from '../components/ui/Alert'
import { Badge } from '../components/ui/Badge'
import { Card } from '../components/ui/Card'
import { Spinner } from '../components/ui/Spinner'
import { useAsyncData } from '../hooks/useAsyncData'
import { formatCurrency, formatDate, getOrderTone } from '../utils/format'

export default function OrderDetailPage() {
  const { id } = useParams()
  const orderId = Number(id)
  const { data: order, loading, error } = useAsyncData(async () => ordersApi.getById(orderId), [orderId])

  if (loading) {
    return <div className="container-shell flex justify-center py-20"><Spinner /></div>
  }

  if (error || !order) {
    return (
      <div className="container-shell py-16">
        <Alert tone="error" title="Order unavailable" message={error ?? 'The requested order could not be loaded.'} />
      </div>
    )
  }

  return (
    <div className="container-shell py-14">
      <div className="mb-6 text-sm text-ink/55">
        <Link to="/orders" className="font-bold uppercase tracking-[0.2em] text-gold hover:text-crimson">? Back to orders</Link>
      </div>
      <div className="grid gap-8 lg:grid-cols-[0.95fr,1.05fr]">
        <Card>
          <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Order #{order.id}</p>
          <h1 className="mt-4 text-4xl text-crimson">Placed {formatDate(order.createdAt)}</h1>
          <div className="mt-6 flex flex-wrap gap-3">
            <Badge tone={getOrderTone(order.status)}>{order.status}</Badge>
            <Badge tone={order.paymentStatus === 'Confirmed' ? 'success' : order.paymentStatus === 'Failed' ? 'danger' : 'warning'}>{order.paymentStatus}</Badge>
          </div>
          <div className="mt-8 space-y-4 text-sm text-ink/70">
            <div><span className="font-bold text-ink">Shipping:</span> {order.shippingAddress}</div>
            <div><span className="font-bold text-ink">Payment:</span> {order.paymentMethod}</div>
            <div><span className="font-bold text-ink">Total:</span> {formatCurrency(order.totalAmount)}</div>
          </div>
        </Card>
        <Card>
          <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Items</p>
          <div className="mt-6 space-y-4">
            {order.items.map((item) => (
              <div key={item.id} className="flex items-center justify-between gap-4 rounded-3xl border border-gold/25 bg-white/80 p-4">
                <div>
                  <p className="font-heading text-xl text-ink">{item.product.name}</p>
                  <p className="text-sm text-ink/60">Qty {item.quantity} · {formatCurrency(item.unitPrice)} each</p>
                </div>
                <p className="font-heading text-2xl text-crimson">{formatCurrency(item.lineTotal)}</p>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  )
}

