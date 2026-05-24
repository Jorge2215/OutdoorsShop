import { create } from 'zustand'
import { mapUserProfile } from '../api/auth.api'
import { buildApiUrl } from '../api/config'
import type { AuthResponse, UserProfileDto, UserRole } from '../types/auth'

interface RawUserProfile {
  userId: string
  email: string
  name: string
  customerID?: number | null
  roles: string[]
}

interface AuthState {
  accessToken: string | null
  user: UserProfileDto | null
  isAuthenticated: boolean
  role: UserRole | null
  setTokenAndUser: (token: string, user: UserProfileDto) => void
  clearAuth: () => void
  refreshToken: () => Promise<boolean>
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  isAuthenticated: false,
  role: null,
  setTokenAndUser: (token, user) => set({ accessToken: token, user, isAuthenticated: true, role: user.role }),
  clearAuth: () => set({ accessToken: null, user: null, isAuthenticated: false, role: null }),
  refreshToken: async () => {
    try {
      const refreshResponse = await fetch(buildApiUrl('/auth/refresh'), {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
      })

      if (!refreshResponse.ok) {
        set({ accessToken: null, user: null, isAuthenticated: false, role: null })
        return false
      }

      const auth = (await refreshResponse.json()) as AuthResponse
      const meResponse = await fetch(buildApiUrl('/auth/me'), {
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${auth.accessToken}`,
        },
      })

      if (!meResponse.ok) {
        set({ accessToken: null, user: null, isAuthenticated: false, role: null })
        return false
      }

      const rawUser = (await meResponse.json()) as RawUserProfile
      const user = mapUserProfile(rawUser)
      set({ accessToken: auth.accessToken, user, isAuthenticated: true, role: user.role })
      return true
    } catch {
      set({ accessToken: null, user: null, isAuthenticated: false, role: null })
      return false
    }
  },
}))

