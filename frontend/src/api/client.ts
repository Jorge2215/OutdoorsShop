import { useAuthStore } from '../store/authStore'
import { buildApiUrl } from './config'

export class ApiError extends Error {
  status: number
  details?: unknown

  constructor(message: string, status: number, details?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

function extractErrorMessage(payload: unknown, fallback: string) {
  if (!payload) {
    return fallback
  }

  if (typeof payload === 'string') {
    return payload
  }

  if (Array.isArray(payload)) {
    const messages = payload
      .map((entry) => {
        if (typeof entry === 'string') {
          return entry
        }
        if (entry && typeof entry === 'object' && 'description' in entry && typeof entry.description === 'string') {
          return entry.description
        }
        return null
      })
      .filter(Boolean)

    return messages.join(' ') || fallback
  }

  if (typeof payload === 'object') {
    const record = payload as Record<string, unknown>
    if (typeof record.message === 'string') {
      return record.message
    }
    if (typeof record.title === 'string') {
      return record.title
    }
    if (record.errors && typeof record.errors === 'object') {
      const nested = Object.values(record.errors as Record<string, unknown>)
        .flatMap((value) => (Array.isArray(value) ? value : [value]))
        .filter((value): value is string => typeof value === 'string')
      if (nested.length > 0) {
        return nested.join(' ')
      }
    }
  }

  return fallback
}

async function parseResponse<T>(response: Response): Promise<T> {
  const text = await response.text()
  const payload = text ? (JSON.parse(text) as unknown) : null

  if (!response.ok) {
    throw new ApiError(extractErrorMessage(payload, response.statusText || 'Request failed'), response.status, payload)
  }

  return payload as T
}

function mergeHeaders(headers?: HeadersInit, token?: string) {
  const merged = new Headers(headers)
  if (!merged.has('Content-Type')) {
    merged.set('Content-Type', 'application/json')
  }
  if (token) {
    merged.set('Authorization', `Bearer ${token}`)
  }
  return merged
}

function redirectToLogin() {
  if (typeof window === 'undefined') {
    return
  }

  const target = `${window.location.pathname}${window.location.search}`
  const next = encodeURIComponent(target)
  window.location.assign(`/login?next=${next}`)
}

export async function request<T>(path: string, init: RequestInit = {}) {
  const response = await fetch(buildApiUrl(path), {
    ...init,
    headers: mergeHeaders(init.headers),
    credentials: 'include',
  })

  return parseResponse<T>(response)
}

export async function requestWithToken<T>(path: string, token: string, init: RequestInit = {}) {
  const response = await fetch(buildApiUrl(path), {
    ...init,
    headers: mergeHeaders(init.headers, token),
    credentials: 'include',
  })

  return parseResponse<T>(response)
}

export async function fetchWithAuth<T>(path: string, init: RequestInit = {}, retried = false): Promise<T> {
  const token = useAuthStore.getState().accessToken

  const response = await fetch(buildApiUrl(path), {
    ...init,
    headers: mergeHeaders(init.headers, token ?? undefined),
    credentials: 'include',
  })

  if (response.status === 401 && !retried) {
    const refreshed = await useAuthStore.getState().refreshToken()
    if (refreshed) {
      return fetchWithAuth<T>(path, init, true)
    }
  }

  if (response.status === 401) {
    useAuthStore.getState().clearAuth()
    redirectToLogin()
  }

  return parseResponse<T>(response)
}

