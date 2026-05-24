import { useState } from 'react'
import { ordersApi } from '../../api/orders.api'
import { Alert } from '../../components/ui/Alert'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Spinner } from '../../components/ui/Spinner'
import { useAsyncData } from '../../hooks/useAsyncData'
import type { OrderStatus } from '../../types/order'
import { formatCurrency, formatDate, getOrderTone } from '../../utils/format'

const statusOptions: OrderStatus[] = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled']

export default function AdminOrdersPage() {
  const { data, loading, error, reload } = useAsyncData(async () => ordersApi.list({ pageNumber: 1, pageSize: 20 }), [])
  const [draftStatus, setDraftStatus] = useState<Record<number, OrderStatus>>({})
  const [savingId, setSavingId] = useState<number | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)

  const handleSave = async (orderId: number, status: OrderStatus) => {
    setSavingId(orderId)
    setSaveError(null)

    try {
      await ordersApi.updateStatus(orderId, status)
      reload()
    } catch (caughtError) {
      setSaveError(caughtError instanceof Error ? caughtError.message : 'Unable to update the order status.')
    } finally {
      setSavingId(null)
    }
  }

  return (
    <div className="container-shell py-14">
      <h1 className="text-4xl text-crimson">Admin orders</h1>
      <p className="mt-3 text-ink/70">Monitor every placed order and update shipment progress directly from the table.</p>

      <Card className="mt-8 overflow-hidden p-0">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner /></div>
        ) : error ? (
          <div className="p-6"><Alert tone="error" title="Orders unavailable" message={error} /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm text-ink/70">
              <thead className="bg-ink text-xs uppercase tracking-[0.22em] text-gold">
                <tr>
                  <th className="px-6 py-4">Order</th>
                  <th className="px-6 py-4">Created</th>
                  <th className="px-6 py-4">Total</th>
                  <th className="px-6 py-4">Current status</th>
                  <th className="px-6 py-4">Update</th>
                  <th className="px-6 py-4 text-right">Detail</th>
                </tr>
              </thead>
              <tbody>
                {data?.items.map((order) => (
                  <tr key={order.id} className="border-t border-gold/20 bg-white/75">
                    <td className="px-6 py-4 font-semibold text-ink">#{order.id}</td>
                    <td className="px-6 py-4">{formatDate(order.createdAt)}</td>
                    <td className="px-6 py-4">{formatCurrency(order.totalAmount)}</td>
                    <td className="px-6 py-4"><Badge tone={getOrderTone(order.status)}>{order.status}</Badge></td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <select className="field-input min-w-40 py-2" value={draftStatus[order.id] ?? order.status} onChange={(event) => setDraftStatus((current) => ({ ...current, [order.id]: event.target.value as OrderStatus }))}>
                          {statusOptions.map((status) => <option key={status} value={status}>{status}</option>)}
                        </select>
                        <Button size="sm" onClick={() => handleSave(order.id, draftStatus[order.id] ?? order.status)} loading={savingId === order.id}>Save</Button>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button type="button" className="font-bold uppercase tracking-[0.18em] text-ink hover:text-crimson" onClick={() => window.location.assign(`/orders/${order.id}`)}>Open</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
      {saveError ? <div className="mt-6"><Alert tone="error" title="Status update failed" message={saveError} /></div> : null}
    </div>
  )
}

