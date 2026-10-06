export type SupplyType = 'Shortage' | 'Oversupply'
export type Severity = 'Low' | 'Medium' | 'High'

export interface ShortageEvent {
  id: string
  cropId: string
  regionId: string
  type: SupplyType
  severity: Severity
  detectedAt: string
  notes: string | null
}

export interface NamedItem {
  id: string
  name: string
}

export interface HeatCell {
  crop: NamedItem
  region: NamedItem
  /** The event the cell is coloured by: highest severity, most recent on a tie. */
  worst: ShortageEvent | null
  events: ShortageEvent[]
}

const rank: Record<Severity, number> = { Low: 1, Medium: 2, High: 3 }

/** One row per crop, one cell per region. */
export function buildHeatmap(events: ShortageEvent[], crops: NamedItem[], regions: NamedItem[]): HeatCell[][] {
  return crops.map((crop) =>
    regions.map((region) => {
      const here = events
        .filter((e) => e.cropId === crop.id && e.regionId === region.id)
        .sort((a, b) => rank[b.severity] - rank[a.severity] || b.detectedAt.localeCompare(a.detectedAt))
      return { crop, region, worst: here[0] ?? null, events: here }
    }),
  )
}

/** CSS custom property for a cell: shortage is the warm arm, oversupply the cool one. */
export function cellColor(event: ShortageEvent | null): string {
  if (!event) return 'var(--neutral-cell)'
  const arm = event.type === 'Shortage' ? 'short' : 'over'
  return `var(--${arm}-${event.severity.toLowerCase()})`
}

/** Text colour readable on the cell. Which steps are dark flips between themes, so it's a token per step. */
export function cellInk(event: ShortageEvent | null): string {
  if (!event) return 'var(--text-secondary)'
  return `var(--cell-ink-${event.severity.toLowerCase()})`
}
