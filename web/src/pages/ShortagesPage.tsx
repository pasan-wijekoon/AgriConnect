import { useState } from 'react'

import { Card, EmptyState, ErrorNotice, Field, PageHeader, StatTile } from '../components/ui.tsx'
import { getFilters, getShortages, type AnalyticsFilters } from '../utils/api.ts'
import { formatDateTime } from '../utils/format.ts'
import { buildHeatmap, cellColor, cellInk, type HeatCell, type Severity, type ShortageEvent, type SupplyType } from '../utils/shortages.ts'
import { useAsync } from '../utils/useAsync.ts'

const SEVERITIES: Severity[] = ['Low', 'Medium', 'High']

export function ShortagesPage() {
  const [type, setType] = useState<SupplyType | ''>('')
  const [severity, setSeverity] = useState<Severity | ''>('')
  const [selected, setSelected] = useState<{ cropId: string; regionId: string } | null>(null)

  const data = useAsync(
    async () => {
      const [filters, events] = await Promise.all([
        getFilters(),
        getShortages({ type: type || null, severity: severity || null }),
      ])
      return { filters, events: events.items }
    },
    `${type}|${severity}`,
  )

  return (
    <>
      <PageHeader
        title="Shortages & oversupply"
        description="Recurring supply imbalances per crop and region (FR17)."
      />
      <div className="filters">
        <Field label="Type">
          <select value={type} onChange={(e) => setType(e.target.value as SupplyType | '')}>
            <option value="">Shortage and oversupply</option>
            <option value="Shortage">Shortage only</option>
            <option value="Oversupply">Oversupply only</option>
          </select>
        </Field>
        <Field label="Severity">
          <select value={severity} onChange={(e) => setSeverity(e.target.value as Severity | '')}>
            <option value="">Any severity</option>
            {SEVERITIES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </Field>
      </div>

      {data.error && <ErrorNotice error={data.error} onRetry={data.reload} />}
      {!data.data && data.loading && <p className="muted">Loading…</p>}
      {data.data && (
        <div className={`stack${data.loading ? ' refetching' : ''}`}>
          <Shortages
            filters={data.data.filters}
            events={data.data.events}
            selected={selected}
            onSelect={setSelected}
          />
        </div>
      )}
    </>
  )
}

function Shortages({ filters, events, selected, onSelect }: {
  filters: AnalyticsFilters
  events: ShortageEvent[]
  selected: { cropId: string; regionId: string } | null
  onSelect: (cell: { cropId: string; regionId: string } | null) => void
}) {
  const grid = buildHeatmap(events, filters.crops, filters.regions)
  const cropName = new Map(filters.crops.map((c) => [c.id, c.name]))
  const regionName = new Map(filters.regions.map((r) => [r.id, r.name]))
  const count = (t: SupplyType) => events.filter((e) => e.type === t)
  const high = (list: ShortageEvent[]) => list.filter((e) => e.severity === 'High').length

  const listed = selected
    ? events.filter((e) => e.cropId === selected.cropId && e.regionId === selected.regionId)
    : events

  return (
    <>
      <div className="stats">
        <StatTile label="Shortages" value={count('Shortage').length} detail={`${high(count('Shortage'))} high severity`} />
        <StatTile label="Oversupply" value={count('Oversupply').length} detail={`${high(count('Oversupply'))} high severity`} />
        <StatTile
          label="Affected crop × region pairs"
          value={grid.flat().filter((c) => c.worst).length}
          detail={`of ${filters.crops.length * filters.regions.length}`}
        />
      </div>

      <Card title="Supply heatmap">
        <p className="muted card-intro">Each cell shows the most severe event for that crop and region. Select a cell to list its events.</p>
        <div className="table-wrap">
          <table className="heatmap">
            <thead>
              <tr>
                <td />
                {filters.regions.map((r) => <th key={r.id} scope="col">{r.name}</th>)}
              </tr>
            </thead>
            <tbody>
              {grid.map((row) => (
                <tr key={row[0]?.crop.id}>
                  <th scope="row">{row[0]?.crop.name}</th>
                  {row.map((cell) => (
                    <td key={cell.region.id}>
                      <HeatmapCell
                        cell={cell}
                        selected={selected?.cropId === cell.crop.id && selected.regionId === cell.region.id}
                        onSelect={() =>
                          onSelect(
                            selected?.cropId === cell.crop.id && selected.regionId === cell.region.id
                              ? null
                              : { cropId: cell.crop.id, regionId: cell.region.id },
                          )
                        }
                      />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <HeatmapLegend />
      </Card>

      <Card
        title={selected ? `Events — ${cropName.get(selected.cropId)}, ${regionName.get(selected.regionId)}` : 'All events'}
        actions={selected && <button type="button" className="btn btn-quiet" onClick={() => onSelect(null)}>Show all</button>}
      >
        {listed.length === 0 ? (
          <EmptyState title="No events">No shortage or oversupply matches these filters.</EmptyState>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th scope="col">Detected</th>
                  <th scope="col">Crop</th>
                  <th scope="col">Region</th>
                  <th scope="col">Type</th>
                  <th scope="col">Severity</th>
                  <th scope="col">Notes</th>
                </tr>
              </thead>
              <tbody>
                {listed.map((e) => (
                  <tr key={e.id}>
                    <td className="nowrap">{formatDateTime(e.detectedAt)}</td>
                    <td>{cropName.get(e.cropId) ?? 'Unknown crop'}</td>
                    <td>{regionName.get(e.regionId) ?? 'Unknown region'}</td>
                    <td>
                      <span className="swatch-label">
                        <span className="swatch" style={{ background: cellColor(e) }} />
                        {e.type}
                      </span>
                    </td>
                    <td>{e.severity}</td>
                    <td className="notes">{e.notes ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </>
  )
}

function HeatmapCell({ cell, selected, onSelect }: { cell: HeatCell; selected: boolean; onSelect: () => void }) {
  const { worst, events } = cell
  const description = worst
    ? `${cell.crop.name} in ${cell.region.name}: ${worst.severity} ${worst.type.toLowerCase()}${events.length > 1 ? `, ${events.length} events` : ''}`
    : `${cell.crop.name} in ${cell.region.name}: no events`
  return (
    <button
      type="button"
      className={`heat-cell${selected ? ' is-selected' : ''}`}
      style={{ background: cellColor(worst), color: cellInk(worst) }}
      onClick={onSelect}
      aria-pressed={selected}
      aria-label={description}
      title={worst?.notes ?? description}
    >
      {worst ? (
        <>
          <strong>{worst.type}</strong>
          <span>{worst.severity}{events.length > 1 ? ` · +${events.length - 1} more` : ''}</span>
        </>
      ) : (
        <span>No event</span>
      )}
    </button>
  )
}

/** Diverging scale: oversupply on the cool arm, shortage on the warm arm, grey for none. */
function HeatmapLegend() {
  const step = (type: SupplyType, severity: Severity) =>
    ({ id: '', cropId: '', regionId: '', type, severity, detectedAt: '', notes: null }) satisfies ShortageEvent
  const arm = (type: SupplyType, order: Severity[]) =>
    order.map((s) => (
      <li key={`${type}-${s}`}>
        <span className="legend-swatch" style={{ background: cellColor(step(type, s)) }} />
        {s}
      </li>
    ))
  return (
    <div className="heat-legend" aria-label="Heatmap colour scale">
      <span className="muted">Oversupply</span>
      <ul>{arm('Oversupply', ['High', 'Medium', 'Low'])}</ul>
      <ul>
        <li>
          <span className="legend-swatch" style={{ background: cellColor(null) }} />
          None
        </li>
      </ul>
      <ul>{arm('Shortage', SEVERITIES)}</ul>
      <span className="muted">Shortage</span>
    </div>
  )
}
