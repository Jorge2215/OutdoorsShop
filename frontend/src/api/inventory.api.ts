import type { PagedResult } from '../types/common'
import type { InventoryItem, InventoryUpdateRequest } from '../types/inventory'
import { fetchWithAuth } from './client'

interface RawInventoryItem {
  productID: number
  productName: string
  quantityAvailable: number
  lastUpdated: string
  reorderThreshold: number
  isLowStock: boolean
}

interface RawPagedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

function mapInventory(raw: RawInventoryItem): InventoryItem {
  return {
    productId: raw.productID,
    productName: raw.productName,
    quantityAvailable: raw.quantityAvailable,
    lastUpdated: raw.lastUpdated,
    reorderThreshold: raw.reorderThreshold,
    isLowStock: raw.isLowStock,
  }
}

function mapPagedResult(raw: RawPagedResult<RawInventoryItem>): PagedResult<InventoryItem> {
  return {
    items: raw.items.map(mapInventory),
    totalCount: raw.totalCount,
    pageNumber: raw.pageNumber,
    pageSize: raw.pageSize,
    totalPages: raw.totalPages,
  }
}

export const inventoryApi = {
  async list(pageNumber = 1, pageSize = 20) {
    const response = await fetchWithAuth<RawPagedResult<RawInventoryItem>>(`/inventory?pageNumber=${pageNumber}&pageSize=${pageSize}`)
    return mapPagedResult(response)
  },
  async getLowStock() {
    const response = await fetchWithAuth<RawInventoryItem[]>('/inventory/low-stock')
    return response.map(mapInventory)
  },
  async update(productId: number, payload: InventoryUpdateRequest) {
    const response = await fetchWithAuth<RawInventoryItem>(`/inventory/${productId}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
    return mapInventory(response)
  },
}

