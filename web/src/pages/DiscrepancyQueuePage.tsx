import { useEffect, useState } from 'react'
import { useDevIdentity } from '../context/DevIdentityContext'
import { PageHeader } from '../components/ui/PageHeader'
import { Card } from '../components/ui/Card'
import { Badge } from '../components/ui/Badge'
import { Button } from '../components/ui/Button'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/StateViews'
import { ApiError } from '../utils/ordersApi'
import { qualityApi, type GradeDiscrepancy } from '../utils/qualityApi'
import '../styles/data-table.css'

type LoadState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready' }

/**
 * Component C — FR14: listings where the farmer's claimed grade didn't match
 * the officer's confirmed grade. Must be resolved before the listing can pass
 * the FR5 publish gate (InspectionService.PublishListingWithGateCheckAsync).
 */
export function DiscrepancyQueuePage() {
  const { identity } = useDevIdentity()
  const [discrepancies, setDiscrepancies] = useState<GradeDiscrepancy[]>([])
  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [resolvingId, setResolvingId] = useState<string | null>(null)
  const [notesById, setNotesById] = useState<Record<string, string>>({})
  const [actionError, setActionError] = useState<string | null>(null)

  const load = () => {
    setState({ kind: 'loading' })
    qualityApi
      .getDiscrepancies(identity, true)
      .then((items) => {
        setDiscrepancies(items)
        setState({ kind: 'ready' })
      })
      .catch((err: unknown) => {
        const message = err instanceof ApiError ? err.message : 'We couldn’t retrieve grade discrepancies.'
        setState({ kind: 'error', message })
      })
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [identity])

  const handleResolve = async (flag: GradeDiscrepancy) => {
    setActionError(null)
    setResolvingId(flag.id)
    try {
      await qualityApi.resolveDiscrepancy(identity, flag.id, notesById[flag.id]?.trim() || 'Resolved by officer.')
      load()
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Failed to resolve the discrepancy.')
    } finally {
      setResolvingId(null)
    }
  }

  return (
    <div>
      <PageHeader
        title="Grade Discrepancies"
        description="Claimed-vs-confirmed grade mismatches that must be resolved before publishing (FR14)."
      />

      {actionError && <ErrorState message={actionError} />}

      <Card>
        {state.kind === 'loading' && <LoadingState label="Loading discrepancies…" />}
        {state.kind === 'error' && <ErrorState message={state.message} onRetry={load} />}
        {state.kind === 'ready' && discrepancies.length === 0 && (
          <EmptyState title="No open discrepancies" description="Every flagged grade mismatch has been resolved." />
        )}
        {state.kind === 'ready' && discrepancies.length > 0 && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Crop</th>
                <th>Farmer</th>
                <th>Claimed</th>
                <th>Confirmed</th>
                <th>Flagged</th>
                <th>Resolution Notes</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {discrepancies.map((flag) => (
                <tr key={flag.id}>
                  <td data-label="Crop">{flag.cropName}</td>
                  <td data-label="Farmer">{flag.farmerName}</td>
                  <td data-label="Claimed">
                    <Badge tone="neutral">{flag.claimedGrade}</Badge>
                  </td>
                  <td data-label="Confirmed">
                    <Badge tone="warning">{flag.confirmedGrade}</Badge>
                  </td>
                  <td data-label="Flagged">{new Date(flag.flaggedAt).toLocaleDateString()}</td>
                  <td data-label="Resolution Notes">
                    <input
                      type="text"
                      placeholder="Resolution notes"
                      value={notesById[flag.id] ?? ''}
                      onChange={(e) => setNotesById((prev) => ({ ...prev, [flag.id]: e.target.value }))}
                    />
                  </td>
                  <td data-label="">
                    <Button variant="primary" loading={resolvingId === flag.id} onClick={() => handleResolve(flag)}>
                      Resolve
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  )
}
