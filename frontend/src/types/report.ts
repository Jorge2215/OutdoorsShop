export type ReportType = 'orders' | 'inventory'
export type ReportFormat = 'csv' | 'excel'

export interface ReportRequestCreateInput {
  reportType: ReportType
  format: ReportFormat
  from?: string
  to?: string
}

export interface ReportDownloadInfo {
  url: string | null
  fileName: string | null
  expiresAt: string | null
  contentType: string | null
}

export interface ReportRequest {
  id: string
  status: string
  reportType: ReportType
  format: ReportFormat
  from: string | null
  to: string | null
  createdAt: string
  updatedAt: string
  completedAt: string | null
  errorMessage: string | null
  download: ReportDownloadInfo | null
}

export type ReportDownloadResult =
  | {
      kind: 'url'
      url: string
      fileName: string | null
      expiresAt: string | null
    }
  | {
      kind: 'blob'
      blob: Blob
      fileName: string | null
      expiresAt: string | null
    }
