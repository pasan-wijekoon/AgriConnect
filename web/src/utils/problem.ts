/** An RFC 7807 ProblemDetails response from the API, as an Error. */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  /** Validation errors by JSON field name, e.g. { dateRangeEnd: ["..."] }. */
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, title: string, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.fieldErrors = fieldErrors
  }
}

const fallback: Record<number, string> = {
  401: 'Your session has expired. Please sign in again.',
  403: "Your role doesn't have access to this.",
  404: 'Not found.',
}

export function toApiError(status: number, body: unknown): ApiError {
  const problem = (body && typeof body === 'object' ? body : {}) as {
    title?: unknown
    detail?: unknown
    errors?: unknown
  }
  const title = typeof problem.title === 'string' ? problem.title : `Request failed (${status})`
  const fieldErrors =
    problem.errors && typeof problem.errors === 'object' ? (problem.errors as Record<string, string[]>) : {}
  const firstFieldError = Object.values(fieldErrors).flat()[0]
  const message =
    (typeof problem.detail === 'string' && problem.detail) ||
    firstFieldError ||
    fallback[status] ||
    title
  return new ApiError(status, title, message, fieldErrors)
}

export const networkError = () =>
  new ApiError(0, 'Network error', "Can't reach the AgriConnect API. Check that it is running and try again.")
