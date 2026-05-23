import type { Product } from './product'

export type OrderStatus = 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled'
export type PaymentStatus = 'Pending' | 'Confirmed' | 'Failed'

export interface OrderItem {
  id: number
  productId: number
  product: Product
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface Order {
  id: number
  customerId: number
  status: OrderStatus
  createdAt: string
  paymentMethod: string
  paymentStatus: PaymentStatus
  totalAmount: number
  items: OrderItem[]
  shippingAddress: string
}

export interface CartItem {
  product: Product
  quantity: number
  unitPrice: number
}

export interface OrderCreateRequest {
  shippingAddress: string
  paymentMethod: string
  items: Array<{
    productId: number
    quantity: number
    unitPrice: number
  }>
}

