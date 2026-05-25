export type UserRole = 'Administrator' | 'Customer'

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  firstName: string
  lastName: string
  email: string
  password: string
  confirmPassword: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken?: string
  expiresAt: string
}

export interface UserProfileDto {
  userId: string
  email: string
  firstName: string
  lastName: string
  role: UserRole | null
  customerId: number | null
  fullName: string
}

