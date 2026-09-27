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
import { ANOMALY_STATUSES, causeLabel, describeDeviation, type AnomalyStatus } from '../utils/anomalies.ts'
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
        title="Anomaly review queue"
        description="Listings priced far from the AI-suggested fair price (FR16). Investigate, then mark reviewed or dismiss."
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
    setMessage(`${place(flag)} listing marked ${status.toLowerCase()}.`)
    setVersion((v) => v + 1)
  }

  const data = list.data
  const totalPages = data ? Math.max(1, Math.ceil(data.total / PAGE_SIZE)) : 1

  return (
    <>
      <div className="filters">
        <div className="tabs" role="tablist" aria-label="Status">
          {TABS.map((t) => (
            <button
              key={t}
              type="button"
              role="tab"
              aria-selected={tab === t}
              className={`tab${tab === t ? ' is-active' : ''}`}
              onClick={() => selectTab(t)}
            >
              {t}
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
        {!data && list.loading && <p className="muted">Loading flags…</p>}
        {data && data.items.length === 0 && (
          <EmptyState title={tab === 'Open' ? 'Nothing to review' : 'No flags here'}>
            {tab === 'Open' ? 'Every flagged listing has been reviewed or dismissed.' : 'No flags match these filters.'}
          </EmptyState>
        )}
        {data && data.items.length > 0 && (
          <div className={list.loading ? 'refetching' : undefined}>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th scope="col">Flagged</th>
                    <th scope="col">Crop · region</th>
                    <th scope="col" className="num">Listing price</th>
                    <th scope="col">Deviation</th>
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
                        <button type="button" className="btn btn-small" onClick={() => setInvestigating(f)}>
                          Investigate
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
  return (
    <Drawer
      title={`Investigate: ${place}`}
      open
      onClose={onClose}
      footer={
        flag.status === 'Open' ? (
          <>
            <button type="button" className="btn btn-quiet" disabled={saving !== null} onClick={() => decide('Dismissed')}>
              {saving === 'Dismissed' ? 'Dismissing…' : 'Dismiss'}
            </button>
            <button type="button" className="btn btn-primary" disabled={saving !== null} onClick={() => decide('Reviewed')}>
              {saving === 'Reviewed' ? 'Saving…' : 'Mark reviewed'}
            </button>
          </>
        ) : (
          <span className="muted">This flag was already {flag.status.toLowerCase()}.</span>
        )
      }
    >
      {saveError && <ErrorNotice error={saveError} />}
      {result.error && <ErrorNotice error={result.error} onRetry={result.reload} />}
      {!r && result.loading && <p className="muted">Investigating…</p>}
      {r && (
        <>
          <section className="drawer-section">
            <div className="drawer-kpi">
              <span className="change">
                <Icon name={r.flag.deviationPercent >= 0 ? 'up' : 'down'} />
                {describeDeviation(r.flag.deviationPercent)}
              </span>
              <StatusBadge status={r.flag.status} />
            </div>
            <p className="muted">Flagged {formatDateTime(r.flag.flaggedAt)} · Listing {flag.listingId.slice(0, 8)}</p>
          </section>

          <section className="drawer-section">
            <h3>Price context</h3>
            <dl className="facts">
              <div><dt>Listing price</dt><dd className="tabular">{formatLkr(r.priceContext.listingPrice)}</dd></div>
              <div>
                <dt>Regional weekly average</dt>
                <dd className="tabular">{r.priceContext.regionalAvgPrice === null ? 'No history yet' : formatLkr(r.priceContext.regionalAvgPrice)}</dd>
              </div>
            </dl>
            <div className="percentile" aria-label={`Priced higher than ${r.priceContext.percentileInRegion}% of weekly averages`}>
              <div className="percentile-track">
                <div className="percentile-marker" style={{ left: `${r.priceContext.percentileInRegion}%` }} />
              </div>
              <p className="muted">
                Priced higher than <strong>{r.priceContext.percentileInRegion}%</strong> of this crop's weekly averages in the region.
              </p>
            </div>
          </section>

          <section className="drawer-section">
            <h3>Likely causes</h3>
            <ol className="causes">
              {r.likelyCauses.map((c) => (
                <li key={c.cause}>
                  <div className="cause-head">
                    <strong>{causeLabel(c.cause)}</strong>
                    <Pill>{c.confidence} confidence</Pill>
                  </div>
                  <p>{c.explanation}</p>
                </li>
              ))}
            </ol>
          </section>

          <section className="drawer-section">
            <h3>Other evidence</h3>
            <External label="Inspection history" context={r.inspectionContext} />
            <External label="Order history" context={r.orderContext} />
          </section>
        </>
      )}
    </Drawer>
  )
}

function External({ label, context }: { label: string; context: ExternalContext }) {
  return (
    <div className="external">
      <strong>{label}</strong>
      <span className="muted">{context.available ? 'Available' : `Not available yet — ${context.reason ?? 'no data'}`}</span>
    </div>
  )
}
