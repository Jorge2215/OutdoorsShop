import type { Category } from '../types/category'
import { request } from './client'

interface RawCategory {
  categoryID: number
  name: string
  isActive: boolean
}

function mapCategory(response: RawCategory): Category {
  return {
    id: response.categoryID,
    name: response.name,
    isActive: response.isActive,
  }
}

export const categoriesApi = {
  async list() {
    const response = await request<RawCategory[]>('/categories')
    return response.map(mapCategory)
  },
  async getById(id: number) {
    const response = await request<RawCategory>(`/categories/${id}`)
    return mapCategory(response)
  },
}

