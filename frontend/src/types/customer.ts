export interface Customer {
  id: number
  userId: string
  firstName: string
  lastName: string
  email: string
  avatarUrl: string | null
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
