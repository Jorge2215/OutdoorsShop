import apiClient from './client';
import type { Category } from '../types/category';

export const categoriesApi = {
  getAll: () => apiClient.get<Category[]>('/categories'),
  getById: (id: number) => apiClient.get<Category>(`/categories/${id}`),
};
