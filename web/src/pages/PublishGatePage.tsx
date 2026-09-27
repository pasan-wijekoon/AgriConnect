import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useDevIdentity } from '../context/DevIdentityContext'
import { PageHeader } from '../components/ui/PageHeader'
import { Card } from '../components/ui/Card'
import { Badge } from '../components/ui/Badge'
import { Button } from '../components/ui/Button'
import { ErrorState, LoadingState } from '../components/ui/StateViews'
import { ApiError } from '../utils/ordersApi'
import { qualityApi, type AgentQualityValidation, type InspectionResponse } from '../utils/qualityApi'

type LoadState = { kind: 'loading' } | { kind: 'error'; message: string } | { kind: 'ready' }

/**
 * Component C — the FR5 gate itself: a listing may only become Published after
 * passing quality inspection. The Agentic AI evaluation below is a proposal an
 * officer reviews (CLAUDE.md §17/§18: AI never commits the decision) — Publish
 * always re-runs InspectionService's own deterministic checks server-side
 * regardless of what the agent recommended.
 */
export function PublishGatePage() {
  const { listingId } = useParams<{ listingId: string }>()
  const { identity } = useDevIdentity()
  const navigate = useNavigate()

  const [inspections, setInspections] = useState<InspectionResponse[]>([])
  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [evaluation, setEvaluation] = useState<AgentQualityValidation | null>(null)
  const [evaluating, setEvaluating] = useState(false)
  const [publishing, setPublishing] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [publishedMessage, setPublishedMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!listingId) return
    setState({ kind: 'loading' })
    qualityApi
      .getListingInspections(identity, listingId)
      .then((items) => {
        setInspections(items)
        setState({ kind: 'ready' })
      })
      .catch((err: unknown) => {
        const message = err instanceof ApiError ? err.message : 'We couldn’t retrieve this listing’s inspection history.'
        setState({ kind: 'error', message })
      })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [identity, listingId])

  if (!listingId) {
    return <ErrorState message="No listing was specified." />
  }

  const latestInspection = inspections[0]

  const handleEvaluate = async () => {
    setActionError(null)
    setEvaluating(true)
    try {
      setEvaluation(await qualityApi.evaluateListingCompliance(identity, listingId))
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Agent evaluation failed.')
    } finally {
      setEvaluating(false)
    }
  }

  const handlePublish = async () => {
    setActionError(null)
    setPublishedMessage(null)
    setPublishing(true)
    try {
      const result = await qualityApi.publishListing(identity, listingId)
      setPublishedMessage(`Listing published (status: ${result.status}).`)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Publish failed.')
    } finally {
      setPublishing(false)
    }
  }

  return (
    <div>
      <PageHeader
        title="Publish Gate"
        description="FR5: a listing can only be published after it passes quality inspection."
        action={
          <Button variant="secondary" onClick={() => navigate('/quality/inspections')}>
            Back to Queue
          </Button>
        }
      />

      {state.kind === 'loading' && <LoadingState label="Loading inspection history…" />}
      {state.kind === 'error' && <ErrorState message={state.message} />}

      {state.kind === 'ready' && (
        <>
          <Card>
            <h3>Inspection Status</h3>
            {latestInspection ? (
              <p>
                Latest confirmed grade: <Badge tone="info">{latestInspection.confirmedGrade}</Badge>{' '}
                {latestInspection.hasDiscrepancy && <Badge tone="warning">Unresolved discrepancy</Badge>}
              </p>
            ) : (
              <p>This listing has not been inspected yet. Record an inspection before publishing.</p>
            )}
          </Card>

          <Card>
            <h3>Agentic AI Quality & Compliance Evaluation (proposal only)</h3>
            <p>
              This is an AI-generated recommendation, not a final decision — the officer's Publish action below always
              re-checks the real gate rules server-side.
            </p>
            <Button variant="secondary" loading={evaluating} onClick={handleEvaluate}>
              Run Evaluation
            </Button>
            {evaluation && (
              <div style={{ marginTop: 'var(--space-4)' }}>
                <p>
                  Recommendation: <Badge tone={evaluation.passed ? 'success' : 'warning'}>{evaluation.recommendedAction}</Badge>
                </p>
                <p>{evaluation.reasoningSummary}</p>
                {evaluation.flags.length > 0 && (
                  <ul>
                    {evaluation.flags.map((flag) => (
                      <li key={flag}>{flag}</li>
                    ))}
                  </ul>
                )}
              </div>
            )}
          </Card>

          {actionError && <ErrorState message={actionError} />}
          {publishedMessage && (
            <Card>
              <Badge tone="success">{publishedMessage}</Badge>
            </Card>
          )}

          <Card>
            <Button variant="primary" loading={publishing} onClick={handlePublish}>
              Publish Listing
            </Button>
          </Card>
        </>
      )}
    </div>
  )
}
