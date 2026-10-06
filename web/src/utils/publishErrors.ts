import type { ToastData } from '../components/Toast';

/**
 * Turns the quality-gate refusal from `POST /api/listings/{id}/publish` (FR5) into something an
 * administrator can act on: what is wrong, why, and the next step.
 */
export function publishErrorToast(message: string, listingId: string, go: (to: string) => void): ToastData {
  if (/not been inspected/i.test(message)) {
    return {
      kind: 'warning',
      title: "This listing hasn't been inspected yet",
      message:
        'Buyers only see a listing after an officer has inspected the produce and confirmed its grade. Record the inspection, then approve it again.',
      action: { label: 'Record inspection', onClick: () => go(`/quality/inspections/${listingId}/record`) },
    };
  }
  if (/discrepancy/i.test(message)) {
    return {
      kind: 'warning',
      title: 'Grade mismatch to resolve',
      message:
        "The inspector's confirmed grade differs from the grade the farmer claimed. Resolve the mismatch, then approve the listing again.",
      action: { label: 'Resolve discrepancy', onClick: () => go('/quality/discrepancies') },
    };
  }
  if (/rejected/i.test(message)) {
    return {
      kind: 'error',
      title: 'Produce failed the quality check',
      message: "The inspection confirmed this produce as 'Rejected', so it cannot be published to buyers.",
    };
  }
  return { kind: 'error', title: "Couldn't publish the listing", message };
}
