import { ShoppingCart } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import type { Product } from '../../types/product';
import { useCartStore } from '../../store/cart.store';
import { Button } from '../ui/Button';
import { Badge } from '../ui/Badge';

export function ProductCard({ product }: { product: Product }) {
  const addItem = useCartStore((s) => s.addItem);
  const navigate = useNavigate();

  return (
    <div className="bg-white rounded-lg border border-gray-200 overflow-hidden hover:shadow-md transition-shadow">
      <div
        className="h-48 bg-gray-100 cursor-pointer flex items-center justify-center overflow-hidden"
        onClick={() => navigate(`/products/${product.productID}`)}
      >
        {product.imageUrl ? (
          <img src={product.imageUrl} alt={product.name} className="h-full w-full object-cover" />
        ) : (
          <span className="text-4xl">🏕️</span>
        )}
      </div>
      <div className="p-4">
        <h3
          className="font-semibold text-gray-800 truncate cursor-pointer hover:text-green-700"
          onClick={() => navigate(`/products/${product.productID}`)}
        >
          {product.name}
        </h3>
        <p className="text-sm text-gray-500 mt-1">{product.categoryName}</p>
        <div className="flex items-center justify-between mt-3">
          <span className="text-lg font-bold text-green-700">${product.price.toFixed(2)}</span>
          {product.quantityAvailable === 0 ? (
            <Badge variant="red">Out of stock</Badge>
          ) : product.quantityAvailable <= 5 ? (
            <Badge variant="yellow">Low stock</Badge>
          ) : null}
        </div>
        <Button
          className="w-full mt-3"
          size="sm"
          disabled={product.quantityAvailable === 0}
          onClick={() =>
            addItem({
              productID: product.productID,
              name: product.name,
              price: product.price,
              quantity: 1,
              imageUrl: product.imageUrl,
            })
          }
        >
          <ShoppingCart size={14} className="mr-1.5" /> Add to cart
        </Button>
      </div>
    </div>
  );
}
