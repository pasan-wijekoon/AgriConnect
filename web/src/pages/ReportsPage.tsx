import { useState, type FormEvent } from 'react'

import { Card, EmptyState, ErrorNotice, Field, Icon, PageHeader } from '../components/ui.tsx'
import { DEV_ROLE_SWITCH, setRole, useRole } from '../context/session.ts'
import { downloadUrl, exportReport, getReport, type ReportExport, type ReportType } from '../utils/api.ts'
import { daysAgo, formatDateTime } from '../utils/format.ts'
import { ApiError } from '../utils/problem.ts'

const TYPES: { value: ReportType; label: string; note?: string }[] = [
  { value: 'PriceTrends', label: 'Price trends' },
  { value: 'Listings', label: 'Listings', note: 'Header only until Component A (Listings) is integrated.' },
  { value: 'Orders', label: 'Orders', note: 'Header only until Component B (Orders) is integrated.' },
]

const HISTORY_KEY = 'agriconnect.recentReports'

interface RecentReport extends ReportExport {
  from: string
  to: string
}

function readHistory(): RecentReport[] {
  try {
    return JSON.parse(localStorage.getItem(HISTORY_KEY) ?? '[]') as RecentReport[]
  } catch {
    return []
  }
}

function writeHistory(items: RecentReport[]) {
  try {
    localStorage.setItem(HISTORY_KEY, JSON.stringify(items))
  } catch {
    // Storage unavailable; the list just won't persist.
  }
}

const typeLabel = (t: ReportType) => TYPES.find((x) => x.value === t)?.label ?? t

export function ReportsPage() {
  const role = useRole()
  return (
    <>
      <PageHeader title="Reports" description="Export summary reports of listings, orders and price trends as CSV (FR18)." />
      {role === 'Administrator' ? (
        <Reports />
      ) : (
        <Card>
          <EmptyState title="Reports are for administrators">
            Your role is Officer. Ask an administrator to export a report.
            {DEV_ROLE_SWITCH && (
              <div className="empty-action">
                <button type="button" className="btn" onClick={() => setRole('Administrator')}>
                  Switch to Administrator (development)
                </button>
              </div>
            )}
          </EmptyState>
        </Card>
      )}
    </>
  )
}

function Reports() {
  const [type, setType] = useState<ReportType>('PriceTrends')
  const [from, setFrom] = useState(daysAgo(90))
  const [to, setTo] = useState(daysAgo(0))
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const [latest, setLatest] = useState<RecentReport | null>(null)
  const [history, setHistory] = useState<RecentReport[]>(readHistory)

  const fieldError = (field: string) => (error instanceof ApiError ? error.fieldErrors[field]?.[0] : undefined)
  const rangeInvalid = from && to && to < from

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (rangeInvalid) return
    setSubmitting(true)
    setError(null)
    try {
      const report = await exportReport({ type, dateRangeStart: from, dateRangeEnd: to })
      const entry = { ...report, from, to }
      setLatest(entry)
      const next = [entry, ...history.filter((h) => h.id !== entry.id)].slice(0, 10)
      setHistory(next)
      writeHistory(next)
    } catch (err) {
      setError(err as Error)
    } finally {
      setSubmitting(false)
    }
  }

  const selectedNote = TYPES.find((t) => t.value === type)?.note

  return (
    <>
      <Card title="Generate a report">
        <form className="report-form" onSubmit={submit} noValidate>
          <fieldset className="type-options">
            <legend>Report type</legend>
            {TYPES.map((t) => (
              <label key={t.value} className={`type-option${type === t.value ? ' is-selected' : ''}`}>
                <input type="radio" name="type" value={t.value} checked={type === t.value} onChange={() => setType(t.value)} />
                {t.label}
              </label>
            ))}
          </fieldset>
          {selectedNote && <p className="muted">{selectedNote}</p>}
          <div className="filters">
            <Field label="From">
              <input type="date" value={from} max={to} required onChange={(e) => setFrom(e.target.value)} aria-invalid={!!fieldError('dateRangeStart')} />
            </Field>
            <Field label="To">
              <input type="date" value={to} min={from} required onChange={(e) => setTo(e.target.value)} aria-invalid={!!rangeInvalid || !!fieldError('dateRangeEnd')} />
            </Field>
          </div>
          {rangeInvalid && <p className="field-error" role="alert">"To" must be on or after "From".</p>}
          {error && <ErrorNotice error={error} />}
          <div>
            <button type="submit" className="btn btn-primary" disabled={submitting || !from || !to || !!rangeInvalid}>
              <Icon name="file" />
              {submitting ? 'Generating…' : 'Generate CSV'}
            </button>
          </div>
        </form>
      </Card>

      {latest && (
        <Card title="Report ready" className="card-success">
          <ReportSummary report={latest} range={`${latest.from} to ${latest.to}`} />
        </Card>
      )}

      <Card title="Recent reports" actions={<span className="muted small">Generated in this browser</span>}>
        {history.length === 0 ? (
          <EmptyState title="No reports yet">Reports you generate appear here.</EmptyState>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th scope="col">Generated</th>
                  <th scope="col">Type</th>
                  <th scope="col">Range</th>
                  <th scope="col"><span className="visually-hidden">Download</span></th>
                </tr>
              </thead>
              <tbody>
                {history.map((h) => (
                  <tr key={h.id}>
                    <td className="nowrap">{formatDateTime(h.generatedAt)}</td>
                    <td>{typeLabel(h.type)}</td>
                    <td className="nowrap">{h.from} to {h.to}</td>
                    <td className="num">
                      <a className="btn btn-small" href={downloadUrl(h.fileUrl)} download>
                        <Icon name="download" /> CSV
                      </a>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <LookupReport />
    </>
  )
}

function LookupReport() {
  const [id, setId] = useState('')
  const [found, setFound] = useState<ReportExport | null>(null)
  const [error, setError] = useState<Error | null>(null)
  const [busy, setBusy] = useState(false)

  const lookup = async (e: FormEvent) => {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setFound(null)
    try {
      setFound(await getReport(id.trim()))
    } catch (err) {
      setError(err as Error)
    } finally {
      setBusy(false)
    }
  }

  const looksLikeId = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id.trim())

  return (
    <Card title="Find a report by ID">
      <form className="lookup" onSubmit={lookup}>
        <Field label="Report ID">
          <input value={id} onChange={(e) => setId(e.target.value)} placeholder="e.g. 8d48f339-ecdd-4ba1-8c73-448641025276" spellCheck={false} />
        </Field>
        <button type="submit" className="btn" disabled={busy || !looksLikeId}>
          {busy ? 'Looking up…' : 'Look up'}
        </button>
      </form>
      {error && <ErrorNotice error={error} />}
      {found && <ReportSummary report={found} />}
    </Card>
  )
}

function ReportSummary({ report, range }: { report: ReportExport; range?: string }) {
  return (
    <div className="report-summary">
      <dl className="facts">
        <div><dt>Type</dt><dd>{typeLabel(report.type)}</dd></div>
        {range && <div><dt>Range</dt><dd>{range}</dd></div>}
        <div><dt>Generated</dt><dd>{formatDateTime(report.generatedAt)}</dd></div>
        <div><dt>Report ID</dt><dd className="mono">{report.id}</dd></div>
      </dl>
      <a className="btn btn-primary" href={downloadUrl(report.fileUrl)} download>
        <Icon name="download" /> Download CSV
      </a>
    </div>
  )
}
