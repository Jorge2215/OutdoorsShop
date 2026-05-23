export interface Customer {
  id: number
  userId: string
  firstName: string
  lastName: string
  email: string
  phone: string
  address: string
  isActive: boolean
}

export interface CustomerUpdateRequest {
  firstName: string
  lastName: string
  phone: string
  address: string
}

