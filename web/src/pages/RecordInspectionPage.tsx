import { useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useDevIdentity } from '../context/DevIdentityContext'
import { PageHeader } from '../components/ui/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { ErrorState } from '../components/ui/StateViews'
import { ApiError } from '../utils/ordersApi'
import { qualityApi, QUALITY_GRADES } from '../utils/qualityApi'

/**
 * Component C — record an officer's inspection outcome for a listing (FR12,
 * FR14). A mismatch between the listing's claimed grade and the confirmed
 * grade entered here is flagged automatically by the backend, not this page.
 */
export function RecordInspectionPage() {
  const { listingId } = useParams<{ listingId: string }>()
  const { identity } = useDevIdentity()
  const navigate = useNavigate()

  const [confirmedGrade, setConfirmedGrade] = useState<string>(QUALITY_GRADES[0])
  const [notes, setNotes] = useState('')
  const [photoUrl, setPhotoUrl] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!listingId) {
    return <ErrorState message="No listing was specified." />
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await qualityApi.recordInspection(identity, {
        listingId,
        confirmedGrade,
        notes: notes.trim() || undefined,
        photoUrls: photoUrl.trim() ? [photoUrl.trim()] : [],
      })
      navigate('/quality/inspections')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to record the inspection.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div>
      <PageHeader title="Record Inspection" description="Confirm the quality grade for this listing after physical inspection." />

      {error && <ErrorState message={error} />}

      <Card>
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)', maxWidth: 480 }}>
          <label>
            Confirmed Grade
            <select value={confirmedGrade} onChange={(e) => setConfirmedGrade(e.target.value)} required>
              {QUALITY_GRADES.map((grade) => (
                <option key={grade} value={grade}>
                  {grade}
                </option>
              ))}
            </select>
          </label>

          <label>
            Notes
            <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={4} placeholder="Optional inspection notes" />
          </label>

          <label>
            Photo URL
            <input
              type="url"
              value={photoUrl}
              onChange={(e) => setPhotoUrl(e.target.value)}
              placeholder="Optional — a link to an inspection photo"
            />
          </label>

          <div style={{ display: 'flex', gap: 'var(--space-3)' }}>
            <Button type="submit" variant="primary" loading={submitting}>
              Submit Inspection
            </Button>
            <Button type="button" variant="secondary" onClick={() => navigate('/quality/inspections')}>
              Cancel
            </Button>
          </div>
        </form>
      </Card>
    </div>
  )
}
