import { Minus, Plus, ShoppingCart } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { productsApi } from '../api/products.api'
import { CategoryBadge } from '../components/products/CategoryBadge'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { Spinner } from '../components/ui/Spinner'
import { useAsyncData } from '../hooks/useAsyncData'
import { useCartStore } from '../store/cartStore'
import { getProductImage } from '../utils/constants'
import { formatCurrency } from '../utils/format'

export default function ProductDetailPage() {
  const { id } = useParams()
  const productId = Number(id)
  const [quantity, setQuantity] = useState(1)
  const addItem = useCartStore((state) => state.addItem)

  const { data: product, loading, error } = useAsyncData(async () => productsApi.getById(productId), [productId])

  const maxQuantity = useMemo(() => Math.max(1, product?.quantityAvailable ?? 1), [product?.quantityAvailable])

  if (loading) {
    return <div className="container-shell flex justify-center py-20"><Spinner /></div>
  }

  if (error || !product) {
    return (
      <div className="container-shell py-16">
        <Alert tone="error" title="Product unavailable" message={error ?? 'The requested item could not be loaded.'} />
      </div>
    )
  }

  return (
    <div className="container-shell py-14">
      <div className="mb-6 text-sm text-ink/55">
        <Link to="/products" className="font-bold uppercase tracking-[0.2em] text-gold hover:text-crimson">? Back to catalog</Link>
      </div>
      <div className="grid gap-8 lg:grid-cols-[1.05fr,0.95fr]">
        <Card className="p-3">
          <img src={getProductImage(product.imageUrl)} alt={product.name} className="h-full w-full rounded-[1.4rem] object-cover" />
        </Card>
        <Card className="flex flex-col justify-center">
          <CategoryBadge category={product.category} />
          <h1 className="mt-5 text-4xl text-crimson">{product.name}</h1>
          <p className="mt-4 text-base text-ink/70">{product.description || 'Thoughtfully built for brave weather, long horizons, and campfire stories.'}</p>
          <div className="mt-8 flex flex-wrap items-end justify-between gap-6">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.24em] text-gold">Bazaar price</p>
              <p className="font-heading text-4xl text-ink">{formatCurrency(product.price)}</p>
            </div>
            <div className="rounded-2xl border border-gold/30 bg-parchment px-4 py-3 text-right">
              <p className="text-xs font-bold uppercase tracking-[0.22em] text-gold">Stock status</p>
              <p className="mt-2 text-lg font-semibold text-ink">{product.quantityAvailable > 0 ? `${product.quantityAvailable} ready to ship` : 'Currently unavailable'}</p>
            </div>
          </div>
          <div className="mt-8 flex flex-wrap items-center gap-4">
            <div className="inline-flex items-center gap-4 rounded-full border border-gold/35 bg-white/80 px-4 py-3">
              <button type="button" onClick={() => setQuantity((current) => Math.max(1, current - 1))} className="rounded-full p-1 text-ink/70 transition hover:bg-gold/10 hover:text-crimson">
                <Minus className="h-4 w-4" />
              </button>
              <span className="min-w-10 text-center text-lg font-bold text-ink">{quantity}</span>
              <button type="button" onClick={() => setQuantity((current) => Math.min(maxQuantity, current + 1))} className="rounded-full p-1 text-ink/70 transition hover:bg-gold/10 hover:text-crimson">
                <Plus className="h-4 w-4" />
              </button>
            </div>
            <Button className="min-w-[220px]" onClick={() => addItem(product, quantity)} disabled={product.quantityAvailable <= 0}>
              <ShoppingCart className="mr-2 h-4 w-4" /> Add {quantity} to cart
            </Button>
          </div>
        </Card>
      </div>
    </div>
  )
}

