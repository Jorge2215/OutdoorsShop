import type { Category } from '../types/category'
import type { Product, ProductUpsertRequest } from '../types/product'
import { fetchWithAuth, request } from './client'

interface RawProduct {
  productID: number
  name: string
  categoryID: number
  categoryName: string
  price: number
  description?: string | null
  imageUrl?: string | null
  isActive: boolean
  quantityAvailable: number
}

function mapCategory(product: RawProduct): Category {
  return {
    id: product.categoryID,
    name: product.categoryName,
    isActive: true,
  }
}

function mapProduct(product: RawProduct): Product {
  return {
    id: product.productID,
    name: product.name,
    description: product.description ?? '',
    price: product.price,
    imageUrl: product.imageUrl ?? null,
    isActive: product.isActive,
    categoryId: product.categoryID,
    category: mapCategory(product),
    quantityAvailable: product.quantityAvailable,
  }
}

function buildQuery(params?: { categoryId?: number; search?: string }) {
  const query = new URLSearchParams()
  if (params?.categoryId) {
    query.set('categoryId', String(params.categoryId))
  }
  if (params?.search) {
    query.set('search', params.search)
  }
  const value = query.toString()
  return value ? `?${value}` : ''
}

function mapPayload(product: ProductUpsertRequest) {
  return {
    name: product.name,
    categoryID: product.categoryId,
    price: product.price,
    description: product.description,
    imageUrl: product.imageUrl || null,
    isActive: product.isActive ?? true,
  }
}

export const productsApi = {
  async list(params?: { categoryId?: number; search?: string }) {
    const response = await request<RawProduct[]>(`/products${buildQuery(params)}`)
    return response.map(mapProduct)
  },
  async getById(id: number) {
    const response = await request<RawProduct>(`/products/${id}`)
    return mapProduct(response)
  },
  async create(payload: ProductUpsertRequest) {
    const response = await fetchWithAuth<RawProduct>('/products', {
      method: 'POST',
      body: JSON.stringify(mapPayload(payload)),
    })
    return mapProduct(response)
  },
  async update(id: number, payload: ProductUpsertRequest) {
    const response = await fetchWithAuth<RawProduct>(`/products/${id}`, {
      method: 'PUT',
      body: JSON.stringify(mapPayload(payload)),
    })
    return mapProduct(response)
  },
  async remove(id: number) {
    await fetchWithAuth<void>(`/products/${id}`, { method: 'DELETE' })
  },
}

