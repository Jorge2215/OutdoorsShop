import apiClient from './client';
import type { CartItem } from '../types/cart';
import type { CreateOrderRequest } from '../types/order';

export const cartApi = {
  submitOrder: (items: CartItem[]) => {
    const payload: CreateOrderRequest = {
      items: items.map((i) => ({ productID: i.productID, quantity: i.quantity })),
    };
    return apiClient.post('/orders', payload);
  },
};
