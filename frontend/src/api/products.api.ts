import type { Category } from '../types/category'
import type { Product, ProductUpsertRequest } from '../types/product'
import { fetchWithAuth, fetchWithAuthMultipart, request } from './client'

export type ProductListSort = 'name_asc' | 'price_asc' | 'price_desc'

export interface ProductListParams {
  categoryId?: number
  search?: string
  includeInactive?: boolean
  minPrice?: number
  maxPrice?: number
  sort?: ProductListSort
}

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

function buildQuery(params?: ProductListParams) {
  const query = new URLSearchParams()
  if (params?.categoryId) {
    query.set('categoryId', String(params.categoryId))
  }
  if (params?.search) {
    query.set('search', params.search)
  }
  if (params?.includeInactive) {
    query.set('includeInactive', 'true')
  }
  if (params?.minPrice !== undefined) {
    query.set('minPrice', String(params.minPrice))
  }
  if (params?.maxPrice !== undefined) {
    query.set('maxPrice', String(params.maxPrice))
  }
  if (params?.sort) {
    query.set('sort', params.sort)
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
  async list(params?: ProductListParams) {
    const response = params?.includeInactive
      ? await fetchWithAuth<RawProduct[]>(`/products${buildQuery(params)}`)
      : await request<RawProduct[]>(`/products${buildQuery(params)}`)
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
  async uploadImage(productId: number, file: File): Promise<string> {
    const formData = new FormData()
    formData.append('file', file)
    const response = await fetchWithAuthMultipart<{ imageUrl: string } | string>(
      `/products/${productId}/image`,
      formData,
    )
    if (typeof response === 'string') {
      return response
    }
    return (response as { imageUrl: string }).imageUrl
  },
}
