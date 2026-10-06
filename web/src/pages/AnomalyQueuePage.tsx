import { useState } from 'react'

import { Drawer } from '../components/Drawer.tsx'
import { Card, EmptyState, ErrorNotice, Field, Icon, PageHeader, Pill, StatusBadge, SuccessNotice } from '../components/ui.tsx'
import {
  getAnomalies,
  getFilters,
  investigateListing,
  updateAnomalyStatus,
  type AnalyticsFilters,
  type AnomalyFlag,
  type ExternalContext,
} from '../utils/api.ts'
import {
  ANOMALY_STATUSES, adviceFor, causeLabel, confidenceLabel, describeDeviation, percentileSentence, statusLabel, type AnomalyStatus,
} from '../utils/anomalies.ts'
import { formatDateTime, formatLkr } from '../utils/format.ts'
import { useAsync } from '../utils/useAsync.ts'

const PAGE_SIZE = 10
type Tab = AnomalyStatus | 'All'
const TABS: Tab[] = [...ANOMALY_STATUSES, 'All']

export function AnomalyQueuePage() {
  const filters = useAsync(getFilters, 'filters')
  return (
    <>
      <PageHeader
        title="Prices to check"
        description="These listings are priced much higher or much lower than the AI fair price. Open each one, read the details, then mark it as checked or ignore it."
      />
      {filters.error && !filters.data && <ErrorNotice error={filters.error} onRetry={filters.reload} />}
      {!filters.data && filters.loading && <p className="muted">Loading…</p>}
      {filters.data && <Queue filters={filters.data} />}
    </>
  )
}

function Queue({ filters }: { filters: AnalyticsFilters }) {
  const [tab, setTab] = useState<Tab>('Open')
  const [cropId, setCropId] = useState('')
  const [page, setPage] = useState(1)
  const [version, setVersion] = useState(0)
  const [investigating, setInvestigating] = useState<AnomalyFlag | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const cropName = new Map(filters.crops.map((c) => [c.id, c.name]))
  const regionName = new Map(filters.regions.map((r) => [r.id, r.name]))
  const place = (f: AnomalyFlag) => `${cropName.get(f.cropId) ?? 'Unknown crop'} · ${regionName.get(f.regionId) ?? 'Unknown region'}`

  const list = useAsync(
    () => getAnomalies({ status: tab === 'All' ? null : tab, cropId: cropId || null, page, size: PAGE_SIZE }),
    `${tab}|${cropId}|${page}|${version}`,
  )

  // One tiny request per tab keeps the counts honest under the crop filter.
  const counts = useAsync(
    async () => {
      const totals = await Promise.all(
        TABS.map((t) => getAnomalies({ status: t === 'All' ? null : t, cropId: cropId || null, page: 1, size: 1 }).then((r) => r.total)),
      )
      return Object.fromEntries(TABS.map((t, i) => [t, totals[i]])) as Record<Tab, number>
    },
    `${cropId}|${version}`,
  )

  const selectTab = (t: Tab) => {
    setTab(t)
    setPage(1)
  }

  const onUpdated = (flag: AnomalyFlag, status: 'Reviewed' | 'Dismissed') => {
    setInvestigating(null)
    setMessage(`${place(flag)}: marked as ${status === 'Reviewed' ? 'checked' : 'ignored'}.`)
    setVersion((v) => v + 1)
  }

  const data = list.data
  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1

  return (
    <>
      <div className="filters">
        <div className="tabs" role="tablist" aria-label="Show prices by status">
          {TABS.map((t) => (
            <button
              key={t}
              type="button"
              role="tab"
              aria-selected={tab === t}
              className={`tab${tab === t ? ' is-active' : ''}`}
              onClick={() => selectTab(t)}
            >
              {t === 'All' ? 'All' : statusLabel(t)}
              {counts.data && <span className="tab-count tabular">{counts.data[t]}</span>}
            </button>
          ))}
        </div>
        <Field label="Crop">
          <select
            value={cropId}
            onChange={(e) => {
              setCropId(e.target.value)
              setPage(1)
            }}
          >
            <option value="">All crops</option>
            {filters.crops.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </Field>
      </div>

      {message && <SuccessNotice onDismiss={() => setMessage(null)}>{message}</SuccessNotice>}
      {list.error && <ErrorNotice error={list.error} onRetry={list.reload} />}

      <Card>
        {!data && list.loading && <p className="muted">Loading prices…</p>}
        {data && data.items.length === 0 && (
          <EmptyState title={tab === 'Open' ? 'Nothing to check' : 'Nothing here'}>
            {tab === 'Open' ? 'Every flagged price has been checked or ignored.' : 'No prices match these filters.'}
          </EmptyState>
        )}
        {data && data.items.length > 0 && (
          <div className={list.loading ? 'refetching' : undefined}>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th scope="col">Date</th>
                    <th scope="col">Produce</th>
                    <th scope="col" className="num">Asking price</th>
                    <th scope="col">Compared with fair price</th>
                    <th scope="col">Status</th>
                    <th scope="col"><span className="visually-hidden">Actions</span></th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((f) => (
                    <tr key={f.id}>
                      <td className="nowrap">{formatDateTime(f.flaggedAt)}</td>
                      <td>{place(f)}</td>
                      <td className="num tabular">{formatLkr(f.listingPrice)}</td>
                      <td>
                        <span className="change">
                          <Icon name={f.deviationPercent >= 0 ? 'up' : 'down'} />
                          {describeDeviation(f.deviationPercent)}
                        </span>
                      </td>
                      <td><StatusBadge status={f.status} /></td>
                      <td className="num">
                        <button type="button" className="btn btn-small" onClick={() => setInvestigating(f)} aria-label={`See details for ${place(f)}`}>
                          See details
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <nav className="pager" aria-label="Pages">
              <span className="muted tabular">
                {(data.page - 1) * PAGE_SIZE + 1}–{(data.page - 1) * PAGE_SIZE + data.items.length} of {data.total}
              </span>
              <button type="button" className="btn btn-quiet" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </button>
              <button type="button" className="btn btn-quiet" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                Next
              </button>
            </nav>
          </div>
        )}
      </Card>

      {investigating && (
        <InvestigateDrawer flag={investigating} place={place(investigating)} onClose={() => setInvestigating(null)} onUpdated={onUpdated} />
      )}
    </>
  )
}

function InvestigateDrawer({ flag, place, onClose, onUpdated }: {
  flag: AnomalyFlag
  place: string
  onClose: () => void
  onUpdated: (flag: AnomalyFlag, status: 'Reviewed' | 'Dismissed') => void
}) {
  const result = useAsync(() => investigateListing(flag.listingId), flag.listingId)
  const [saving, setSaving] = useState<'Reviewed' | 'Dismissed' | null>(null)
  const [saveError, setSaveError] = useState<Error | null>(null)

  const decide = async (status: 'Reviewed' | 'Dismissed') => {
    setSaving(status)
    setSaveError(null)
    try {
      await updateAnomalyStatus(flag.id, status)
      onUpdated(flag, status)
    } catch (e) {
      setSaveError(e as Error)
      setSaving(null)
    }
  }

  const r = result.data
  const topCause = r?.likelyCauses[0]
  const handled = flag.status === 'Open' ? null : flag.status === 'Reviewed' ? 'checked' : 'ignored'
  return (
    <Drawer
      title={`Price check: ${place}`}
      open
      onClose={onClose}
      footer={
        flag.status === 'Open' ? (
          <>
            <button type="button" className="btn btn-quiet" disabled={saving !== null} onClick={() => decide('Dismissed')} title="The price is fine. Remove it from the list.">
              {saving === 'Dismissed' ? 'Saving…' : 'Ignore'}
            </button>
            <button type="button" className="btn btn-primary" disabled={saving !== null} onClick={() => decide('Reviewed')} title="You looked into this price.">
              {saving === 'Reviewed' ? 'Saving…' : 'Mark as checked'}
            </button>
          </>
        ) : (
          <span className="muted">This price was already {handled}.</span>
        )
      }
    >
      {saveError && <ErrorNotice error={saveError} />}
      {result.error && <ErrorNotice error={result.error} onRetry={result.reload} />}
      {!r && result.loading && <p className="muted">Loading details…</p>}
      {r && (
        <>
          <section className="drawer-section verdict">
            <div className="drawer-kpi">
              <span className="change">
                <Icon name={r.flag.deviationPercent >= 0 ? 'up' : 'down'} />
                {describeDeviation(r.flag.deviationPercent)}
              </span>
              <StatusBadge status={r.flag.status} />
            </div>
            <p className="verdict-text">
              The farmer is asking <strong>{formatLkr(r.priceContext.listingPrice)}</strong>.
              {r.priceContext.aiFairPrice !== null && (
                <> The AI fair price is about <strong>{formatLkr(r.priceContext.aiFairPrice)}</strong>.</>
              )}{' '}
              That is <strong>{describeDeviation(r.flag.deviationPercent)}</strong>.
            </p>
            <p className="muted">Flagged {formatDateTime(r.flag.flaggedAt)} · Listing {flag.listingId.slice(0, 8)}</p>
          </section>

          {topCause && (
            <section className="drawer-section advice" aria-label="What to do">
              <strong>What to do</strong>
              <p>{adviceFor(topCause.cause)}</p>
            </section>
          )}

          <section className="drawer-section">
            <h3>Compare the prices</h3>
            <PriceBars
              rows={[
                { label: 'Asking price', value: r.priceContext.listingPrice, asking: true },
                ...(r.priceContext.aiFairPrice !== null ? [{ label: 'AI fair price', value: r.priceContext.aiFairPrice }] : []),
                ...(r.priceContext.regionalAvgPrice !== null ? [{ label: 'Usual weekly price in this region', value: r.priceContext.regionalAvgPrice }] : []),
              ]}
            />
            {r.priceContext.regionalAvgPrice === null
              ? <p className="muted">There is no price history for this crop in this region yet.</p>
              : <p className="muted">{percentileSentence(r.priceContext.percentileInRegion)}</p>}
          </section>

          <section className="drawer-section">
            <h3>Why this might have happened</h3>
            <ol className="causes">
              {r.likelyCauses.map((c) => (
                <li key={c.cause}>
                  <div className="cause-head">
                    <strong>{causeLabel(c.cause)}</strong>
                    <Pill>{confidenceLabel(c.confidence)}</Pill>
                  </div>
                  <p>{c.explanation}</p>
                </li>
              ))}
            </ol>
          </section>

          <section className="drawer-section">
            <h3>Other checks</h3>
            <External label="Quality inspection" context={r.inspectionContext} />
            <External label="Orders" context={r.orderContext} />
          </section>
        </>
      )}
    </Drawer>
  )
}

/** Horizontal bars on one shared scale, so "too high" can be seen as well as read. */
function PriceBars({ rows }: { rows: { label: string; value: number; asking?: boolean }[] }) {
  const max = Math.max(...rows.map((row) => row.value), 1)
  return (
    <ul className="compare">
      {rows.map((row) => (
        <li key={row.label} className="compare-row">
          <div className="compare-label">
            <span>{row.label}</span>
            <strong className="tabular">{formatLkr(row.value)}</strong>
          </div>
          <div className="compare-track" aria-hidden="true">
            <div className={`compare-bar${row.asking ? ' is-asking' : ''}`} style={{ width: `${Math.max(2, (row.value / max) * 100)}%` }} />
          </div>
        </li>
      ))}
    </ul>
  )
}

function External({ label, context }: { label: string; context: ExternalContext }) {
  return (
    <div className="external">
      <strong>{label}</strong>
      <span className="muted">{context.available ? context.summary ?? 'Nothing to show.' : 'This information could not be loaded.'}</span>
    </div>
  )
}
