import { apiClient } from './client'

export interface Budget {
  id: number
  categoryId: number
  year: number
  month: number
  amount: number
}

export interface BudgetInput {
  categoryId: number
  year: number
  month: number
  amount: number
}

export const budgetsApi = {
  fetchForMonth: (year: number, month: number) => apiClient.get<Budget[]>(`/budgets?year=${year}&month=${month}`),
  create: (input: BudgetInput) => apiClient.post<Budget>('/budgets', input),
  update: (id: number, input: BudgetInput) => apiClient.put<Budget>(`/budgets/${id}`, input),
  delete: (id: number) => apiClient.delete<void>(`/budgets/${id}`),
}
