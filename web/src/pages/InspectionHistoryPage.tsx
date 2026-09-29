import { useEffect, useState } from 'react'

import { PageHeader } from '../components/ui/PageHeader'
import { Card } from '../components/ui/Card'
import { Badge } from '../components/ui/Badge'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/StateViews'
import { ApiError } from '../utils/ordersApi'
import { qualityApi, QUALITY_GRADES, type InspectionResponse } from '../utils/qualityApi'
import '../styles/data-table.css'

type LoadState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready' }

/**
 * Component C — FR13: full, searchable/filterable inspection history.
 */
export function InspectionHistoryPage() {
  
  const [search, setSearch] = useState('')
  const [grade, setGrade] = useState<string>('')
  const [inspections, setInspections] = useState<InspectionResponse[]>([])
  const [state, setState] = useState<LoadState>({ kind: 'loading' })

  const load = () => {
    setState({ kind: 'loading' })
    qualityApi
      .getInspections({ search: search || undefined, grade: grade || undefined })
      .then((result) => {
        setInspections(result.items)
        setState({ kind: 'ready' })
      })
      .catch((err: unknown) => {
        const message = err instanceof ApiError ? err.message : 'We couldn’t retrieve inspection history.'
        setState({ kind: 'error', message })
      })
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search, grade])

  return (
    <div>
      <PageHeader title="Inspection History" description="Every quality inspection recorded, searchable and filterable (FR13)." />

      <div style={{ display: 'flex', gap: 'var(--space-3)', marginBottom: 'var(--space-4)' }}>
        <input
          type="search"
          placeholder="Search crop, farmer, officer, notes…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select value={grade} onChange={(e) => setGrade(e.target.value)}>
          <option value="">All grades</option>
          {QUALITY_GRADES.map((g) => (
            <option key={g} value={g}>
              {g}
            </option>
          ))}
        </select>
      </div>

      <Card>
        {state.kind === 'loading' && <LoadingState label="Loading inspections…" />}
        {state.kind === 'error' && <ErrorState message={state.message} onRetry={load} />}
        {state.kind === 'ready' && inspections.length === 0 && (
          <EmptyState title="No inspections found" description="No inspections match your current search/filter." />
        )}
        {state.kind === 'ready' && inspections.length > 0 && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Crop</th>
                <th>Farmer</th>
                <th>Officer</th>
                <th>Claimed</th>
                <th>Confirmed</th>
                <th>Inspected</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              {inspections.map((inspection) => (
                <tr key={inspection.id}>
                  <td data-label="Crop">{inspection.cropName}</td>
                  <td data-label="Farmer">{inspection.farmerName}</td>
                  <td data-label="Officer">{inspection.officerName}</td>
                  <td data-label="Claimed">
                    <Badge tone="neutral">{inspection.claimedGrade}</Badge>
                  </td>
                  <td data-label="Confirmed">
                    <Badge tone={inspection.hasDiscrepancy ? 'warning' : 'success'}>{inspection.confirmedGrade}</Badge>
                  </td>
                  <td data-label="Inspected">{new Date(inspection.inspectedAt).toLocaleDateString()}</td>
                  <td data-label="Notes">{inspection.notes ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  )
}
