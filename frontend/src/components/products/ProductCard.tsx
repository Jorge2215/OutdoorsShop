import { ShoppingCart } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useCartStore } from '../../store/cartStore'
import type { Product } from '../../types/product'
import { getProductImage } from '../../utils/constants'
import { formatCurrency } from '../../utils/format'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { CategoryBadge } from './CategoryBadge'

interface ProductCardProps {
  product: Product
}

export function ProductCard({ product }: ProductCardProps) {
  const addItem = useCartStore((state) => state.addItem)

  return (
    <Card className="flex h-full flex-col">
      <Link to={`/products/${product.id}`} className="overflow-hidden rounded-[1.35rem]">
        <img
          src={getProductImage(product.imageUrl)}
          alt={product.name}
          className="h-56 w-full object-cover transition duration-500 hover:scale-105"
        />
      </Link>
      <div className="mt-5 flex flex-1 flex-col">
        <div className="mb-4 flex items-center justify-between gap-3">
          <CategoryBadge category={product.category} />
          <span className="text-sm font-bold uppercase tracking-[0.18em] text-ink/50">
            {product.quantityAvailable > 0 ? `${product.quantityAvailable} in stock` : 'Sold out'}
          </span>
        </div>
        <Link to={`/products/${product.id}`} className="text-xl font-semibold text-ink transition hover:text-crimson">
          {product.name}
        </Link>
        <p className="mt-3 flex-1 text-sm text-ink/70">{product.description || 'Equipment touched with craft, comfort, and expedition-ready detail.'}</p>
        <div className="mt-5 flex items-end justify-between gap-4">
          <div>
            <p className="text-xs uppercase tracking-[0.24em] text-ink/50">Bazaar price</p>
            <p className="font-heading text-2xl text-crimson">{formatCurrency(product.price)}</p>
          </div>
          <Button onClick={() => addItem(product, 1)} disabled={product.quantityAvailable <= 0}>
            <ShoppingCart className="mr-2 h-4 w-4" /> Add to cart
          </Button>
        </div>
      </div>
    </Card>
  )
}

