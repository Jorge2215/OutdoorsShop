import apiClient from './client';
import type { Product, CreateProductRequest, UpdateProductRequest } from '../types/product';

export interface ProductFilters {
  categoryId?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const productsApi = {
  getAll: (filters?: ProductFilters) => apiClient.get<Product[]>('/products', { params: filters }),
  getById: (id: number) => apiClient.get<Product>(`/products/${id}`),
  create: (data: CreateProductRequest) => apiClient.post<Product>('/products', data),
  update: (id: number, data: UpdateProductRequest) => apiClient.put<Product>(`/products/${id}`, data),
  delete: (id: number) => apiClient.delete(`/products/${id}`),
};
