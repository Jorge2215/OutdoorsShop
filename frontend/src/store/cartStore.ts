import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import type { CartItem } from '../types/order'
import type { Product } from '../types/product'

interface CartState {
  items: CartItem[]
  totalItems: number
  totalPrice: number
  addItem: (product: Product, quantity: number) => void
  removeItem: (productId: number) => void
  updateQuantity: (productId: number, quantity: number) => void
  clearCart: () => void
}

function compute(items: CartItem[]) {
  return {
    items,
    totalItems: items.reduce((sum, item) => sum + item.quantity, 0),
    totalPrice: items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0),
  }
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      ...compute([]),
      addItem: (product, quantity) => {
        const existing = get().items.find((item) => item.product.id === product.id)
        if (existing) {
          const nextItems = get().items.map((item) =>
            item.product.id === product.id
              ? { ...item, quantity: Math.min(item.quantity + quantity, product.quantityAvailable || item.quantity + quantity) }
              : item,
          )
          set(compute(nextItems))
          return
        }

        const nextItems = [...get().items, { product, quantity, unitPrice: product.price }]
        set(compute(nextItems))
      },
      removeItem: (productId) => set(compute(get().items.filter((item) => item.product.id !== productId))),
      updateQuantity: (productId, quantity) => {
        if (quantity <= 0) {
          set(compute(get().items.filter((item) => item.product.id !== productId)))
          return
        }

        const nextItems = get().items.map((item) => (item.product.id === productId ? { ...item, quantity } : item))
        set(compute(nextItems))
      },
      clearCart: () => set(compute([])),
    }),
    {
      name: 'outdoorsshop-cart',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ items: state.items, totalItems: state.totalItems, totalPrice: state.totalPrice }),
    },
  ),
)

