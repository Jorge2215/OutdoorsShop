import { Save } from 'lucide-react'
import { useMemo, useState } from 'react'
import { inventoryApi } from '../../api/inventory.api'
import { Alert } from '../../components/ui/Alert'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Spinner } from '../../components/ui/Spinner'
import { useAsyncData } from '../../hooks/useAsyncData'
import { formatDate } from '../../utils/format'

export default function AdminInventoryPage() {
  const { data, loading, error, reload } = useAsyncData(
    async () => {
      const [inventory, lowStock] = await Promise.all([inventoryApi.list(1, 50), inventoryApi.getLowStock()])
      return { inventory, lowStock }
    },
    [],
  )
  const [drafts, setDrafts] = useState<Record<number, { quantityAvailable: number; reorderThreshold: number }>>({})
  const [saveError, setSaveError] = useState<string | null>(null)
  const [savingId, setSavingId] = useState<number | null>(null)

  const rows = useMemo(() => data?.inventory.items ?? [], [data?.inventory.items])

  const getDraft = (productId: number, quantityAvailable: number, reorderThreshold: number) => drafts[productId] ?? { quantityAvailable, reorderThreshold }

  const handleSave = async (productId: number, quantityAvailable: number, reorderThreshold: number) => {
    setSavingId(productId)
    setSaveError(null)
    const draft = getDraft(productId, quantityAvailable, reorderThreshold)

    try {
      await inventoryApi.update(productId, draft)
      reload()
    } catch (caughtError) {
      setSaveError(caughtError instanceof Error ? caughtError.message : 'Unable to update inventory.')
    } finally {
      setSavingId(null)
    }
  }

  return (
    <div className="container-shell py-14">
      <h1 className="text-4xl text-crimson">Admin inventory</h1>
      <p className="mt-3 text-ink/70">Adjust live stock quantities and reorder thresholds while spotlighting low-stock alerts.</p>

      <div className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {data?.lowStock.map((item) => (
          <Card key={item.productId}>
            <Badge tone="danger">Low stock</Badge>
            <h2 className="mt-4 text-2xl text-ink">{item.productName}</h2>
            <p className="mt-3 text-sm text-ink/65">{item.quantityAvailable} available · threshold {item.reorderThreshold}</p>
          </Card>
        ))}
      </div>

      <Card className="mt-8 overflow-hidden p-0">
        {loading ? (
          <div className="flex justify-center py-16"><Spinner /></div>
        ) : error ? (
          <div className="p-6"><Alert tone="error" title="Inventory unavailable" message={error} /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm text-ink/70">
              <thead className="bg-ink text-xs uppercase tracking-[0.22em] text-gold">
                <tr>
                  <th className="px-6 py-4">Product</th>
                  <th className="px-6 py-4">Quantity</th>
                  <th className="px-6 py-4">Threshold</th>
                  <th className="px-6 py-4">Updated</th>
                  <th className="px-6 py-4 text-right">Action</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const draft = getDraft(row.productId, row.quantityAvailable, row.reorderThreshold)
                  return (
                    <tr key={row.productId} className="border-t border-gold/20 bg-white/75">
                      <td className="px-6 py-4 font-semibold text-ink">{row.productName}</td>
                      <td className="px-6 py-4">
                        <input className="field-input max-w-28 py-2" type="number" min="0" value={draft.quantityAvailable} onChange={(event) => setDrafts((current) => ({ ...current, [row.productId]: { ...draft, quantityAvailable: Number(event.target.value) } }))} />
                      </td>
                      <td className="px-6 py-4">
                        <input className="field-input max-w-28 py-2" type="number" min="0" value={draft.reorderThreshold} onChange={(event) => setDrafts((current) => ({ ...current, [row.productId]: { ...draft, reorderThreshold: Number(event.target.value) } }))} />
                      </td>
                      <td className="px-6 py-4">{formatDate(row.lastUpdated)}</td>
                      <td className="px-6 py-4 text-right">
                        <Button size="sm" onClick={() => handleSave(row.productId, row.quantityAvailable, row.reorderThreshold)} loading={savingId === row.productId}><Save className="mr-2 h-4 w-4" /> Save</Button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>
      {saveError ? <div className="mt-6"><Alert tone="error" title="Inventory update failed" message={saveError} /></div> : null}
    </div>
  )
}

