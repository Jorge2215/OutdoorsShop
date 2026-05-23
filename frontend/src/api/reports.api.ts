import apiClient from './client';

export const reportsApi = {
  exportOrders: () => apiClient.get('/reports/orders', { responseType: 'blob' }),
  exportInventory: () => apiClient.get('/reports/inventory', { responseType: 'blob' }),
};
