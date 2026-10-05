/**
 * The marketplace api client throws `API error <status>: <body>` where the body is usually an
 * RFC 7807 ProblemDetails JSON. Return the human-readable part of it.
 */
export function readableError(err: unknown, fallback: string): string {
  const raw = err instanceof Error ? err.message : '';
  const json = raw.includes('{') ? raw.slice(raw.indexOf('{')) : '';
  try {
    const body = JSON.parse(json);
    if (typeof body.detail === 'string' && body.detail) return body.detail;
    if (typeof body.error === 'string' && body.error) return body.error;
    if (body.errors && typeof body.errors === 'object') {
      const first = Object.values(body.errors as Record<string, string[]>)[0];
      if (first?.[0]) return first[0];
    }
    if (typeof body.title === 'string' && body.title) return body.title;
  } catch {
    // not JSON; fall through
  }
  return raw && !raw.startsWith('API error') ? raw : fallback;
}
