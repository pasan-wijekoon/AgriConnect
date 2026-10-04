export type AnomalyStatus = 'Open' | 'Reviewed' | 'Dismissed'

export const ANOMALY_STATUSES: AnomalyStatus[] = ['Open', 'Reviewed', 'Dismissed']

// The API keeps its own status names; officers see these plainer words.
const STATUS_LABELS: Record<AnomalyStatus, string> = { Open: 'To check', Reviewed: 'Checked', Dismissed: 'Ignored' }

export function statusLabel(status: AnomalyStatus): string {
  return STATUS_LABELS[status]
}

/** "52.7% too high" — the direction is spelled out, never carried by colour alone. */
export function describeDeviation(percent: number): string {
  return `${Math.abs(percent).toFixed(1)}% too ${percent >= 0 ? 'high' : 'low'}`
}

/** Plain wording for the investigation's cause codes. */
export function causeLabel(cause: string): string {
  const labels: Record<string, string> = {
    PotentialDataEntryError: 'Typing mistake',
    PremiumQualityGrade: 'Asking extra for quality',
    DistressedSale: 'Needs to sell fast',
    MarketVolatility: 'Normal price movement',
  }
  return labels[cause] ?? cause.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function confidenceLabel(confidence: 'High' | 'Medium' | 'Low'): string {
  return { High: 'Very likely', Medium: 'Possible', Low: 'Less likely' }[confidence]
}

/** What the officer should do next, for the most likely cause. */
export function adviceFor(cause: string): string {
  const advice: Record<string, string> = {
    PotentialDataEntryError: 'Call the farmer and confirm the price. A digit may have been typed wrongly.',
    PremiumQualityGrade: 'Check the quality grade. The farmer may be asking extra for top quality.',
    DistressedSale: 'Check whether the farmer needs to sell fast. A very low price may also be a typing mistake.',
    MarketVolatility: 'Nothing unusual found. You can mark this price as checked.',
  }
  return advice[cause] ?? 'Look at the details below, then mark this price as checked or ignore it.'
}

/** "Higher than every weekly price recorded…" instead of a statistics term. */
export function percentileSentence(percentile: number): string {
  if (percentile >= 100) return 'This price is higher than every weekly price recorded for this crop in this region.'
  if (percentile <= 0) return 'This price is lower than every weekly price recorded for this crop in this region.'
  return `This price is higher than about ${percentile} out of every 100 weekly prices recorded for this crop in this region.`
}
