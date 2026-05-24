import { ordersApi } from '../api/orders.api'
import { Alert } from '../components/ui/Alert'
import { Badge } from '../components/ui/Badge'
import { Card } from '../components/ui/Card'
import { Spinner } from '../components/ui/Spinner'
import { useAsyncData } from '../hooks/useAsyncData'
import { formatCurrency, formatDate, getOrderTone } from '../utils/format'

export default function OrdersPage() {
  const { data, loading, error } = useAsyncData(async () => ordersApi.list({ pageNumber: 1, pageSize: 20 }), [])

  return (
    <div className="container-shell py-14">
      <h1 className="text-4xl text-crimson">Order history</h1>
      <p className="mt-3 text-ink/70">Track every placed order, payment state, and delivery status from one page.</p>
      <div className="mt-8 space-y-4">
        {loading ? (
          <div className="flex justify-center py-12"><Spinner /></div>
        ) : error ? (
          <Alert tone="error" title="Orders unavailable" message={error} />
        ) : (data?.items.length ?? 0) === 0 ? (
          <Alert tone="info" title="No orders yet" message="Once you checkout, your journey history will appear here." />
        ) : (
          data?.items.map((order) => (
            <Card key={order.id}>
              <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div>
                  <p className="text-sm font-bold uppercase tracking-[0.28em] text-gold">Order #{order.id}</p>
                  <p className="mt-3 text-lg text-ink/70">Placed {formatDate(order.createdAt)} · {order.items.length} item{order.items.length === 1 ? '' : 's'}</p>
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Badge tone={getOrderTone(order.status)}>{order.status}</Badge>
                  <span className="font-heading text-2xl text-crimson">{formatCurrency(order.totalAmount)}</span>
                  <button type="button" className="text-sm font-bold uppercase tracking-[0.22em] text-ink hover:text-crimson" onClick={() => window.location.assign(`/orders/${order.id}`)}>View details</button>
                </div>
              </div>
            </Card>
          ))
        )}
      </div>
    </div>
  )
}

