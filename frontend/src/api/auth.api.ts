import type { AuthResponse, ChangePasswordRequest, LoginRequest, RegisterRequest, UserProfileDto } from '../types/auth'
import { splitDisplayName } from '../utils/format'
import { fetchWithAuth, request, requestWithToken } from './client'

interface RawAuthResponse {
  accessToken: string
  refreshToken?: string
  expiresAt: string
}

interface RawUserProfile {
  userId: string
  email: string
  name: string
  customerID?: number | null
  roles: string[]
}

function mapAuthResponse(response: RawAuthResponse): AuthResponse {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    expiresAt: response.expiresAt,
  }
}

export function mapUserProfile(response: RawUserProfile): UserProfileDto {
  const nameParts = splitDisplayName(response.name)
  const role = response.roles.includes('Administrator') ? 'Administrator' : response.roles.includes('Customer') ? 'Customer' : null

  return {
    userId: response.userId,
    email: response.email,
    firstName: nameParts.firstName,
    lastName: nameParts.lastName,
    role,
    customerId: response.customerID ?? null,
    fullName: `${nameParts.firstName} ${nameParts.lastName}`.trim() || response.name || response.email,
  }
}

export const authApi = {
  async login(payload: LoginRequest) {
    const response = await request<RawAuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    })

    return mapAuthResponse(response)
  },
  async register(payload: RegisterRequest) {
    const response = await request<RawAuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify({
        name: `${payload.firstName} ${payload.lastName}`.trim(),
        email: payload.email,
        password: payload.password,
        confirmPassword: payload.confirmPassword,
      }),
    })

    return mapAuthResponse(response)
  },
  async logout() {
    await request<void>('/auth/logout', { method: 'POST' })
  },
  async refreshSession() {
    const response = await request<RawAuthResponse>('/auth/refresh', { method: 'POST' })
    return mapAuthResponse(response)
  },
  async getMe(token?: string) {
    const response = token ? await requestWithToken<RawUserProfile>('/auth/me', token) : await request<RawUserProfile>('/auth/me')
    return mapUserProfile(response)
  },
  async changePassword(payload: ChangePasswordRequest) {
    await fetchWithAuth<void>('/users/change-password', {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },
}

