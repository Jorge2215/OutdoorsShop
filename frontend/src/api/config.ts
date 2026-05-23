export const API_ORIGIN = (import.meta.env.VITE_API_URL || 'http://localhost:5000').replace(/\/$/, '')
export const API_BASE_PATH = '/api/v1'

export function buildApiUrl(path: string) {
  return `${API_ORIGIN}${API_BASE_PATH}${path.startsWith('/') ? path : `/${path}`}`
}

