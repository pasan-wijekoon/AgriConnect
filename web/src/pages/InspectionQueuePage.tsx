import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useDevIdentity } from '../context/DevIdentityContext'
import { PageHeader } from '../components/ui/PageHeader'
import { Card } from '../components/ui/Card'
import { Badge, type BadgeTone } from '../components/ui/Badge'
import { Button } from '../components/ui/Button'
import { EmptyState, ErrorState, LoadingState } from '../components/ui/StateViews'
import { ApiError } from '../utils/ordersApi'
import { qualityApi, type ListingSummary } from '../utils/qualityApi'
import '../styles/data-table.css'

type LoadState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready' }

function gradeTone(grade?: string): BadgeTone {
  switch (grade) {
    case 'Grade A':
      return 'success'
    case 'Grade B':
      return 'info'
    case 'Grade C':
      return 'warning'
    case 'Rejected':
      return 'error'
    default:
      return 'neutral'
  }
}

/**
 * Component C — Quality Grading & Inspection (FR12–FR14/FR5). Listings awaiting
 * officer inspection, with a Record Inspection action and — once inspected — a
 * link into the FR5 publish gate. Mirrors OrderQueuePage.tsx's conventions
 * (PageHeader/Card/StateViews, self-fetching via useDevIdentity()) rather than
 * the page's original prop-drilled, standalone-app-shell design.
 */
export function InspectionQueuePage() {
  const { identity } = useDevIdentity()
  const navigate = useNavigate()
  const [listings, setListings] = useState<ListingSummary[]>([])
  const [state, setState] = useState<LoadState>({ kind: 'loading' })

  const load = () => {
    setState({ kind: 'loading' })
    qualityApi
      .getPendingListings(identity)
      .then((items) => {
        setListings(items)
        setState({ kind: 'ready' })
      })
      .catch((err: unknown) => {
        const message = err instanceof ApiError ? err.message : 'We couldn’t retrieve listings awaiting inspection.'
        setState({ kind: 'error', message })
      })
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [identity])

  return (
    <div>
      <PageHeader
        title="Quality Inspection Queue"
        description="Listings awaiting officer quality verification before they can be published (FR5, FR12)."
      />

      <Card>
        {state.kind === 'loading' && <LoadingState label="Loading listings…" />}
        {state.kind === 'error' && <ErrorState message={state.message} onRetry={load} />}
        {state.kind === 'ready' && listings.length === 0 && (
          <EmptyState title="Nothing to inspect" description="No listings are currently awaiting quality inspection." />
        )}
        {state.kind === 'ready' && listings.length > 0 && (
          <table className="data-table">
            <thead>
              <tr>
                <th>Crop</th>
                <th>Farmer</th>
                <th>Region</th>
                <th>Qty</th>
                <th>Claimed Grade</th>
                <th>Confirmed Grade</th>
                <th>Status</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {listings.map((listing) => (
                <tr key={listing.id}>
                  <td data-label="Crop">{listing.cropName}</td>
                  <td data-label="Farmer">{listing.farmerName}</td>
                  <td data-label="Region">{listing.regionName}</td>
                  <td data-label="Qty">
                    {listing.quantity}
                    {listing.unit}
                  </td>
                  <td data-label="Claimed Grade">
                    <Badge tone={gradeTone(listing.claimedGrade)}>{listing.claimedGrade}</Badge>
                  </td>
                  <td data-label="Confirmed Grade">
                    {listing.latestConfirmedGrade ? (
                      <Badge tone={gradeTone(listing.latestConfirmedGrade)}>{listing.latestConfirmedGrade}</Badge>
                    ) : (
                      <Badge tone="pending">Not inspected</Badge>
                    )}
                    {listing.hasUnresolvedDiscrepancy && (
                      <Badge tone="warning">Discrepancy</Badge>
                    )}
                  </td>
                  <td data-label="Status">
                    <Badge tone={listing.status === 'Published' ? 'success' : 'pending'}>{listing.status}</Badge>
                  </td>
                  <td data-label="" style={{ display: 'flex', gap: 'var(--space-2)' }}>
                    <Button variant="secondary" onClick={() => navigate(`/quality/inspections/${listing.id}/record`)}>
                      Record Inspection
                    </Button>
                    <Button variant="primary" onClick={() => navigate(`/quality/publish/${listing.id}`)}>
                      Publish Gate
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
