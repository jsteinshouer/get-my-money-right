import { apiClient } from './client'

export type AccountType = 'Checking' | 'Savings' | 'CreditCard'

export const accountTypes: AccountType[] = ['Checking', 'Savings', 'CreditCard']

export interface Account {
  id: number
  name: string
  type: AccountType
  isActive: boolean
}

export const accountsApi = {
  fetchAll: (includeInactive = false) =>
    apiClient.get<Account[]>(`/accounts${includeInactive ? '?includeInactive=true' : ''}`),
  create: (name: string, type: AccountType) => apiClient.post<Account>('/accounts', { name, type }),
  update: (id: number, name: string, type: AccountType) => apiClient.put<Account>(`/accounts/${id}`, { name, type }),
  deactivate: (id: number) => apiClient.post<void>(`/accounts/${id}/deactivate`),
}
