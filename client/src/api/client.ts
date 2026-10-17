export class ApiError extends Error {
  readonly status: number
  /** The server's ProblemDetails body, when it sent one — e.g. a conflict's `ruleCount`. */
  readonly problem: Record<string, unknown> | null

  constructor(status: number, message: string, problem: Record<string, unknown> | null = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function readProblem(response: Response): Promise<Record<string, unknown> | null> {
  try {
    const body: unknown = await response.json()
    return typeof body === 'object' && body !== null ? (body as Record<string, unknown>) : null
  } catch {
    return null
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    credentials: 'include',
    headers: {
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    throw new ApiError(
      response.status,
      `${init?.method ?? 'GET'} ${path} failed with ${response.status}`,
      await readProblem(response),
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
