import { useLayoutEffect, useRef, useState, type KeyboardEvent, type PointerEvent, type ReactNode } from 'react'

import { niceTicks } from '../utils/trend.ts'

export interface ChartSeries {
  id: string
  label: string
  /** A CSS colour, normally a --series-N token. */
  color: string
  /** One value per period; null is a gap (no data), never zero. */
  values: (number | null)[]
}

export interface ChartBand {
  label: string
  color: string
  lower: (number | null)[]
  upper: (number | null)[]
}

interface Props {
  periods: string[]
  series: ChartSeries[]
  band?: ChartBand
  ariaLabel: string
  formatValue: (value: number) => string
  formatAxisValue: (value: number) => string
  formatPeriod: (period: string) => string
  formatTooltipPeriod: (period: string) => string
  /** Extra tooltip lines for a period, e.g. listing counts. */
  tooltipExtra?: (index: number) => ReactNode
  height?: number
}

const MARGIN = { top: 12, right: 16, bottom: 28, left: 52 }
const END_LABEL_SPACE = 104

/** Contiguous runs of non-null values, as [index, value] pairs. */
function segments(values: (number | null)[]): [number, number][][] {
  const runs: [number, number][][] = []
  let run: [number, number][] = []
  values.forEach((v, i) => {
    if (v === null) {
      if (run.length) runs.push(run)
      run = []
    } else run.push([i, v])
  })
  if (run.length) runs.push(run)
  return runs
}

export function LineChart({
  periods,
  series,
  band,
  ariaLabel,
  formatValue,
  formatAxisValue,
  formatPeriod,
  formatTooltipPeriod,
  tooltipExtra,
  height = 300,
}: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(640)
  const [active, setActive] = useState<number | null>(null)

  useLayoutEffect(() => {
    const el = containerRef.current
    if (!el) return
    const observer = new ResizeObserver(([entry]) => setWidth(Math.max(280, entry.contentRect.width)))
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  const directLabels = series.length > 1 && series.length <= 4
  const right = MARGIN.right + (directLabels ? END_LABEL_SPACE : 0)
  const plotW = width - MARGIN.left - right
  const plotH = height - MARGIN.top - MARGIN.bottom
  const n = periods.length

  const all = [...series.flatMap((s) => s.values), ...(band ? [...band.lower, ...band.upper] : [])].filter(
    (v): v is number => v !== null,
  )
  const ticks = niceTicks(Math.min(...all), Math.max(...all))
  const yMin = ticks[0] ?? 0
  const yMax = ticks[ticks.length - 1] ?? 1

  const x = (i: number) => MARGIN.left + (n <= 1 ? plotW / 2 : (i / (n - 1)) * plotW)
  const y = (v: number) => MARGIN.top + plotH - ((v - yMin) / (yMax - yMin || 1)) * plotH

  const linePath = (values: (number | null)[]) =>
    segments(values)
      .map((run) => run.map(([i, v], k) => `${k ? 'L' : 'M'}${x(i).toFixed(1)},${y(v).toFixed(1)}`).join(''))
      .join('')

  const bandPath = band
    ? segments(band.lower.map((lo, i) => (lo === null || band.upper[i] === null ? null : lo)))
        .map((run) => {
          const top = run.map(([i]) => `${x(i).toFixed(1)},${y(band.upper[i]!).toFixed(1)}`)
          const bottom = [...run].reverse().map(([i, lo]) => `${x(i).toFixed(1)},${y(lo).toFixed(1)}`)
          return `M${top.join('L')}L${bottom.join('L')}Z`
        })
        .join('')
    : ''

  // Roughly one x label per 72px, always including the latest period.
  const labelEvery = Math.max(1, Math.ceil(n / Math.max(2, Math.floor(plotW / 72))))
  const xLabels = periods.map((_, i) => i).filter((i) => (n - 1 - i) % labelEvery === 0)

  const lastIndex = (values: (number | null)[]) => values.findLastIndex((v) => v !== null)

  const pickIndex = (e: PointerEvent<SVGSVGElement>) => {
    const rect = e.currentTarget.getBoundingClientRect()
    const px = ((e.clientX - rect.left) / rect.width) * width
    const i = Math.round(((px - MARGIN.left) / (plotW || 1)) * (n - 1))
    setActive(Math.min(n - 1, Math.max(0, i)))
  }

  const onKeyDown = (e: KeyboardEvent<SVGSVGElement>) => {
    const moves: Record<string, (i: number) => number> = {
      ArrowLeft: (i) => Math.max(0, i - 1),
      ArrowRight: (i) => Math.min(n - 1, i + 1),
      Home: () => 0,
      End: () => n - 1,
    }
    const move = moves[e.key]
    if (!move) return
    e.preventDefault()
    setActive((i) => move(i ?? n - 1))
  }

  const tooltipLeft = active === null ? 0 : Math.min(Math.max(x(active), 90), width - 90)

  return (
    <div className="chart" ref={containerRef}>
      {(series.length > 1 || band) && (
        <ul className="legend">
          {series.map((s) => (
            <li key={s.id}>
              <span className="key-line" style={{ background: s.color }} />
              {s.label}
            </li>
          ))}
          {band && (
            <li>
              <span className="key-area" style={{ background: band.color }} />
              {band.label}
            </li>
          )}
        </ul>
      )}
      <svg
        width={width}
        height={height}
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label={`${ariaLabel}. Use the arrow keys to read values; a table view is available below.`}
        tabIndex={0}
        onPointerMove={pickIndex}
        onPointerLeave={() => setActive(null)}
        onFocus={() => setActive((i) => i ?? n - 1)}
        onBlur={() => setActive(null)}
        onKeyDown={onKeyDown}
      >
        {ticks.map((t) => (
          <g key={t}>
            <line className="grid-line" x1={MARGIN.left} x2={MARGIN.left + plotW} y1={y(t)} y2={y(t)} />
            <text className="axis-label tabular" x={MARGIN.left - 8} y={y(t)} dy="0.32em" textAnchor="end">
              {formatAxisValue(t)}
            </text>
          </g>
        ))}
        <line className="baseline" x1={MARGIN.left} x2={MARGIN.left + plotW} y1={MARGIN.top + plotH} y2={MARGIN.top + plotH} />
        {xLabels.map((i) => (
          <text key={i} className="axis-label" x={x(i)} y={height - 8} textAnchor={i === n - 1 ? 'end' : 'middle'}>
            {formatPeriod(periods[i])}
          </text>
        ))}

        {band && <path d={bandPath} fill={band.color} stroke="none" />}
        {series.map((s) => (
          <path key={s.id} d={linePath(s.values)} fill="none" stroke={s.color} strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" />
        ))}

        {/* Latest point of each series, ringed with the surface so it reads over the band. */}
        {series.map((s) => {
          const i = lastIndex(s.values)
          return i < 0 ? null : (
            <circle key={s.id} cx={x(i)} cy={y(s.values[i]!)} r={4} fill={s.color} stroke="var(--surface)" strokeWidth={2} />
          )
        })}

        {directLabels &&
          series.map((s) => {
            const i = lastIndex(s.values)
            return i < 0 ? null : (
              <g key={s.id} transform={`translate(${x(i) + 10}, ${y(s.values[i]!)})`}>
                <line x1={0} x2={10} y1={0} y2={0} stroke={s.color} strokeWidth={2} />
                <text className="end-label" x={14} dy="0.32em">
                  {s.label}
                </text>
              </g>
            )
          })}

        {active !== null && (
          <g pointerEvents="none">
            <line className="crosshair" x1={x(active)} x2={x(active)} y1={MARGIN.top} y2={MARGIN.top + plotH} />
            {series.map((s) =>
              s.values[active] === null ? null : (
                <circle key={s.id} cx={x(active)} cy={y(s.values[active]!)} r={5} fill={s.color} stroke="var(--surface)" strokeWidth={2} />
              ),
            )}
          </g>
        )}
      </svg>

      {active !== null && (
        <div className="tooltip" style={{ left: tooltipLeft }} role="status">
          <div className="tooltip-title">{formatTooltipPeriod(periods[active])}</div>
          {series.map((s) => (
            <div key={s.id} className="tooltip-row">
              <span className="key-line" style={{ background: s.color }} />
              <strong className="tabular">{s.values[active] === null ? 'No data' : formatValue(s.values[active]!)}</strong>
              {series.length > 1 && <span className="muted">{s.label}</span>}
            </div>
          ))}
          {tooltipExtra?.(active)}
        </div>
      )}
    </div>
  )
}
