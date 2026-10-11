import { useState, useEffect, useCallback, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { PageHeader } from '../components/ui/PageHeader'
import { Card } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Badge, type BadgeTone } from '../components/ui/Badge'
import { LoadingState, ErrorState } from '../components/ui/StateViews'
import { Toast, type ToastData } from '../components/Toast'
import { ApiError } from '../utils/ordersApi'
import { qualityApi, QUALITY_GRADES, type ListingSummary } from '../utils/qualityApi'
import './RecordInspectionPage.css'

/**
 * Component C — record an officer's inspection outcome for a listing (FR12,
 * FR14). Loads listing context so the officer can see what they are inspecting,
 * pre-selects the confirmed grade to the claimed grade, supports multiple photo
 * URLs, and gives inline feedback on success and failure.
 */

type PageState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; listing: ListingSummary }

function gradeTone(grade?: string): BadgeTone {
  switch (grade) {
    case 'Grade A': return 'success'
    case 'Grade B': return 'info'
    case 'Grade C': return 'warning'
    case 'Rejected': return 'error'
    default: return 'neutral'
  }
}

export function RecordInspectionPage() {
  const { listingId } = useParams<{ listingId: string }>()
  const navigate = useNavigate()

  // ── Page-level load state ──────────────────────────────────────────────
  const [pageState, setPageState] = useState<PageState>({ kind: 'loading' })

  // ── Form field state ───────────────────────────────────────────────────
  const [confirmedGrade, setConfirmedGrade] = useState<string>(QUALITY_GRADES[0])
  const [notes, setNotes] = useState('')
  const [photoUrls, setPhotoUrls] = useState<string[]>([])
  const [photoInput, setPhotoInput] = useState('')

  // ── Submission state ───────────────────────────────────────────────────
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastData | null>(null)

  // ── Load listing context ───────────────────────────────────────────────
  const loadListing = useCallback(() => {
    if (!listingId) return
    setPageState({ kind: 'loading' })
    qualityApi
      .getPendingListings()
      .then((items) => {
        const listing = items.find((l) => l.id === listingId)
        if (!listing) {
          setPageState({
            kind: 'error',
            message: 'This listing was not found in the inspection queue. It may have already been inspected or withdrawn.',
          })
          return
        }
        setPageState({ kind: 'ready', listing })
        // Pre-select confirmed grade to match the listing's claimed grade
        setConfirmedGrade(listing.claimedGrade)
      })
      .catch((err: unknown) => {
        const message =
          err instanceof ApiError
            ? err.message
            : 'Failed to load the listing. Check your connection and try again.'
        setPageState({ kind: 'error', message })
      })
  }, [listingId])

  useEffect(() => {
    loadListing()
  }, [loadListing])

  // ── Guard: no listingId in the URL ─────────────────────────────────────
  if (!listingId) {
    return <ErrorState message="No listing was specified in the URL." />
  }

  // ── Loading / error screens ────────────────────────────────────────────
  if (pageState.kind === 'loading') {
    return <LoadingState label="Loading listing details…" />
  }

  if (pageState.kind === 'error') {
    return (
      <div>
        <PageHeader title="Record Inspection" />
        <ErrorState message={pageState.message} onRetry={loadListing} />
      </div>
    )
  }

  const { listing } = pageState

  // ── Photo URL helpers ──────────────────────────────────────────────────
  const handleAddPhoto = () => {
    const url = photoInput.trim()
    if (!url) return
    setPhotoUrls((prev) => [...prev, url])
    setPhotoInput('')
  }

  const handleRemovePhoto = (index: number) => {
    setPhotoUrls((prev) => prev.filter((_, i) => i !== index))
  }

  // ── Form submission ────────────────────────────────────────────────────
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setFormError(null)
    setSubmitting(true)
    try {
      await qualityApi.recordInspection({
        listingId,
        confirmedGrade,
        notes: notes.trim() || undefined,
        photoUrls,
      })
      setToast({
        kind: 'success',
        title: 'Inspection recorded',
        message: `${listing.cropName} (${listing.farmerName}) has been graded ${confirmedGrade}.`,
      })
      // Brief delay so the toast is visible before navigating away
      setTimeout(() => navigate('/quality/inspections'), 1800)
    } catch (err) {
      setFormError(
        err instanceof ApiError ? err.message : 'Failed to record the inspection. Please try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div>
      <PageHeader
        title="Record Inspection"
        description="Confirm the quality grade for this listing after physical inspection."
      />

      <Card>
        {/* ── Listing context summary ── */}
        <div className="ri-summary">
          <div className="ri-summary-item">
            <span className="ri-summary-label">Crop</span>
            <span className="ri-summary-value ri-summary-crop">{listing.cropName}</span>
          </div>
          <div className="ri-summary-item">
            <span className="ri-summary-label">Farmer</span>
            <span className="ri-summary-value">{listing.farmerName}</span>
          </div>
          <div className="ri-summary-item">
            <span className="ri-summary-label">Quantity</span>
            <span className="ri-summary-value">
              {listing.quantity} {listing.unit}
            </span>
          </div>
          <div className="ri-summary-item">
            <span className="ri-summary-label">Claimed Grade</span>
            <span className="ri-summary-value">
              <Badge tone={gradeTone(listing.claimedGrade)}>{listing.claimedGrade}</Badge>
            </span>
          </div>
          <div className="ri-summary-item">
            <span className="ri-summary-label">Region</span>
            <span className="ri-summary-value">{listing.regionName}</span>
          </div>
        </div>

        {/* ── Inline form error ── */}
        {formError && (
          <div className="notice notice-error" style={{ marginBottom: 'var(--space-4)' }}>
            {formError}
          </div>
        )}

        {/* ── Inspection form ── */}
        <form onSubmit={handleSubmit} className="ri-form">
          {/* Confirmed grade */}
          <div className="field">
            <label htmlFor="ri-grade">Confirmed Grade</label>
            <select
              id="ri-grade"
              value={confirmedGrade}
              onChange={(e) => {
                setConfirmedGrade(e.target.value)
                setFormError(null)
              }}
              required
            >
              {QUALITY_GRADES.map((grade) => (
                <option key={grade} value={grade}>
                  {grade}
                </option>
              ))}
            </select>
          </div>

          {/* Notes */}
          <div className="field">
            <label htmlFor="ri-notes">Notes</label>
            <textarea
              id="ri-notes"
              value={notes}
              rows={4}
              placeholder="Optional — describe observations, defects, packaging condition, etc."
              onChange={(e) => {
                setNotes(e.target.value)
                setFormError(null)
              }}
            />
          </div>

          {/* Photo URLs */}
          <div className="field">
            <label>Photo URLs</label>
            <div className="ri-photo-add-row">
              <input
                type="url"
                className="form-input"
                value={photoInput}
                placeholder="https://example.com/photo.jpg"
                onChange={(e) => {
                  setPhotoInput(e.target.value)
                  setFormError(null)
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault()
                    handleAddPhoto()
                  }
                }}
              />
              <Button
                type="button"
                variant="secondary"
                onClick={handleAddPhoto}
                disabled={!photoInput.trim()}
              >
                Add
              </Button>
            </div>

            {photoUrls.length > 0 && (
              <ul className="ri-photo-list" aria-label="Added photo URLs">
                {photoUrls.map((url, idx) => (
                  <li key={idx} className="ri-photo-item">
                    <span className="ri-photo-url" title={url}>
                      {url}
                    </span>
                    <button
                      type="button"
                      className="ri-photo-remove"
                      aria-label={`Remove photo ${idx + 1}`}
                      onClick={() => handleRemovePhoto(idx)}
                    >
                      ×
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Actions */}
          <div className="ri-actions">
            <Button type="submit" variant="primary" loading={submitting}>
              Submit Inspection
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={submitting}
              onClick={() => navigate(-1)}
            >
              Cancel
            </Button>
          </div>
        </form>
      </Card>

      {/* ── Success toast ── */}
      <Toast toast={toast} onClose={() => setToast(null)} />
    </div>
  )
}
