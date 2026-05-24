export interface InventoryItem {
  productId: number
  productName: string
  quantityAvailable: number
  lastUpdated: string
  reorderThreshold: number
  isLowStock: boolean
}

export interface InventoryUpdateRequest {
  quantityAvailable: number
  reorderThreshold: number
}

