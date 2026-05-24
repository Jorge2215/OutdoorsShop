import type { Category } from './category'

export interface Product {
  id: number
  name: string
  description: string
  price: number
  imageUrl: string | null
  isActive: boolean
  categoryId: number
  category: Category
  quantityAvailable: number
}

export interface ProductUpsertRequest {
  name: string
  description: string
  price: number
  imageUrl: string
  categoryId: number
  isActive?: boolean
}

