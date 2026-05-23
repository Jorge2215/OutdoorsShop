import apiClient from './client';
import type { Order, CreateOrderRequest } from '../types/order';

export const ordersApi = {
  getAll: () => apiClient.get<Order[]>('/orders'),
  getById: (id: number) => apiClient.get<Order>(`/orders/${id}`),
  create: (data: CreateOrderRequest) => apiClient.post<Order>('/orders', data),
  cancel: (id: number) => apiClient.patch(`/orders/${id}/cancel`),
};
