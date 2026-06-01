import type { Customer, CustomerUpdateRequest } from '../types/customer'
import { fetchWithAuth, fetchWithAuthMultipart } from './client'

interface RawCustomer {
  customerID: number
  userId: string
  email: string
  firstName: string
  lastName: string
  avatarUrl?: string | null
  phone?: string | null
  address?: string | null
  isActive: boolean
}

function mapCustomer(raw: RawCustomer): Customer {
  return {
    id: raw.customerID,
    userId: raw.userId,
    firstName: raw.firstName,
    lastName: raw.lastName,
    email: raw.email,
    avatarUrl: raw.avatarUrl ?? null,
    phone: raw.phone ?? '',
    address: raw.address ?? '',
    isActive: raw.isActive,
  }
}

export const customersApi = {
  async getById(id: number) {
    const response = await fetchWithAuth<RawCustomer>(`/customers/${id}`)
    return mapCustomer(response)
  },
  async update(id: number, payload: CustomerUpdateRequest) {
    const response = await fetchWithAuth<RawCustomer>(`/customers/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
    return mapCustomer(response)
  },
  async uploadAvatar(id: number, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    const response = await fetchWithAuthMultipart<RawCustomer>(`/customers/${id}/avatar`, formData)
    return mapCustomer(response)
  },
  async removeAvatar(id: number) {
    const response = await fetchWithAuth<RawCustomer>(`/customers/${id}/avatar`, {
      method: 'DELETE',
    })
    return mapCustomer(response)
  },
}
