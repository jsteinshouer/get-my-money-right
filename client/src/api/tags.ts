import { apiClient } from './client'

export interface Tag {
  id: number
  name: string
}

export const tagsApi = {
  fetchAll: () => apiClient.get<Tag[]>('/tags'),
  create: (name: string) => apiClient.post<Tag>('/tags', { name }),
  delete: (id: number) => apiClient.delete<void>(`/tags/${id}`),
  assign: (transactionId: number, tagId: number) =>
    apiClient.put<void>(`/transactions/${transactionId}/tags/${tagId}`),
  remove: (transactionId: number, tagId: number) =>
    apiClient.delete<void>(`/transactions/${transactionId}/tags/${tagId}`),
}
