import { Minus, Plus, Trash2 } from 'lucide-react'
import { useCartStore } from '../../store/cartStore'
import type { CartItem } from '../../types/order'
import { getProductImage } from '../../utils/constants'
import { formatCurrency } from '../../utils/format'

interface CartItemRowProps {
  item: CartItem
}

export function CartItemRow({ item }: CartItemRowProps) {
  const removeItem = useCartStore((state) => state.removeItem)
  const updateQuantity = useCartStore((state) => state.updateQuantity)

  return (
    <div className="grid gap-4 rounded-[1.5rem] border border-gold/30 bg-white/80 p-4 shadow-sm md:grid-cols-[120px,1fr,auto] md:items-center">
      <img src={getProductImage(item.product.imageUrl)} alt={item.product.name} className="h-28 w-full rounded-2xl object-cover md:w-[120px]" />
      <div>
        <p className="font-heading text-xl text-ink">{item.product.name}</p>
        <p className="mt-2 text-sm text-ink/65">{item.product.description || 'Ready for the next winding trail and lantern-lit camp.'}</p>
        <p className="mt-3 text-sm font-bold uppercase tracking-[0.2em] text-crimson">{formatCurrency(item.unitPrice)} each</p>
      </div>
      <div className="flex flex-col items-start gap-3 md:items-end">
        <div className="inline-flex items-center gap-3 rounded-full border border-gold/35 bg-parchment px-3 py-2">
          <button type="button" className="rounded-full p-1 text-ink/70 transition hover:bg-gold/15 hover:text-crimson" onClick={() => updateQuantity(item.product.id, item.quantity - 1)} aria-label="Decrease quantity">
            <Minus className="h-4 w-4" />
          </button>
          <span className="min-w-8 text-center text-sm font-bold text-ink">{item.quantity}</span>
          <button type="button" className="rounded-full p-1 text-ink/70 transition hover:bg-gold/15 hover:text-crimson" onClick={() => updateQuantity(item.product.id, item.quantity + 1)} aria-label="Increase quantity">
            <Plus className="h-4 w-4" />
          </button>
        </div>
        <p className="font-heading text-xl text-crimson">{formatCurrency(item.quantity * item.unitPrice)}</p>
        <button type="button" onClick={() => removeItem(item.product.id)} className="inline-flex items-center gap-2 text-sm font-bold uppercase tracking-[0.18em] text-ink/55 transition hover:text-crimson">
          <Trash2 className="h-4 w-4" /> Remove
        </button>
      </div>
    </div>
  )
}

