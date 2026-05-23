export interface InventoryItem {
  productID: number;
  productName: string;
  quantityAvailable: number;
  reorderThreshold: number;
  lastUpdated: string;
  isLowStock: boolean;
}
