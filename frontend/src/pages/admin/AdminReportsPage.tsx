import { Clock3, Download, FileSpreadsheet, RefreshCcw, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { reportsApi } from '../../api/reports.api'
import { Alert } from '../../components/ui/Alert'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { useAsyncData } from '../../hooks/useAsyncData'
import type { ReportFormat, ReportRequest, ReportType } from '../../types/report'
import { saveStoredReportRequestIds, loadStoredReportRequestIds } from '../../utils/reportRequestHistory'

type TrackedReportRequest = {
  id: string
  request: ReportRequest | null
  error: string | null
}

const reportTypes: Array<{ value: ReportType; label: string; description: string }> = [
  { value: 'orders', label: 'Orders', description: 'Export placed orders with optional date filtering.' },
  { value: 'inventory', label: 'Inventory', description: 'Export current stock levels and thresholds.' },
]

const formats: Array<{ value: ReportFormat; label: string }> = [
  { value: 'csv', label: 'CSV' },
  { value: 'excel', label: 'Excel' },
]

function normalizeStatus(status: string) {
  return status.trim().toLowerCase()
}

function getStatusTone(status: string): 'neutral' | 'success' | 'warning' | 'danger' {
  const normalized = normalizeStatus(status)

  if (normalized === 'completed' || normalized === 'succeeded' || normalized === 'ready') {
    return 'success'
  }

  if (normalized === 'failed' || normalized === 'cancelled' || normalized === 'error' || normalized === 'rejected') {
    return 'danger'
  }

  if (normalized === 'processing' || normalized === 'queued' || normalized === 'running' || normalized === 'pending') {
    return 'warning'
  }

  return 'neutral'
}

function isReady(request: ReportRequest) {
  const normalized = normalizeStatus(request.status)
  return normalized === 'completed' || normalized === 'succeeded' || normalized === 'ready' || Boolean(request.download?.url || request.completedAt)
}

function isTerminal(request: ReportRequest) {
  const normalized = normalizeStatus(request.status)
  return isReady(request) || normalized === 'failed' || normalized === 'cancelled' || normalized === 'error' || normalized === 'rejected'
}

function formatDateTime(value: string | null) {
  if (!value) {
    return '—'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(parsed)
}

function labelForReportType(value: ReportType) {
  return value === 'inventory' ? 'Inventory' : 'Orders'
}

function labelForFormat(value: ReportFormat) {
  return value === 'excel' ? 'Excel' : 'CSV'
}

function defaultFileName(request: ReportRequest) {
  return `${request.reportType}-report-${request.id}.${request.format === 'excel' ? 'xlsx' : 'csv'}`
}

function triggerDownload(url: string, fileName?: string | null) {
  const link = document.createElement('a')
  link.href = url
  link.rel = 'noopener noreferrer'
  link.target = '_blank'
  if (fileName) {
    link.download = fileName
  }
  document.body.append(link)
  link.click()
  link.remove()
}

export default function AdminReportsPage() {
  const [form, setForm] = useState<{ reportType: ReportType; format: ReportFormat; from: string; to: string }>({
    reportType: 'orders',
    format: 'csv',
    from: '',
    to: '',
  })
  const [trackedIds, setTrackedIds] = useState<string[]>(() => loadStoredReportRequestIds())
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const requestKey = useMemo(() => trackedIds.join('|'), [trackedIds])
  const { data, loading, error, reload } = useAsyncData<TrackedReportRequest[]>(
    async () => {
      if (trackedIds.length === 0) {
        return []
      }

      const results = await Promise.allSettled(trackedIds.map((id) => reportsApi.getRequest(id)))

      return trackedIds.map((id, index) => {
        const result = results[index]
        if (result.status === 'fulfilled') {
          return { id, request: result.value, error: null }
        }

        return {
          id,
          request: null,
          error: result.reason instanceof Error ? result.reason.message : 'Unable to refresh this request.',
        }
      })
    },
    [requestKey],
  )

  useEffect(() => {
    saveStoredReportRequestIds(trackedIds)
  }, [trackedIds])

  const requests = useMemo(() => data ?? [], [data])
  const pendingKey = useMemo(
    () => requests.filter((entry) => entry.request && !isTerminal(entry.request)).map((entry) => entry.id).join('|'),
    [requests],
  )

  useEffect(() => {
    if (!pendingKey) {
      return
    }

    const timer = window.setInterval(() => {
      reload()
    }, 5000)

    return () => {
      window.clearInterval(timer)
    }
  }, [pendingKey, reload])

  const summary = useMemo(() => {
    const readyCount = requests.filter((entry) => entry.request && isReady(entry.request)).length
    const inProgressCount = requests.filter((entry) => entry.request && !isTerminal(entry.request)).length
    const failedCount = requests.filter((entry) => entry.request && !isReady(entry.request) && isTerminal(entry.request)).length

    return {
      totalCount: trackedIds.length,
      readyCount,
      inProgressCount,
      failedCount,
    }
  }, [requests, trackedIds.length])

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSubmitting(true)
    setActionError(null)
    setActionMessage(null)

    try {
      if (form.reportType === 'orders' && form.from && form.to && new Date(form.from) > new Date(form.to)) {
        throw new Error('The start date must be earlier than or equal to the end date.')
      }

      const payload = {
        reportType: form.reportType,
        format: form.format,
        ...(form.reportType === 'orders' && form.from ? { from: form.from } : {}),
        ...(form.reportType === 'orders' && form.to ? { to: form.to } : {}),
      }

      const created = await reportsApi.createRequest(payload)
      setTrackedIds((current) => [created.id, ...current.filter((id) => id !== created.id)].slice(0, 12))
      setActionMessage(`${labelForReportType(created.reportType)} ${labelForFormat(created.format)} export queued successfully.`)
    } catch (caughtError) {
      setActionError(caughtError instanceof Error ? caughtError.message : 'Unable to queue the export request.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleDownload = async (request: ReportRequest) => {
    setDownloadingId(request.id)
    setActionError(null)
    setActionMessage(null)

    try {
      const result = await reportsApi.getDownload(request.id)

      if (result.kind === 'url') {
        triggerDownload(result.url, result.fileName ?? defaultFileName(request))
      } else {
        const objectUrl = window.URL.createObjectURL(result.blob)
        triggerDownload(objectUrl, result.fileName ?? defaultFileName(request))
        window.setTimeout(() => {
          window.URL.revokeObjectURL(objectUrl)
        }, 1000)
      }

      setActionMessage(`Download started for request ${request.id}.`)
    } catch (caughtError) {
      setActionError(caughtError instanceof Error ? caughtError.message : 'Unable to start the download.')
    } finally {
      setDownloadingId(null)
    }
  }

  const removeTrackedRequest = (id: string) => {
    setTrackedIds((current) => current.filter((candidate) => candidate !== id))
  }

  return (
    <div className="container-shell py-14">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-4xl text-crimson">Admin reports</h1>
          <p className="mt-3 text-ink/70">Queue CSV or Excel exports, watch processing progress, and download finished files when Azure completes them.</p>
        </div>
        <Button variant="secondary" onClick={() => reload()}>
          <RefreshCcw className="mr-2 h-4 w-4" /> Refresh requests
        </Button>
      </div>

      <div className="mt-8 grid gap-4 md:grid-cols-3 xl:grid-cols-4">
        <Card>
          <FileSpreadsheet className="h-7 w-7 text-gold" />
          <p className="mt-5 text-sm font-bold uppercase tracking-[0.28em] text-ink/60">Tracked requests</p>
          <p className="mt-3 font-heading text-5xl text-crimson">{summary.totalCount}</p>
        </Card>
        <Card>
          <Clock3 className="h-7 w-7 text-copper" />
          <p className="mt-5 text-sm font-bold uppercase tracking-[0.28em] text-ink/60">In progress</p>
          <p className="mt-3 font-heading text-5xl text-crimson">{summary.inProgressCount}</p>
        </Card>
        <Card>
          <Download className="h-7 w-7 text-jade" />
          <p className="mt-5 text-sm font-bold uppercase tracking-[0.28em] text-ink/60">Ready</p>
          <p className="mt-3 font-heading text-5xl text-crimson">{summary.readyCount}</p>
        </Card>
        <Card>
          <Trash2 className="h-7 w-7 text-crimson" />
          <p className="mt-5 text-sm font-bold uppercase tracking-[0.28em] text-ink/60">Needs attention</p>
          <p className="mt-3 font-heading text-5xl text-crimson">{summary.failedCount}</p>
        </Card>
      </div>

      <div className="mt-8 grid gap-8 lg:grid-cols-[0.95fr,1.05fr]">
        <Card>
          <div className="flex items-center gap-3">
            <FileSpreadsheet className="h-5 w-5 text-gold" />
            <h2 className="text-2xl text-ink">Create export request</h2>
          </div>
          <form className="mt-6 grid gap-5" onSubmit={handleSubmit}>
            <label className="block">
              <span className="field-label">Report type</span>
              <select className="field-input" value={form.reportType} onChange={(event) => setForm((current) => ({ ...current, reportType: event.target.value as ReportType }))}>
                {reportTypes.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="block">
              <span className="field-label">Format</span>
              <select className="field-input" value={form.format} onChange={(event) => setForm((current) => ({ ...current, format: event.target.value as ReportFormat }))}>
                {formats.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <div className="grid gap-5 md:grid-cols-2">
              <label className="block">
                <span className="field-label">From</span>
                <input
                  className="field-input"
                  type="date"
                  value={form.from}
                  onChange={(event) => setForm((current) => ({ ...current, from: event.target.value }))}
                  disabled={form.reportType !== 'orders'}
                />
              </label>
              <label className="block">
                <span className="field-label">To</span>
                <input
                  className="field-input"
                  type="date"
                  value={form.to}
                  onChange={(event) => setForm((current) => ({ ...current, to: event.target.value }))}
                  disabled={form.reportType !== 'orders'}
                />
              </label>
            </div>

            <Alert
              tone="info"
              title={form.reportType === 'orders' ? 'Optional date window' : 'Inventory exports use the current snapshot'}
              message={form.reportType === 'orders' ? 'Leave either date blank to export the full order history for this report.' : 'The API ignores date filters for inventory, so the current stock state is exported immediately when the job is processed.'}
            />

            <div className="flex justify-end">
              <Button type="submit" loading={submitting}>
                Queue export
              </Button>
            </div>
          </form>
        </Card>

        <Card>
          <div className="flex items-center gap-3">
            <Clock3 className="h-5 w-5 text-copper" />
            <h2 className="text-2xl text-ink">Admin guidance</h2>
          </div>
          <div className="mt-6 space-y-4">
            {reportTypes.map((option) => (
              <div key={option.value} className="rounded-3xl border border-gold/25 bg-white/80 p-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-heading text-xl text-ink">{option.label} export</p>
                  <Badge tone={form.reportType === option.value ? 'success' : 'neutral'}>{form.reportType === option.value ? 'Selected' : 'Available'}</Badge>
                </div>
                <p className="mt-2 text-sm text-ink/65">{option.description}</p>
              </div>
            ))}
            <Alert
              tone="info"
              title="Recent requests stay in this browser"
              message="The current async API exposes create, status, and download endpoints, so this page keeps the latest request IDs in local storage for quick follow-up polling."
            />
          </div>
        </Card>
      </div>

      {actionError ? <div className="mt-6"><Alert tone="error" title="Report action failed" message={actionError} /></div> : null}
      {actionMessage ? <div className="mt-6"><Alert tone="success" title="Reports updated" message={actionMessage} /></div> : null}

      <Card className="mt-8 overflow-hidden p-0">
        {loading ? (
          <div className="p-6">
            <Alert tone="info" title="Refreshing report requests" message="Checking the latest status from the async export API." />
          </div>
        ) : error ? (
          <div className="p-6"><Alert tone="error" title="Reports unavailable" message={error} /></div>
        ) : requests.length === 0 ? (
          <div className="p-6">
            <Alert tone="info" title="No exports tracked yet" message="Queue a CSV or Excel report above to start tracking progress and download availability here." />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm text-ink/70">
              <thead className="bg-ink text-xs uppercase tracking-[0.22em] text-gold">
                <tr>
                  <th className="px-6 py-4">Request</th>
                  <th className="px-6 py-4">Window</th>
                  <th className="px-6 py-4">Created</th>
                  <th className="px-6 py-4">Updated</th>
                  <th className="px-6 py-4">Status</th>
                  <th className="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {requests.map((entry) => {
                  if (!entry.request) {
                    return (
                      <tr key={entry.id} className="border-t border-gold/20 bg-crimson/5">
                        <td className="px-6 py-4 font-semibold text-ink">{entry.id}</td>
                        <td className="px-6 py-4">—</td>
                        <td className="px-6 py-4">—</td>
                        <td className="px-6 py-4">—</td>
                        <td className="px-6 py-4"><Badge tone="danger">Unavailable</Badge></td>
                        <td className="px-6 py-4 text-right">
                          <div className="flex justify-end gap-2">
                            <Button variant="secondary" size="sm" onClick={() => reload()}>
                              Retry
                            </Button>
                            <Button variant="ghost" size="sm" onClick={() => removeTrackedRequest(entry.id)}>
                              Remove
                            </Button>
                          </div>
                          {entry.error ? <p className="mt-2 text-xs text-crimson">{entry.error}</p> : null}
                        </td>
                      </tr>
                    )
                  }

                  const request = entry.request

                  return (
                    <tr key={entry.id} className="border-t border-gold/20 bg-white/75">
                      <td className="px-6 py-4">
                        <p className="font-semibold text-ink">{labelForReportType(request.reportType)} · {labelForFormat(request.format)}</p>
                        <p className="mt-1 text-xs uppercase tracking-[0.16em] text-ink/50">{request.id}</p>
                      </td>
                      <td className="px-6 py-4">
                        <p>{request.reportType === 'orders' ? `${formatDateTime(request.from)} → ${formatDateTime(request.to)}` : 'Current inventory snapshot'}</p>
                      </td>
                      <td className="px-6 py-4">{formatDateTime(request.createdAt)}</td>
                      <td className="px-6 py-4">{formatDateTime(request.updatedAt || request.completedAt)}</td>
                      <td className="px-6 py-4">
                        <div className="space-y-2">
                          <Badge tone={getStatusTone(request.status)}>{request.status}</Badge>
                          {request.errorMessage ? <p className="text-xs text-crimson">{request.errorMessage}</p> : null}
                        </div>
                      </td>
                      <td className="px-6 py-4 text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="secondary" size="sm" onClick={() => reload()}>
                            Refresh
                          </Button>
                          <Button size="sm" onClick={() => handleDownload(request)} disabled={!isReady(request)} loading={downloadingId === entry.id}>
                            <Download className="mr-2 h-4 w-4" /> Download
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => removeTrackedRequest(entry.id)}>
                            Remove
                          </Button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  )
}
