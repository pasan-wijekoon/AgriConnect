export type AnomalyStatus = 'Open' | 'Reviewed' | 'Dismissed'

export const ANOMALY_STATUSES: AnomalyStatus[] = ['Open', 'Reviewed', 'Dismissed']

/** "52.7% above AI price" — the sign is spelled out, never carried by colour alone. */
export function describeDeviation(percent: number): string {
  const direction = percent >= 0 ? 'above' : 'below'
  return `${Math.abs(percent).toFixed(1)}% ${direction} AI price`
}

/** Human wording for the investigation's cause codes. */
export function causeLabel(cause: string): string {
  const labels: Record<string, string> = {
    PotentialDataEntryError: 'Possible data-entry error',
    PremiumQualityGrade: 'Premium quality grade',
    DistressedSale: 'Distressed sale',
    MarketVolatility: 'Market volatility',
  }
  return labels[cause] ?? cause.replace(/([a-z])([A-Z])/g, '$1 $2')
}
