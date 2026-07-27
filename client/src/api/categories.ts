import { apiClient } from './client'

export interface Category {
  id: number
  name: string
}

export const categoriesApi = {
  fetchAll: () => apiClient.get<Category[]>('/categories'),
  create: (name: string) => apiClient.post<Category>('/categories', { name }),
  update: (id: number, name: string) => apiClient.put<Category>(`/categories/${id}`, { name }),
  delete: (id: number) => apiClient.delete<void>(`/categories/${id}`),
}
