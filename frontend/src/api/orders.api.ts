import type { PagedResult } from '../types/common'
import type { Order, OrderCreateRequest, OrderStatus } from '../types/order'
import { fetchWithAuth } from './client'

interface RawOrderItem {
  orderDetailID: number
  productID: number
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

interface RawOrder {
  orderID: number
  customerID: number
  orderDate: string
  shippingAddress: string
  paymentMethod: string
  totalAmount: number
  status: OrderStatus
  paymentStatus: 'Pending' | 'Confirmed' | 'Failed'
  items: RawOrderItem[]
}

interface RawPagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

function mapOrder(raw: RawOrder): Order {
  return {
    id: raw.orderID,
    customerId: raw.customerID,
    status: raw.status,
    createdAt: raw.orderDate,
    paymentMethod: raw.paymentMethod,
    paymentStatus: raw.paymentStatus,
    totalAmount: raw.totalAmount,
    shippingAddress: raw.shippingAddress,
    items: raw.items.map((item) => ({
      id: item.orderDetailID,
      productId: item.productID,
      product: {
        id: item.productID,
        name: item.productName,
        description: '',
        price: item.unitPrice,
        imageUrl: null,
        isActive: true,
        categoryId: 0,
        category: { id: 0, name: 'Bazaar Special', isActive: true },
        quantityAvailable: item.quantity,
      },
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      lineTotal: item.lineTotal,
    })),
  }
}

function mapPagedOrders(raw: RawPagedResult<RawOrder>): PagedResult<Order> {
  return {
    items: raw.items.map(mapOrder),
    totalCount: raw.totalCount,
    pageNumber: raw.pageNumber,
    pageSize: raw.pageSize,
    totalPages: raw.totalPages,
  }
}

function buildQuery(params?: { pageNumber?: number; pageSize?: number; status?: string }) {
  const query = new URLSearchParams()
  if (params?.pageNumber) {
    query.set('pageNumber', String(params.pageNumber))
  }
  if (params?.pageSize) {
    query.set('pageSize', String(params.pageSize))
  }
  if (params?.status) {
    query.set('status', params.status)
  }
  const value = query.toString()
  return value ? `?${value}` : ''
}

export const ordersApi = {
  async list(params?: { pageNumber?: number; pageSize?: number; status?: string }) {
    const response = await fetchWithAuth<RawPagedResult<RawOrder>>(`/orders${buildQuery(params)}`)
    return mapPagedOrders(response)
  },
  async getById(id: number) {
    const response = await fetchWithAuth<RawOrder>(`/orders/${id}`)
    return mapOrder(response)
  },
  async create(payload: OrderCreateRequest) {
    const response = await fetchWithAuth<RawOrder>('/orders', {
      method: 'POST',
      body: JSON.stringify({
        shippingAddress: payload.shippingAddress,
        paymentMethod: payload.paymentMethod,
        items: payload.items.map((item) => ({
          productID: item.productId,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
        })),
      }),
    })
    return mapOrder(response)
  },
  async updateStatus(id: number, status: OrderStatus) {
    const response = await fetchWithAuth<RawOrder>(`/orders/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    })
    return mapOrder(response)
  },
}

