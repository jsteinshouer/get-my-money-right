import { apiClient } from './client'

export type NeedWant = 'Need' | 'Want'

export const needWants: NeedWant[] = ['Need', 'Want']

export interface Transaction {
  id: number
  accountId: number
  /** Null is the review queue: an imported transaction nobody has filed yet. */
  categoryId: number | null
  date: string
  amount: number
  description: string
  needWant: NeedWant
  tagIds: number[]
}

export interface TransactionInput {
  accountId: number
  categoryId: number
  date: string
  amount: number
  description: string
  needWant: NeedWant
}

export interface TransactionFilters {
  accountId?: number
  categoryId?: number
  dateFrom?: string
  dateTo?: string
  needWant?: NeedWant
  tagId?: number
  /** Just the review queue — the transactions with no category. */
  uncategorized?: boolean
}

function buildQuery(filters: TransactionFilters): string {
  const params = new URLSearchParams()
  if (filters.accountId) params.set('accountId', String(filters.accountId))
  if (filters.categoryId) params.set('categoryId', String(filters.categoryId))
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom)
  if (filters.dateTo) params.set('dateTo', filters.dateTo)
  if (filters.needWant) params.set('needWant', filters.needWant)
  if (filters.tagId) params.set('tagId', String(filters.tagId))
  if (filters.uncategorized) params.set('uncategorized', 'true')
  const query = params.toString()
  return query ? `?${query}` : ''
}

export const transactionsApi = {
  fetchAll: (filters: TransactionFilters = {}) => apiClient.get<Transaction[]>(`/transactions${buildQuery(filters)}`),
  create: (input: TransactionInput) => apiClient.post<Transaction>('/transactions', input),
  update: (id: number, input: TransactionInput) => apiClient.put<Transaction>(`/transactions/${id}`, input),
  delete: (id: number) => apiClient.delete<void>(`/transactions/${id}`),
}
