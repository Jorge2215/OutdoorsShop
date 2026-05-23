import apiClient from './client';
import type { InventoryItem } from '../types/inventory';

export const inventoryApi = {
  getAll: () => apiClient.get<InventoryItem[]>('/inventory'),
  update: (productId: number, quantity: number) =>
    apiClient.patch(`/inventory/${productId}`, { quantityAvailable: quantity }),
};
