import type { Customer, CustomerUpdateRequest } from '../types/customer'
import { fetchWithAuth } from './client'

interface RawCustomer {
  customerID: number
  userId: string
  email: string
  firstName: string
  lastName: string
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
}

