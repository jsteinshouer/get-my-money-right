import { ApiError, apiClient } from './client'

/** The five roles a CSV column can be given, plus the absence of one. */
export type ColumnRole = 'date' | 'description' | 'amount' | 'debit' | 'credit' | null

export interface SavedMapping {
  dateColumn: string
  descriptionColumn: string
  amountColumn: string | null
  debitColumn: string | null
  creditColumn: string | null
  dateFormat: string
  delimiter: string
  hasHeaderRow: boolean
  updatedAt: string
}

export interface UploadPreview {
  token: string
  accountId: number
  fileName: string
  columns: string[]
  sampleRows: string[][]
  delimiter: string
  hasHeaderRow: boolean
  dateFormat: string
  saved: SavedMapping | null
  /** Columns the saved mapping names that this file no longer has. */
  missingColumns: string[]
}

export interface ReadingRow {
  date: string | null
  description: string | null
  amount: number | null
  error: string | null
}

export interface Reading {
  columns: string[]
  sampleRows: string[][]
  rows: ReadingRow[]
}

/** How a rule's words are held up against a row's description. */
export type IgnoreMatchType = 'Contains' | 'StartsWith' | 'Equals'

export interface IgnoreRule {
  id: number
  /** Null for a rule that applies to every account. */
  accountId: number | null
  accountName: string | null
  matchText: string
  matchType: IgnoreMatchType
  isActive: boolean
}

export interface IgnoreRuleInput {
  accountId: number | null
  matchText: string
  matchType: IgnoreMatchType
}

/** A non-null `skippedReason` is the strike, and it silences `error`: the row is leaving anyway. */
export interface PreviewRow {
  date: string | null
  description: string | null
  amount: number | null
  error: string | null
  skippedReason: string | null
}

export interface FullPreview {
  rows: PreviewRow[]
  willImportCount: number
  skippedCount: number
  errorCount: number
}

/** The three kinds of match, said the way the slip reads them out: "contains AUTOPAY". */
export const matchTypes: { value: IgnoreMatchType; label: string }[] = [
  { value: 'Contains', label: 'contains' },
  { value: 'StartsWith', label: 'starts with' },
  { value: 'Equals', label: 'is' },
]

export function matchTypeLabel(value: IgnoreMatchType): string {
  return matchTypes.find((t) => t.value === value)?.label ?? 'contains'
}

export interface MappingDraft {
  delimiter: string
  hasHeaderRow: boolean
  dateFormat: string
  dateColumn: string | null
  descriptionColumn: string | null
  amountColumn: string | null
  debitColumn: string | null
  creditColumn: string | null
}

/** The formats the server will read, in the words a household member uses for them. */
export const dateFormats: { value: string; label: string }[] = [
  { value: 'yyyy-MM-dd', label: 'year-month-day (2026-08-14)' },
  { value: 'MM/dd/yyyy', label: 'month/day/year (08/14/2026)' },
  { value: 'M/d/yyyy', label: 'month/day/year, no leading zeros (8/14/2026)' },
  { value: 'dd/MM/yyyy', label: 'day/month/year (14/08/2026)' },
  { value: 'd/M/yyyy', label: 'day/month/year, no leading zeros (14/8/2026)' },
  { value: 'yyyy/MM/dd', label: 'year/month/day (2026/08/14)' },
  { value: 'MM-dd-yyyy', label: 'month-day-year (08-14-2026)' },
  { value: 'dd-MM-yyyy', label: 'day-month-year (14-08-2026)' },
  { value: 'dd.MM.yyyy', label: 'day.month.year (14.08.2026)' },
  { value: 'd.M.yyyy', label: 'day.month.year, no leading zeros (14.8.2026)' },
  { value: 'MM/dd/yy', label: 'month/day/two-digit year (08/14/26)' },
  { value: 'M/d/yy', label: 'month/day/two-digit year, no leading zeros (8/14/26)' },
]

export const delimiters: { value: string; label: string }[] = [
  { value: ',', label: 'Comma' },
  { value: ';', label: 'Semicolon' },
  { value: '\t', label: 'Tab' },
  { value: '|', label: 'Pipe' },
]

export function delimiterLabel(value: string): string {
  return delimiters.find((d) => d.value === value)?.label.toLowerCase() ?? 'comma'
}

export function dateFormatLabel(value: string): string {
  return dateFormats.find((f) => f.value === value)?.label ?? value
}

async function uploadPreview(accountId: number, file: File): Promise<UploadPreview> {
  const body = new FormData()
  body.append('accountId', String(accountId))
  body.append('file', file)

  // Multipart, so this one bypasses apiClient's JSON body handling.
  const response = await fetch('/api/import/preview', { method: 'POST', credentials: 'include', body })

  if (!response.ok) {
    throw new ApiError(response.status, await problemDetail(response))
  }

  return (await response.json()) as UploadPreview
}

/** The server names the problem with the file; keep its wording rather than inventing our own. */
async function problemDetail(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { errors?: Record<string, string[]>; detail?: string }
    const firstField = problem.errors && Object.values(problem.errors)[0]
    if (firstField?.length) {
      return firstField[0]
    }
    if (problem.detail) {
      return problem.detail
    }
  } catch {
    // A response that isn't ProblemDetails falls through to the generic wording below.
  }
  return response.status === 404
    ? 'That account no longer exists.'
    : 'That file could not be read as a CSV.'
}

export const importApi = {
  uploadPreview,
  fetchMapping: (accountId: number) => apiClient.get<SavedMapping>(`/import/mappings/${accountId}`),
  readPreview: (token: string, draft: MappingDraft) =>
    apiClient.post<Reading>(`/import/previews/${token}/reading`, draft),
  /** Station 3: the whole file, with the rows an active rule catches marked and counted. */
  readAllRows: (token: string, draft: MappingDraft) =>
    apiClient.post<FullPreview>(`/import/previews/${token}/rows`, draft),
  fetchIgnoreRules: () => apiClient.get<IgnoreRule[]>('/import/ignore-rules'),
  createIgnoreRule: (input: IgnoreRuleInput) => apiClient.post<IgnoreRule>('/import/ignore-rules', input),
  deleteIgnoreRule: (id: number) => apiClient.delete<void>(`/import/ignore-rules/${id}`),
  saveMapping: (accountId: number, draft: MappingDraft) =>
    apiClient.put<SavedMapping>(`/import/mappings/${accountId}`, {
      dateColumn: draft.dateColumn,
      descriptionColumn: draft.descriptionColumn,
      amountColumn: draft.amountColumn,
      debitColumn: draft.debitColumn,
      creditColumn: draft.creditColumn,
      dateFormat: draft.dateFormat,
      delimiter: draft.delimiter,
      hasHeaderRow: draft.hasHeaderRow,
    }),
}
