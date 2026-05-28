import { useAuthStore } from '../store/authStore'
import type { ReportDownloadInfo, ReportDownloadResult, ReportFormat, ReportRequest, ReportRequestCreateInput, ReportType } from '../types/report'
import { buildApiUrl } from './config'
import { ApiError, fetchWithAuth } from './client'

type RawRecord = Record<string, unknown>

function asRecord(value: unknown): RawRecord {
  return value && typeof value === 'object' ? (value as RawRecord) : {}
}

function pickValue(record: RawRecord, keys: string[]) {
  for (const key of keys) {
    if (key in record && record[key] !== undefined && record[key] !== null) {
      return record[key]
    }
  }

  return null
}

function pickString(record: RawRecord, keys: string[]) {
  const value = pickValue(record, keys)
  return typeof value === 'string' ? value : null
}

function pickId(record: RawRecord, keys: string[]) {
  const value = pickValue(record, keys)
  if (typeof value === 'string' || typeof value === 'number') {
    return String(value)
  }
  return ''
}

function normalizeReportType(value: string | null): ReportType {
  return value?.toLowerCase() === 'inventory' ? 'inventory' : 'orders'
}

function normalizeFormat(value: string | null): ReportFormat {
  const normalized = value?.toLowerCase()
  return normalized === 'excel' || normalized === 'xlsx' ? 'excel' : 'csv'
}

function mapDownloadInfo(raw: unknown): ReportDownloadInfo | null {
  const direct = asRecord(raw)
  const nested = asRecord(pickValue(direct, ['download', 'downloadInfo', 'file', 'result']))
  const source = Object.keys(nested).length > 0 ? nested : direct

  const url = pickString(source, ['downloadUrl', 'url', 'downloadUri'])
  const fileName = pickString(source, ['fileName', 'filename', 'blobName'])
  const expiresAt = pickString(source, ['expiresAt', 'expiryAt', 'downloadExpiresAt'])
  const contentType = pickString(source, ['contentType'])

  if (!url && !fileName && !expiresAt && !contentType) {
    return null
  }

  return { url, fileName, expiresAt, contentType }
}

function mapReportRequest(raw: unknown): ReportRequest {
  const record = asRecord(raw)

  return {
    id: pickId(record, ['id', 'requestId', 'reportRequestId']),
    status: pickString(record, ['status']) ?? 'Pending',
    reportType: normalizeReportType(pickString(record, ['reportType', 'type'])),
    format: normalizeFormat(pickString(record, ['format'])),
    from: pickString(record, ['from', 'fromDate']),
    to: pickString(record, ['to', 'toDate']),
    createdAt: pickString(record, ['createdAt', 'requestedAt']) ?? '',
    updatedAt: pickString(record, ['updatedAt', 'lastUpdatedAt']) ?? '',
    completedAt: pickString(record, ['completedAt', 'finishedAt']),
    errorMessage: pickString(record, ['errorMessage', 'failureReason', 'message']),
    download: mapDownloadInfo(raw),
  }
}

function extractErrorMessage(payload: unknown, fallback: string) {
  if (!payload) {
    return fallback
  }

  if (typeof payload === 'string') {
    return payload
  }

  if (typeof payload === 'object') {
    const record = payload as RawRecord
    if (typeof record.message === 'string') {
      return record.message
    }
    if (typeof record.title === 'string') {
      return record.title
    }
  }

  return fallback
}

async function toApiError(response: Response) {
  const text = await response.text()
  let payload: unknown = text

  if (text) {
    try {
      payload = JSON.parse(text) as unknown
    } catch {
      payload = text
    }
  }

  return new ApiError(extractErrorMessage(payload, response.statusText || 'Request failed'), response.status, payload)
}

function buildAuthHeaders(token: string | null) {
  const headers = new Headers()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  return headers
}

function resolveUrl(pathOrUrl: string) {
  return /^https?:\/\//i.test(pathOrUrl) ? pathOrUrl : buildApiUrl(pathOrUrl)
}

async function fetchAuthorized(pathOrUrl: string, retried = false): Promise<Response> {
  const token = useAuthStore.getState().accessToken
  const response = await fetch(resolveUrl(pathOrUrl), {
    credentials: 'include',
    headers: buildAuthHeaders(token),
  })

  if (response.status === 401 && !retried) {
    const refreshed = await useAuthStore.getState().refreshToken()
    if (refreshed) {
      return fetchAuthorized(pathOrUrl, true)
    }
  }

  if (response.status === 401) {
    useAuthStore.getState().clearAuth()
    if (typeof window !== 'undefined') {
      const target = `${window.location.pathname}${window.location.search}`
      window.location.assign(`/login?next=${encodeURIComponent(target)}`)
    }
  }

  return response
}

function isApiManagedUrl(url: string) {
  return url.startsWith('/') || url.startsWith(buildApiUrl('/').slice(0, -1))
}

function getFileNameFromHeaders(headers: Headers) {
  const contentDisposition = headers.get('content-disposition')
  if (!contentDisposition) {
    return null
  }

  const utfMatch = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i)
  if (utfMatch?.[1]) {
    return decodeURIComponent(utfMatch[1])
  }

  const asciiMatch = contentDisposition.match(/filename="?([^";]+)"?/i)
  return asciiMatch?.[1] ?? null
}

async function fetchDownloadAsset(url: string, expiresAt: string | null, fileName: string | null): Promise<ReportDownloadResult> {
  const response = await fetchAuthorized(url)
  if (!response.ok) {
    throw await toApiError(response)
  }

  return {
    kind: 'blob',
    blob: await response.blob(),
    fileName: fileName ?? getFileNameFromHeaders(response.headers),
    expiresAt,
  }
}

function mapDownloadPayload(payload: unknown) {
  const download = mapDownloadInfo(payload)
  return {
    url: download?.url ?? null,
    fileName: download?.fileName ?? null,
    expiresAt: download?.expiresAt ?? null,
  }
}

export const reportsApi = {
  async createRequest(payload: ReportRequestCreateInput) {
    const response = await fetchWithAuth<unknown>('/reports/requests', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
    return mapReportRequest(response)
  },
  async getRequest(id: string) {
    const response = await fetchWithAuth<unknown>(`/reports/requests/${encodeURIComponent(id)}`)
    return mapReportRequest(response)
  },
  async getDownload(id: string): Promise<ReportDownloadResult> {
    const response = await fetchAuthorized(`/reports/requests/${encodeURIComponent(id)}/download`)

    if (!response.ok) {
      throw await toApiError(response)
    }

    if (response.redirected && response.url) {
      return { kind: 'url', url: response.url, fileName: null, expiresAt: null }
    }

    const contentType = response.headers.get('content-type')?.toLowerCase() ?? ''

    if (contentType.includes('application/json')) {
      const payload = (await response.json()) as unknown
      const download = mapDownloadPayload(payload)

      if (!download.url) {
        throw new Error('Download is not available yet.')
      }

      if (isApiManagedUrl(download.url)) {
        return fetchDownloadAsset(download.url, download.expiresAt, download.fileName)
      }

      return {
        kind: 'url',
        url: download.url,
        fileName: download.fileName,
        expiresAt: download.expiresAt,
      }
    }

    if (contentType.startsWith('text/plain')) {
      const url = (await response.text()).trim()
      if (!url) {
        throw new Error('Download is not available yet.')
      }

      if (isApiManagedUrl(url)) {
        return fetchDownloadAsset(url, null, null)
      }

      return { kind: 'url', url, fileName: null, expiresAt: null }
    }

    return {
      kind: 'blob',
      blob: await response.blob(),
      fileName: getFileNameFromHeaders(response.headers),
      expiresAt: null,
    }
  },
}
