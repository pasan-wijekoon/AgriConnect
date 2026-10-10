import { useState, type ReactNode } from 'react'

import { LineChart, type ChartSeries } from '../components/LineChart.tsx'
import { Card, EmptyState, ErrorNotice, Field, Icon, PageHeader, StatTile, SuccessNotice } from '../components/ui.tsx'
import { useRole } from '../context/session.ts'
import { getFilters, getPriceTrends, refreshSnapshots, type AnalyticsFilters, type PriceTrendResponse } from '../utils/api.ts'
import { daysAgo, formatDay, formatLkr, formatMonth, formatPercent, formatWhole } from '../utils/format.ts'
import { changePercent, fillGrid, periodGrid, type Bucket, type GridSlot, type TrendPoint } from '../utils/trend.ts'
import { useAsync } from '../utils/useAsync.ts'

const PERIODS = [
  { weeks: 8, label: 'Last 8 weeks' },
  { weeks: 12, label: 'Last 12 weeks' },
  { weeks: 16, label: 'Last 16 weeks' },
  { weeks: 26, label: 'Last 26 weeks' },
]

const ALL = 'all'
const COMPARE = 'compare'

interface TrendData {
  headline: PriceTrendResponse
  perRegion: { regionId: string; name: string; trend: PriceTrendResponse }[]
}

export function PriceTrendsPage() {
  const filters = useAsync(getFilters, 'filters')

  if (filters.error && !filters.data) return <PageShell><ErrorNotice error={filters.error} onRetry={filters.reload} /></PageShell>
  if (!filters.data) return <PageShell><p className="muted">Loading…</p></PageShell>
  if (filters.data.crops.length === 0) {
    return <PageShell><EmptyState title="No crops yet">Crops appear here once they are added to AgriConnect.</EmptyState></PageShell>
  }
  return <PriceTrends filters={filters.data} />
}

function PageShell({ children, actions }: { children: ReactNode; actions?: ReactNode }) {
  return (
    <>
      <PageHeader
        title="Price trends"
        description="Track how wholesale prices are moving by crop, region, and time period."
        actions={actions}
      />
      {children}
    </>
  )
}

function PriceTrends({ filters }: { filters: AnalyticsFilters }) {
  const role = useRole()
  // Open on a crop that has prices; the first one alphabetically may have none yet.
  const [cropId, setCropId] = useState((filters.crops.find((c) => c.hasPriceHistory) ?? filters.crops[0]).id)
  const [region, setRegion] = useState(ALL)
  const [weeks, setWeeks] = useState(16)
  const [bucket, setBucket] = useState<Bucket>('week')
  const [showTable, setShowTable] = useState(false)
  const [refreshing, setRefreshing] = useState(false)
  const [refreshMessage, setRefreshMessage] = useState<string | null>(null)
  const [refreshError, setRefreshError] = useState<Error | null>(null)

  const cropName = filters.crops.find((c) => c.id === cropId)?.name ?? 'This crop'
  const regionName = filters.regions.find((r) => r.id === region)?.name
  const from = daysAgo(7 * weeks)
  const to = daysAgo(0)

  const trend = useAsync<TrendData>(async () => {
    const query = { cropId, from, to, bucket }
    const headline = await getPriceTrends({ ...query, regionId: region === ALL || region === COMPARE ? null : region })
    const perRegion =
      region === COMPARE
        ? await Promise.all(
            filters.regions.map(async (r) => ({ regionId: r.id, name: r.name, trend: await getPriceTrends({ ...query, regionId: r.id }) })),
          )
        : []
    return { headline, perRegion }
  }, `${cropId}|${region}|${from}|${to}|${bucket}`)

  const rebuild = async () => {
    setRefreshing(true)
    setRefreshMessage(null)
    setRefreshError(null)
    try {
      const result = await refreshSnapshots()
      setRefreshMessage(`Rebuilt ${formatWhole(result.snapshotsUpserted)} snapshots across ${result.periodsProcessed} weeks.`)
      trend.reload()
    } catch (e) {
      setRefreshError(e as Error)
    } finally {
      setRefreshing(false)
    }
  }

  const data = trend.data
  const points = data?.headline.points ?? []
  const latest = points.at(-1)
  const stepsBack = bucket === 'week' ? 4 : 1
  const change = changePercent(points, bucket, stepsBack)
  const periodLabel = bucket === 'week' ? formatDay : formatMonth

  const grid = periodGrid(
    [...points.map((p) => p.period), ...(data?.perRegion.flatMap((r) => r.trend.points.map((p) => p.period)) ?? [])],
    bucket,
  )
  const headlineSlots = fillGrid(points, grid)
  const colors = ['var(--series-1)', 'var(--series-2)']
  const series: ChartSeries[] =
    region === COMPARE && data
      ? data.perRegion.slice(0, colors.length).map((r, i) => ({
          id: r.regionId,
          label: r.name,
          color: colors[i],
          values: fillGrid(r.trend.points, grid).map((s) => s.point?.avgPrice ?? null),
        }))
      : [{ id: 'avg', label: 'Average price', color: 'var(--series-1)', values: headlineSlots.map((s) => s.point?.avgPrice ?? null) }]

  const scope = region === COMPARE ? 'all regions (compared)' : regionName ?? 'all regions'

  return (
    <PageShell
      actions={
        role === 'Administrator' && (
          <button type="button" className="btn" title="Rebuild historical price snapshots from published listings" onClick={rebuild} disabled={refreshing}>
            <Icon name="refresh" />
            {refreshing ? 'Rebuilding…' : 'Rebuild snapshots'}
          </button>
        )
      }
    >
      <div className="filters analytics-filter-panel" aria-label="Price trend filters">
        <Field label="Crop">
          <select value={cropId} onChange={(e) => setCropId(e.target.value)}>
            {filters.crops.map((c) => (
              <option key={c.id} value={c.id}>{c.name}{c.hasPriceHistory === false ? ' (no data yet)' : ''}</option>
            ))}
          </select>
        </Field>
        <Field label="Region">
          <select value={region} onChange={(e) => setRegion(e.target.value)}>
            <option value={ALL}>All regions combined</option>
            {filters.regions.length > 1 && <option value={COMPARE}>Compare regions</option>}
            {filters.regions.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
          </select>
        </Field>
        <Field label="Period">
          <select value={weeks} onChange={(e) => setWeeks(Number(e.target.value))}>
            {PERIODS.map((p) => <option key={p.weeks} value={p.weeks}>{p.label}</option>)}
          </select>
        </Field>
        <Field label="Group by">
          <select value={bucket} onChange={(e) => setBucket(e.target.value as Bucket)}>
            <option value="week">Week</option>
            <option value="month">Month</option>
          </select>
        </Field>
        <button
          type="button"
          className="btn btn-quiet filter-reset"
          onClick={() => { setCropId((filters.crops.find((c) => c.hasPriceHistory) ?? filters.crops[0]).id); setRegion(ALL); setWeeks(16); setBucket('week'); setShowTable(false) }}
        >
          Reset filters
        </button>
      </div>

      {refreshMessage && <SuccessNotice onDismiss={() => setRefreshMessage(null)}>{refreshMessage}</SuccessNotice>}
      {refreshError && <ErrorNotice error={refreshError} />}
      {trend.error && <ErrorNotice error={trend.error} onRetry={trend.reload} />}

      <div className={`stack${trend.loading && data ? ' refetching' : ''}`} aria-busy={trend.loading}>
        {!data && trend.loading && <p className="muted">Loading prices…</p>}
        {data && trend.loading && <p className="inline-loading" role="status">Updating price trend…</p>}
        {data && points.length === 0 && (
          <EmptyState title={`No prices for ${cropName}`}>
            Nothing was recorded for {scope} in this period. Try a longer period or another region.
          </EmptyState>
        )}
        {data && latest && (
          <>
            <div className="stats">
              <StatTile
                label="Current average"
                value={<>{formatLkr(latest.avgPrice)}<span className="unit"> /kg</span></>}
                detail={`${bucket === 'week' ? `Week of ${formatDay(latest.period)}` : formatMonth(latest.period)} · ${scope}`}
              />
              <StatTile
                label={`Change vs ${bucket === 'week' ? '4 weeks' : '1 month'} ago`}
                value={
                  change === null ? '—' : (
                    <span className={`change ${change >= 0 ? 'change-up' : 'change-down'}`}>
                      <Icon name={change >= 0 ? 'up' : 'down'} />
                      {change >= 0 ? 'Up' : 'Down'} {formatPercent(change)}
                    </span>
                  )
                }
                detail={change === null ? 'No data for the comparison period' : undefined}
              />
              <StatTile
                label="Latest price range"
                value={`${formatWhole(latest.minPrice)}–${formatWhole(latest.maxPrice)}`}
                detail="Lowest to highest, LKR/kg"
              />
              <StatTile label="Listings" value={formatWhole(latest.sampleCount)} detail={`In the latest ${bucket}`} />
            </div>

            <Card
              title={`${cropName} — average price per kg`}
              className="trend-card"
              actions={
                <button type="button" className="btn btn-table-toggle" onClick={() => setShowTable((v) => !v)} aria-pressed={showTable}>
                  <Icon name={showTable ? 'chart' : 'grid'} />
                  {showTable ? 'View chart' : 'View data table'}
                </button>
              }
            >
              <LineChart
                periods={grid}
                series={series}
                band={
                  region === COMPARE
                    ? undefined
                    : {
                        label: 'Lowest–highest',
                        color: 'var(--band)',
                        lower: headlineSlots.map((s) => s.point?.minPrice ?? null),
                        upper: headlineSlots.map((s) => s.point?.maxPrice ?? null),
                      }
                }
                ariaLabel={`Line chart of the ${bucket}ly average ${cropName} price for ${scope}`}
                formatValue={formatLkr}
                formatAxisValue={formatWhole}
                formatPeriod={periodLabel}
                formatTooltipPeriod={(p) => (bucket === 'week' ? `Week of ${formatDay(p)}` : formatMonth(p))}
                tooltipExtra={(i) => {
                  const p = headlineSlots[i]?.point
                  return p && region !== COMPARE ? (
                    <div className="tooltip-extra muted">
                      Range {formatWhole(p.minPrice)}–{formatWhole(p.maxPrice)} · {p.sampleCount} listings
                    </div>
                  ) : null
                }}
              />
              {grid.length > headlineSlots.filter((s) => s.point).length && region !== COMPARE && (
                <p className="footnote muted" role="note">Breaks in the line indicate periods with no listings.</p>
              )}
              {showTable && <TrendTable slots={headlineSlots} periodLabel={periodLabel} />}
            </Card>
          </>
        )}
      </div>
    </PageShell>
  )
}

function TrendTable({ slots, periodLabel }: { slots: GridSlot<TrendPoint>[]; periodLabel: (p: string) => string }) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th scope="col">Period</th>
            <th scope="col" className="num">Average</th>
            <th scope="col" className="num">Lowest</th>
            <th scope="col" className="num">Highest</th>
            <th scope="col" className="num">Listings</th>
          </tr>
        </thead>
        <tbody>
          {[...slots].reverse().map(({ period, point }) => (
            <tr key={period}>
              <th scope="row">{periodLabel(period)}</th>
              {point ? (
                <>
                  <td className="num tabular">{formatLkr(point.avgPrice)}</td>
                  <td className="num tabular">{formatLkr(point.minPrice)}</td>
                  <td className="num tabular">{formatLkr(point.maxPrice)}</td>
                  <td className="num tabular">{point.sampleCount}</td>
                </>
              ) : (
                <td colSpan={4} className="muted">No listings</td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
