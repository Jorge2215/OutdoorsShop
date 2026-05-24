import { AlertTriangle, Boxes, PackageCheck, ScrollText } from 'lucide-react'
import { inventoryApi } from '../../api/inventory.api'
import { ordersApi } from '../../api/orders.api'
import { productsApi } from '../../api/products.api'
import { Alert } from '../../components/ui/Alert'
import { Badge } from '../../components/ui/Badge'
import { Card } from '../../components/ui/Card'
import { Spinner } from '../../components/ui/Spinner'
import { useAsyncData } from '../../hooks/useAsyncData'
import { formatCurrency, formatDate, getOrderTone } from '../../utils/format'

export default function AdminDashboardPage() {
  const { data, loading, error } = useAsyncData(
    async () => {
      const [products, lowStock, recentOrders] = await Promise.all([
        productsApi.list(),
        inventoryApi.getLowStock(),
        ordersApi.list({ pageNumber: 1, pageSize: 5 }),
      ])
      return { products, lowStock, recentOrders }
    },
    [],
  )

  return (
    <div className="container-shell py-14">
      <h1 className="text-4xl text-crimson">Admin dashboard</h1>
      <p className="mt-3 text-ink/70">A quick read on products, low-stock pressure, and the latest orders entering the system.</p>
      {loading ? (
        <div className="flex justify-center py-16"><Spinner /></div>
      ) : error ? (
        <div className="mt-8"><Alert tone="error" title="Dashboard unavailable" message={error} /></div>
      ) : (
        <>
          <div className="mt-8 grid gap-6 md:grid-cols-3">
            {[
              { icon: Boxes, label: 'Products', value: data?.products.length ?? 0 },
              { icon: AlertTriangle, label: 'Low stock', value: data?.lowStock.length ?? 0 },
              { icon: PackageCheck, label: 'Recent orders', value: data?.recentOrders.items.length ?? 0 },
            ].map((item) => (
              <Card key={item.label}>
                <item.icon className="h-7 w-7 text-gold" />
                <p className="mt-5 text-sm font-bold uppercase tracking-[0.28em] text-ink/60">{item.label}</p>
                <p className="mt-3 font-heading text-5xl text-crimson">{item.value}</p>
              </Card>
            ))}
          </div>

          <div className="mt-10 grid gap-8 lg:grid-cols-[0.95fr,1.05fr]">
            <Card>
              <div className="flex items-center gap-3">
                <AlertTriangle className="h-5 w-5 text-crimson" />
                <h2 className="text-2xl text-ink">Low stock alerts</h2>
              </div>
              <div className="mt-6 space-y-3">
                {(data?.lowStock.length ?? 0) === 0 ? (
                  <Alert tone="success" title="Inventory looks steady" message="No items are currently below their reorder threshold." />
                ) : (
                  data?.lowStock.map((item) => (
                    <div key={item.productId} className="rounded-3xl border border-gold/25 bg-white/80 p-4">
                      <p className="font-heading text-xl text-ink">{item.productName}</p>
                      <p className="mt-2 text-sm text-ink/65">{item.quantityAvailable} available · threshold {item.reorderThreshold}</p>
                    </div>
                  ))
                )}
              </div>
            </Card>
            <Card>
              <div className="flex items-center gap-3">
                <ScrollText className="h-5 w-5 text-gold" />
                <h2 className="text-2xl text-ink">Recent orders</h2>
              </div>
              <div className="mt-6 space-y-3">
                {data?.recentOrders.items.map((order) => (
                  <div key={order.id} className="rounded-3xl border border-gold/25 bg-white/80 p-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div>
                        <p className="font-heading text-xl text-ink">Order #{order.id}</p>
                        <p className="text-sm text-ink/60">{formatDate(order.createdAt)}</p>
                      </div>
                      <Badge tone={getOrderTone(order.status)}>{order.status}</Badge>
                    </div>
                    <div className="mt-3 flex items-center justify-between text-sm text-ink/70">
                      <span>{order.items.length} item{order.items.length === 1 ? '' : 's'}</span>
                      <span className="font-bold text-crimson">{formatCurrency(order.totalAmount)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          </div>
        </>
      )}
    </div>
  )
}

